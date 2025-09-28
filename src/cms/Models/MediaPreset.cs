using System.ComponentModel.DataAnnotations;

namespace cms.Models;

public class MediaPreset
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = default!; // slug, fx "thumb", "hero-16x9"

    [Range(0, 10000)]
    public int Width { get; set; }   // 0 = ikke begrænset

    [Range(0, 10000)]
    public int Height { get; set; }  // 0 = ikke begrænset

    // CSV med tilladte typer (lowercase), fx "webp,jpg"
    [Required, MaxLength(128)]
    public string Types { get; set; } = "webp";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}