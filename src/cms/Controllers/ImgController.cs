using System.Globalization;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using cms.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace cms.Controllers;

[ApiController]
public class ImgController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _cfg;

    private static readonly HashSet<string> KnownTypes =
        new(StringComparer.OrdinalIgnoreCase) { "webp", "jpg", "jpeg", "png" };

    public ImgController(ApplicationDbContext db, IAmazonS3 s3, IConfiguration cfg)
    {
        _db = db;
        _s3 = s3;
        _cfg = cfg;
    }

    // GET /img/{preset}/{aa}/{bb}/{hash}.{type}
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
        // --- basic path checks ---
        if (!IsHex2(a) || !IsHex2(b) || !IsHex64(hash)) return BadRequest("invalid_path");

        // --- preset ---
        var p = await _db.MediaPresets.AsNoTracking().FirstOrDefaultAsync(x => x.Name == preset, ct);
        if (p is null) return NotFound("preset_not_found");

        var t = NormalizeType(type);
        if (!KnownTypes.Contains(t)) return StatusCode(415, "unsupported_type");

        var allowed = (p.Types)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(t)) return StatusCode(415, "type_not_allowed_for_preset");

        // --- kilde fra work-bucket ---
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

        // --- læs bytes ---
        await using var srcMs = new MemoryStream();
        await obj.ResponseStream.CopyToAsync(srcMs, ct);
        var srcBytes = srcMs.ToArray();

        // --- ImageSharp load ---
        var dec = new DecoderOptions { Configuration = Configuration.Default };
        using var image = Image.Load(dec, srcBytes);

        // --- crop (hvis sat) ---
        // crop er normaliserede [x,y,w,h] i [0..1]
        var crop = await _db.MediaAssetCrops.AsNoTracking()
            .FirstOrDefaultAsync(c => c.AssetHash == hash && c.PresetName == preset, ct);

        if (crop is not null)
        {
            var rx = (int)Math.Round(crop.X * image.Width);
            var ry = (int)Math.Round(crop.Y * image.Height);
            var rw = (int)Math.Round(crop.W * image.Width);
            var rh = (int)Math.Round(crop.H * image.Height);

            // clamp
            rx = Math.Clamp(rx, 0, image.Width - 1);
            ry = Math.Clamp(ry, 0, image.Height - 1);
            rw = Math.Clamp(rw, 1, image.Width - rx);
            rh = Math.Clamp(rh, 1, image.Height - ry);

            image.Mutate(x => x.Crop(new Rectangle(rx, ry, rw, rh)));
        }

        // --- resize til præcis preset-størrelse hvis begge > 0, ellers fit ---
        if (p.Width > 0 && p.Height > 0)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(p.Width, p.Height),
                Mode = ResizeMode.Crop,              // cover -> exact WxH
                Sampler = KnownResamplers.Bicubic,
                Position = AnchorPositionMode.Center // kan gøres preset-styret senere
            }));
        }
        else if (p.Width > 0 || p.Height > 0)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(p.Width > 0 ? p.Width : 0, p.Height > 0 ? p.Height : 0),
                Mode = ResizeMode.Max,               // fit når kun én dimension er sat
                Sampler = KnownResamplers.Bicubic
            }));
        }

        // strip metadata
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;

        // --- encoder + content-type ---
        IImageEncoder encoder;
        string contentType;
        switch (t)
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

        // --- ETag der afspejler output-parametre (inkl. crop hvis sat) ---
        var etag = BuildEtag(hash, preset, p.Width, p.Height, t, crop);
        

        // If-None-Match
        if (Request.Headers.TryGetValue("If-None-Match", out var inm) &&
            inm.ToString().Split(',').Select(s => s.Trim()).Contains(etag, StringComparer.Ordinal))
        {
            Response.Headers["ETag"] = etag;
            Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            
            var lm = obj.LastModified;
            if (lm != null)
                Response.GetTypedHeaders().LastModified = new DateTimeOffset(DateTime.SpecifyKind((DateTime)lm, DateTimeKind.Utc));
            
            return StatusCode(304);
        }

        // --- render ---
        await using var outMs = new MemoryStream();
        image.Save(outMs, encoder);
        var bytes = outMs.ToArray();

        // --- headers ---
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{hash}.{t}\"";
        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["ETag"] = etag;
        
        var lm2 = obj.LastModified;
        if (lm2 != null)
            Response.GetTypedHeaders().LastModified = new DateTimeOffset(DateTime.SpecifyKind((DateTime)lm2, DateTimeKind.Utc));

        return File(bytes, contentType);
    }

    // ---------- helpers ----------

    private static string BuildEtag(string hash, string preset, int w, int h, string type, Models.MediaAssetCrop? crop)
    {
        // inkluder crop-koordinater hvis sat for at invalidere korrekt
        var cropPart = crop is null ? "nocrop" :
            $"{Round6(crop.X)},{Round6(crop.Y)},{Round6(crop.W)},{Round6(crop.H)}";

        var raw = $"{hash}|{preset}|{w}x{h}|{type}|{cropPart}";
        var sha = SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        var sig = Convert.ToHexString(sha).ToLowerInvariant();
        return $"\"{sig}\"";
    }

    private static string Round6(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);

    private static bool IsHex2(string s) =>
        s.Length == 2 && s.All(IsHexChar);

    private static bool IsHex64(string s) =>
        s.Length == 64 && s.All(IsHexChar);

    private static bool IsHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static string NormalizeType(string ext)
    {
        var t = ext.Trim().TrimStart('.').ToLowerInvariant();
        return t == "jpeg" ? "jpg" : t;
    }
}