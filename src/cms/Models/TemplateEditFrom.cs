namespace cms.Models;

public class TemplateEditFrom
{
    public string? OriginalName { get; set; } // til redirect ved rename
    
    public string Name { get; set; } = "";
    
    public string[] EditorComponents { get; set; } = [];
}