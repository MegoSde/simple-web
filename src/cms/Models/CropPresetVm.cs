namespace cms.Models;
public class CropPresetVm
{
    public string Name { get; set; } = default!;
    public int Width { get; set; }
    public int Height { get; set; }
    public string RatioKey { get; set; } = "free";
    public string Types { get; set; } = "webp";
    public double[]? Existing { get; set; } // x,y,w,h normaliseret
}