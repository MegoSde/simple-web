using System.ComponentModel.DataAnnotations;

namespace cms.Models;

public class PageSettingsWm
{
    public Guid Id { get; set; }
    // visning
    public string FullPath { get; set; } = "";
    public string Title { get; set; } = "";

    // nuværende/template-valg
    [Required] public Guid TemplateId { get; set; }
    public string? CurrentTemplateName { get; set; }
    public List<Template> Templates { get; set; } = new();

    // indstillinger
    [Required]public bool InMenu { get; set; }
    [Required]public bool InSitemap { get; set; }
    public string? TemplateConfirm { get; set; }
}