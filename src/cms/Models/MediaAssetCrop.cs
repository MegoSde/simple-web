using System.ComponentModel.DataAnnotations;

namespace cms.Models;
public class MediaAssetCrop
{
    public Guid Id { get; set; }
    [MaxLength(64)]
    public string AssetHash { get; set; } = default!;
    [MaxLength(32)]
    public string PresetName { get; set; } = default!;
    public double X { get; set; } // [0..1]
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    [MaxLength(254)]
    public string UpdatedBy { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}