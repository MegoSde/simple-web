using System.Net;
using Amazon.S3;
using Microsoft.Net.Http.Headers;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace cms.Extensions;

public static class CmsImageProxyEndpoints
{
    public static IEndpointRouteBuilder MapCmsImageProxyEndpoints(this IEndpointRouteBuilder app)
    {
        // Kun logged-in (tilpas roller hvis ønsket)
        var grp = app.MapGroup("/cmsimg")
                     .RequireAuthorization(); // .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,editor" });

        // /cmsimg/{bucket}/{aa}/{bb}/{file}
        grp.MapGet("{bucket}/{a:length(2):regex(^[a-f0-9]+$)}/{b:length(2):regex(^[a-f0-9]+$)}/{file}",
            async (string bucket, string a, string b, string file, IAmazonS3 s3, IConfiguration cfg, HttpContext ctx, CancellationToken ct) =>
            {
                // Kun tilladte buckets via alias -> faktiske bucket-navne fra config
                var bucketAlias = bucket.ToLowerInvariant();
                var workBucket = cfg["Storage:S3:Buckets:Work"] ?? "work";
                var thumbBucket = cfg["Storage:S3:Buckets:Thumbnail"] ?? "thumbnail";

                string? bucketName = bucketAlias switch
                {
                    "work" => workBucket,
                    "thumbnail" => thumbBucket,
                    _ => null
                };
                if (bucketName is null)
                    return Results.NotFound(); // ukendt bucket

                // Ekstra sikkerhed mod path tricks
                if (file.Contains('/') || file.Contains('\\'))
                    return Results.BadRequest();

                var key = $"{a}/{b}/{file}";

                try
                {
                    using var obj = await s3.GetObjectAsync(bucketName, key, ct);

                    // MIME: fra S3 eller gæt ud fra filendelse
                    var mime = obj.Headers.ContentType;
                    if (string.IsNullOrWhiteSpace(mime))
                        mime = GuessMime(file);

                    var rsp = ctx.Response;
                    rsp.ContentType = mime;
                    if (obj.Headers.ContentLength >= 0)
                        rsp.ContentLength = obj.Headers.ContentLength;

                    // Cache-hints til CMS (privat cache ok)
                    rsp.Headers.CacheControl = "private, max-age=3600";
                    if (!string.IsNullOrEmpty(obj.ETag))
                        rsp.Headers.ETag = obj.ETag;
                    
                    var typed = ctx.Response.GetTypedHeaders();
                    typed.CacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromHours(1) };
                    typed.ETag = string.IsNullOrEmpty(obj.ETag) ? null : new EntityTagHeaderValue(obj.ETag);
                    typed.LastModified = obj.LastModified; // accepterer DateTimeOffset/DateTime
                    
                    if (!string.IsNullOrEmpty(obj.ETag))
                        ctx.Response.Headers["ETag"] = obj.ETag;

                    // inline visning
                    rsp.Headers.ContentDisposition = $"inline; filename=\"{file}\"";

                    await obj.ResponseStream.CopyToAsync(rsp.Body, ct);
                    return Results.Empty;
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return Results.NotFound();
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }
            });

        return app;
    }

    private static string GuessMime(string file)
    {
        var ext = System.IO.Path.GetExtension(file)?.ToLowerInvariant();
        return ext switch
        {
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
