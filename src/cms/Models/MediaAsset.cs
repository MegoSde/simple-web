using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cms.Models;

public sealed class MediaAsset
{
    public Guid Id { get; set; }

    [MaxLength(64)]
    public string Hash { get; set; } = default!;

    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = default!;

    [MaxLength(127)]
    public string Mime { get; set; } = default!;

    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }

    [MaxLength(512)]
    public string? AltText { get; set; }

    [MaxLength(254)]
    public string UploadedBy { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column(TypeName = "text")]
    [MaxLength(2048)]
    public string Meta { get; set; } = "{}";
}