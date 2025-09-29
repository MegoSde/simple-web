namespace cms.Models;

public class PresetFormVm
{
    public string? OriginalName { get; set; } // til redirect ved rename
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public string[]? Types { get; set; }
    public string[] AllowedTypes { get; set; } = Array.Empty<string>();
}