namespace cms.Models;
public class CropEditorVm
{
    public string Hash { get; set; } = default!;
    public string A { get; set; } = default!;
    public string B { get; set; } = default!;
    public string WorkSrc { get; set; } = default!;
    public List<CropPresetVm> Presets { get; set; } = new();
    public List<RatioGroupVm> Groups { get; set; } = new();
}