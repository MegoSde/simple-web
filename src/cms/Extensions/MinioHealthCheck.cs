using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;

namespace cms.Extensions;

public sealed class MinioHealthCheck : IHealthCheck
{
    private readonly IMinioClient _minio;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _httpClientFactory;

    public MinioHealthCheck(IMinioClient minio, IConfiguration cfg,  IHttpClientFactory httpClientFactory)
    {
        _minio = minio;
        _cfg = cfg;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var endpoint = _cfg["Storage:S3:Endpoint"] ?? "";
        var useSsl   = bool.TryParse(_cfg["Storage:S3:UseSSL"], out var u) && u;
        var access   = _cfg["Storage:S3:AccessKey"];
        var secret   = _cfg["Storage:S3:SecretKey"];
        var desiredBucket = _cfg["Health:Minio:Bucket"] ?? _cfg["Storage:S3:Buckets:Originals"] ?? "";

        var data = new Dictionary<string, object?>
        {
            ["endpoint"] = endpoint,
            ["useSsl"]   = useSsl,
            ["hasAccessKey"] = !string.IsNullOrWhiteSpace(access),
            ["hasSecretKey"] = !string.IsNullOrWhiteSpace(secret),
            ["desiredBucket"] = desiredBucket
        };

        // 1) Ping /minio/health/live med HttpClient (ingen auth)
        try
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return HealthCheckResult.Unhealthy("Missing S3 endpoint", null, data!);

            var baseUri = new Uri(endpoint);
            var client = _httpClientFactory.CreateClient("minio-health");
            client.Timeout = TimeSpan.FromSeconds(2);
            
            var resp = await client.GetAsync(new Uri(baseUri, "/minio/health/live"), ct);
            data["httpStatus"] = (int)resp.StatusCode;

            if (!resp.IsSuccessStatusCode)
            {
                data["liveBody"] = await SafeRead(resp, ct);
                return HealthCheckResult.Unhealthy("MinIO health/live not OK", null, data!);
            }
        }
        catch (Exception ex)
        {
            data["exception"] = ex.Message;
            return HealthCheckResult.Unhealthy("MinIO not reachable (HTTP)", ex, data!);
        }

        // 2) Tjek bucket + credentials via SDK – robust mod SDK-bugs
        try
        {
            if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret))
                return HealthCheckResult.Unhealthy("Missing S3 credentials", null, data!);

            var buckets = await _minio.ListBucketsAsync(ct);
            var names = buckets?.Buckets?.Select(b => b.Name).ToArray() ?? Array.Empty<string>();
            data["bucketCount"] = names.Length;
            data["buckets"] = names;

            // 3) (Valgfrit) bekræft at ønsket bucket findes – uden BucketExistsAsync
            if (!string.IsNullOrWhiteSpace(desiredBucket))
            {
                var found = names.Any(n => string.Equals(n, desiredBucket, StringComparison.Ordinal));
                data["desiredBucketFound"] = found;
                if (!found)
                    return HealthCheckResult.Unhealthy($"Bucket '{desiredBucket}' not found (via ListBuckets)", null, data!);
            }

            return HealthCheckResult.Healthy("MinIO OK (credentials + reachability)", data!);
        }
        catch (Exception ex)
        {
            data["exception"] = ex.Message;
            return HealthCheckResult.Unhealthy("MinIO credentials/list failed", ex, data!);
        }
    }
    
    private static async Task<string?> SafeRead(HttpResponseMessage resp, CancellationToken ct)
    {
        try { return await resp.Content.ReadAsStringAsync(ct); }
        catch { return null; }
    }
}