using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace cms.Controllers;


[ApiController]
[Route("cmsimg")]
[Authorize]
public class CmsImageController : ControllerBase
{
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _cfg;

    public CmsImageController(IAmazonS3 s3, IConfiguration cfg)
    {
        _s3 = s3; _cfg = cfg;
    }

    // GET /cmsimg/{bucket}/{aa}/{bb}/{file}
    [HttpGet("cmsimg/{bucket}/{a:regex(^[[0-9A-Fa-f]]{{2}}$)}/{b:regex(^[[0-9A-Fa-f]]{{2}}$)}/{file}")]
    public async Task<IActionResult> Get(
        string bucket, string a, string b, string file, CancellationToken ct)
    {
        var workBucket = _cfg["Storage:S3:Buckets:Work"] ?? "work";
        var thumbBucket = _cfg["Storage:S3:Buckets:Thumbnail"] ?? "thumbnail";
        var bucketName = bucket.ToLowerInvariant() switch
        {
            "work" => workBucket,
            "thumbnail" => thumbBucket,
            _ => null
        };
        if (bucketName is null) return NotFound();
        if (file.Contains('/') || file.Contains('\\')) return BadRequest();

        var key = $"{a}/{b}/{file}";
        try
        {
            using var obj = await _s3.GetObjectAsync(bucketName, key, ct);

            Response.Headers.CacheControl = "private, max-age=3600";
            if (!string.IsNullOrEmpty(obj.ETag)) Response.Headers.ETag = obj.ETag;
            Response.GetTypedHeaders().LastModified = obj.LastModified;
            Response.Headers.ContentDisposition = $"inline; filename=\"{file}\"";

            await obj.ResponseStream.CopyToAsync(Response.Body, ct);
            return new EmptyResult();
        }
        catch (AmazonS3Exception ex) when ((int)ex.StatusCode == 404) { return NotFound(); }
        catch (AmazonS3Exception ex) when ((int)ex.StatusCode == 403) { return StatusCode(403); }
    }
}
