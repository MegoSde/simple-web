using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using cms.Data;
using cms.Models;
using cms.Common.Errors;

namespace cms.Services;

public class MediaService
{
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _cfg;
    private readonly ApplicationDbContext _db;

    public MediaService(IAmazonS3 s3, IConfiguration cfg, ApplicationDbContext db)
    {
        _s3 = s3;
        _cfg = cfg;
        _db = db;
    }

    public async Task<MediaCreateResponse> UploadAsync(
        Stream file,
        string fileName,
        string mimeFromClient,
        string? altText,
        string uploadedBy,
        string? metaJson,
        CancellationToken ct)
    {
        // --- 0) Buckets fra config (ensartede nøgler) ---
        var bucketOriginals = _cfg["Storage:S3:Buckets:Originals"] ?? "originals";
        var bucketWork      = _cfg["Storage:S3:Buckets:Work"]      ?? "work";
        var bucketThumb     = _cfg["Storage:S3:Buckets:Thumbnail"] ?? "thumbnail";

        // --- 1) Validering (MIME + størrelse) ---
        var allowed = _cfg.GetSection("Upload:AllowedMime").Get<string[]>() ?? Array.Empty<string>();
        var mime = (mimeFromClient ?? "application/octet-stream").ToLowerInvariant();
        if (!allowed.Contains(mime))
            throw new ApiError(415, "unsupported_mime", "MIME-type ikke tilladt.");

        var maxBytes = _cfg.GetValue<long>("Upload:MaxBytes", 5 * 1024 * 1024);
        if (file.Length <= 0 || file.Length > maxBytes)
            throw new ApiError(413, "file_too_large", $"Filen overstiger max {maxBytes} bytes.");

        // --- 2) Læs bytes i memory ---
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var originalBytes = ms.ToArray();

        // --- 3) Detect format + load image (ImageSharp v3) ---
        var dec = new DecoderOptions { Configuration = Configuration.Default };
        var detectedFormat = Image.DetectFormat(dec, originalBytes);
        if (detectedFormat is null)
            throw new ApiError(415, "unsupported_mime", "Filen er ikke et understøttet billede.");

        using var img = Image.Load(dec, originalBytes);
        var width = img.Width;
        var height = img.Height;

        // --- 4) Strip EXIF/ICC og encoder til originalens format ---
        var strip = _cfg.GetValue("Upload:StripMetadataOnUpload", true);
        IImageEncoder originalEncoder = EncoderForFormat(detectedFormat);

        byte[] originalPayload;
        if (strip)
        {
            img.Metadata.ExifProfile = null;
            img.Metadata.IccProfile = null;
            using var outMs = new MemoryStream();
            img.Save(outMs, originalEncoder);
            originalPayload = outMs.ToArray();
        }
        else
        {
            originalPayload = originalBytes;
        }

        // Hash over de bytes, vi faktisk gemmer
        var hash = Convert.ToHexString(SHA256.HashData(originalPayload)).ToLowerInvariant();
        var a = hash[..2];
        var b = hash[2..4];

        // --- 5) Nøgler + URL ---
        var ext = detectedFormat.FileExtensions?.FirstOrDefault() ?? "bin";
        var originalKey = $"{a}/{b}/{hash}.{ext}";

        var publicBase = (_cfg["Storage:PublicBaseUrl"] ?? "").TrimEnd('/');
        var originalUrl = string.IsNullOrEmpty(publicBase) ? originalKey : $"{publicBase}/{originalKey}";

        // --- 6) Upload ORIGINAL til originals-bucket ---
        await PutObjectAsync(bucketOriginals, originalKey, originalPayload, detectedFormat.DefaultMimeType ?? mime, ct);

        // --- 7) Upload WEBP-kopi til work-bucket (best effort) ---
        try
        {
            using var webpMs = new MemoryStream();
            var webpEnc = new WebpEncoder { Quality = 82 };
            img.Save(webpMs, webpEnc);                // brug den allerede indlæste img
            await PutObjectAsync(bucketWork, $"{a}/{b}/{hash}.webp", webpMs.ToArray(), "image/webp", ct);
        }
        catch
        {
            // bevidst: fejl i derivat må ikke vælte hele upload
        }

        // --- 8) Upload THUMBNAIL til thumbnail-bucket (best effort) ---
        try
        {
            //using var clone = img.Clone();            // undgå at resize original-instansen
            /*var wTarget = 320;
            var resize = new ResizeOptions
            {
                Size = new Size(wTarget, 0),
                Mode = ResizeMode.Max // bevar aspekt
            };
            img.Mutate(x => x.Resize(resize));

            using var thumbMs = new MemoryStream();
            var webpEncThumbnail = new WebpEncoder { Quality = 60 };
            img.Save(thumbMs, webpEncThumbnail);
            await PutObjectAsync(bucketThumb, $"{a}/{b}/{hash}.webp", thumbMs.ToArray(), "image/webp", ct);*/
            using (var thumb = img.Clone(ctx => ctx.Resize(new ResizeOptions {
                       Size = new Size(320, 0), Mode = ResizeMode.Max
                   })))
            using (var thumbMs = new MemoryStream())
            {
                var webpEncThumb = new WebpEncoder { Quality = 60 };
                thumb.Save(thumbMs, webpEncThumb);
                await PutObjectAsync(bucketThumb, $"{a}/{b}/{hash}.webp", thumbMs.ToArray(), "image/webp", ct);
            }
        }
        catch
        {
            // bevidst: fejl i derivat må ikke vælte hele upload
        }

        // --- 9) Gem metadata i DB ---
        var entity = new MediaAsset
        {
            Hash = hash,
            OriginalUrl = originalUrl,
            Mime = detectedFormat.DefaultMimeType ?? mime,
            Width = width,
            Height = height,
            Bytes = originalPayload.LongLength,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
            UploadedBy = uploadedBy,
            Meta = string.IsNullOrWhiteSpace(metaJson) ? "{}" : metaJson
        };

        _db.MediaAssets.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new MediaCreateResponse(entity.Id, entity.Hash, entity.OriginalUrl, entity.Width, entity.Height, entity.Mime);
    }

    private async Task PutObjectAsync(string bucket, string key, byte[] payload, string contentType, CancellationToken ct)
    {
        using var s = new MemoryStream(payload);
        var req = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = s,
            ContentType = contentType
        };
        req.Headers.ContentLength = payload.LongLength;
        await _s3.PutObjectAsync(req, ct);
    }

    private static IImageEncoder EncoderForFormat(IImageFormat fmt) =>
        fmt switch
        {
            JpegFormat => new JpegEncoder(),
            PngFormat  => new PngEncoder(),
            WebpFormat => new WebpEncoder(),
            _ => throw new ApiError(415, "unsupported_mime", "Kun JPEG/PNG/WebP understøttes.")
        };
}