using cms.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;


namespace cms.Extensions;

public static class HealthSetupExtensions
{
    /// <summary>
    /// Tilføjer controllers og health checks til DI-containeren.
    /// Holdes som extension for at holde Program.cs minimal.
    /// </summary>
    public static WebApplicationBuilder AddAppHealth(this WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration;

        // Controllers (så HealthController virker uden Program.cs-mapping)
        builder.Services.AddControllers();

        builder.Services.AddHttpClient("minio-health").ConfigureHttpClient(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(2);
        });
        // Start HealthChecks builder med "self" og Postgres
        var hc = builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddDbContextCheck<ApplicationDbContext>(
                name: "postgres",
                customTestQuery: (ctx, ct) => ctx.Database.CanConnectAsync(ct),
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "db" });

        // ---- MinIO (S3) Health optional ----
        // Styres af: Health:Minio:Enabled (default true)
        var minioEnabled = cfg.GetValue<bool?>("Health:Minio:Enabled") ?? true;
        if (minioEnabled)
        {
            // Læs S3 settings
            var s3 = cfg.GetSection("Storage:S3");
            var endpoint = s3["Endpoint"] ?? "http://minio:9000";
            var endpointHost = endpoint.Replace("http://", "").Replace("https://", "");
            var useSsl = bool.TryParse(s3["UseSSL"], out var u) && u;
            var accessKey = s3["AccessKey"];
            var secretKey = s3["SecretKey"];

            // Registrér IMinioClient hvis ikke allerede registreret andetsteds
            builder.Services.AddSingleton<IMinioClient>(_ =>
            {
                var mc = new MinioClient()
                    .WithEndpoint(endpointHost)
                    .WithCredentials(accessKey, secretKey);
                if (useSsl) mc = mc.WithSSL();
                if (!string.IsNullOrWhiteSpace(s3["Region"]))
                    mc = mc.WithRegion(s3["Region"]);
                return mc.Build();
            });

            // Tilføj MinIO health check (bucketnavn læses inde i selve checket)
            hc.AddCheck<MinioHealthCheck>(
                "minio",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "s3" });
        }

        return builder;
    }
}
