namespace cms.Models;

public class TemplateNewForm
{
    public string? OriginalName { get; set; } // til redirect ved rename
    public string Name { get; set; } = "";
}