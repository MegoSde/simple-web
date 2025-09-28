using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using cms.Data;

namespace cms.Controllers;

[ApiController]
public class ImgController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _cfg;
    private static readonly HashSet<string> KnownTypes = new(StringComparer.OrdinalIgnoreCase) { "webp", "jpg", "jpeg", "png" };

    public ImgController(ApplicationDbContext db, IAmazonS3 s3, IConfiguration cfg)
    {
        _db = db;
        _s3 = s3;
        _cfg = cfg;
    }

    // /img/{preset}/{aa}/{bb}/{hash}.{type}
    // Undgår komplekse regex i route for ikke at ramme { }-escape-problemer.
    [HttpGet("/img/{preset}/{a:length(2)}/{b:length(2)}/{hash:length(64)}.{type}")]
    [ResponseCache(NoStore = false, Location = ResponseCacheLocation.Any, Duration = 31536000)]
    public async Task<IActionResult> Get(
        [FromRoute] string preset,
        [FromRoute] string a,
        [FromRoute] string b,
        [FromRoute] string hash,
        [FromRoute] string type,
        CancellationToken ct)
    {
        // Basic path validation (hex)
        if (!(IsHex2(a) && IsHex2(b) && IsHex64(hash)))
            return BadRequest("invalid_path");

        // Find preset i DB
        var p = await _db.MediaPresets.AsNoTracking().FirstOrDefaultAsync(x => x.Name == preset, ct);
        if (p is null) return NotFound("preset_not_found");

        // Normaliser type + check mod preset.Types
        var t = NormalizeType(type); // "jpeg" -> "jpg"
        if (!KnownTypes.Contains(t)) return StatusCode(415, "unsupported_type");

        var allowed = (p.Types ?? "webp")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => NormalizeType(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(t))
            return StatusCode(415, "type_not_allowed_for_preset");

        // ETag baseret på content-adressable hash + preset + type + dims
        var etag = $"\"{hash}.{preset}.{t}.{p.Width}x{p.Height}\"";
        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString().Split(',').Select(s => s.Trim()).Contains(etag))
        {
            Response.Headers["ETag"] = etag;
            Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            return StatusCode(304);
        }

        // Hent source (arbejdskopi i WEBP)
        var workBucket = _cfg["Storage:S3:Buckets:Work"] ?? "work";
        var key = $"{a}/{b}/{hash}.webp";

        GetObjectResponse obj;
        try
        {
            obj = await _s3.GetObjectAsync(workBucket, key, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("source_not_found");
        }

        // Læs/transformér
        await using var s = new MemoryStream();
        await obj.ResponseStream.CopyToAsync(s, ct);
        s.Position = 0;

        var dec = new DecoderOptions { Configuration = Configuration.Default };

        using var image = Image.Load(dec, s); // loader WEBP-arbejdskopien

        // Resize (præcis størrelse hvis begge dimensioner er angivet; ellers "fit")
        if (p.Width > 0 && p.Height > 0)
        {
            // "Cover" = skaler og beskær for at udfylde nøjagtigt Width×Height
            var opts = new ResizeOptions
            {
                Size = new Size(p.Width, p.Height),
                Mode = ResizeMode.Crop,                 // <-- vigtig: beskæring
                Sampler = KnownResamplers.Bicubic,
                Position = AnchorPositionMode.Center    // evt. senere: gør dette konfigurerbart
            };
            image.Mutate(x => x.Resize(opts));
        }
        else if (p.Width > 0 || p.Height > 0)
        {
            // Backwards compatible: hvis kun én dimension er sat, så fit (bevar aspekt)
            var size = new Size(p.Width > 0 ? p.Width : 0, p.Height > 0 ? p.Height : 0);
            var opts = new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Bicubic
            };
            image.Mutate(x => x.Resize(opts));
        }
        // ellers: ingen resize

        // Stripp metadata for output
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;

        // Vælg encoder + content-type
        IImageEncoder encoder;
        string contentType;
        switch (t.ToLowerInvariant())
        {
            case "webp":
                encoder = new WebpEncoder { Quality = 82 };
                contentType = "image/webp";
                break;
            case "jpg":
            case "jpeg":
                encoder = new JpegEncoder { Quality = 82 };
                contentType = "image/jpeg";
                break;
            case "png":
                encoder = new PngEncoder();
                contentType = "image/png";
                break;
            default:
                return StatusCode(415, "unsupported_type");
        }

        await using var outMs = new MemoryStream();
        image.Save(outMs, encoder);
        var bytes = outMs.ToArray();

        // Cache-headere (perfekt for Nginx cache)
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{hash}.{t}\"";
        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["ETag"] = etag;
        var th = Response.GetTypedHeaders();
        // DateTime eller DateTimeOffset accepteres
        th.LastModified = obj.LastModified;

        return File(bytes, contentType);
    }

    private static bool IsHex2(string s) =>
        s.Length == 2 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

    private static bool IsHex64(string s) =>
        s.Length == 64 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

    private static string NormalizeType(string ext)
    {
        var t = (ext ?? "").Trim().TrimStart('.').ToLowerInvariant();
        return t == "jpeg" ? "jpg" : t;
    }
}
