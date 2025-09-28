namespace cms.Models;

public class MediaAsset {
    public Guid Id { get; set; }
    public string Hash { get; set; } = default!;
    public string OriginalUrl { get; set; } = default!;
    public string Mime { get; set; } = default!;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }
    public string? AltText { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Meta { get; set; } = "{}"; // map til jsonb via EF: string <-> jsonb
    // Alternativt: brug JsonDocument / Dictionary<string,object> + EF converter
}