using System.ComponentModel.DataAnnotations;

namespace cms.Models;

public class AddChildPageInput
{
    [Required] public string Title { get; set; } = "";
    [Required, RegularExpression("^[a-z0-9-]+$")]
    public string Slug { get; set; } = "";
    [Required] public Guid TemplateId { get; set; } 
    public bool InMenu { get; set; } = true;
    public bool InSitemap { get; set; } = true;

    // visuel hjælp i view
    public string ParentFullPath { get; set; } = "";
    public string ParentTitle { get; set; } = "";
    
    public List<Template> Templates { get; set; } = new();
}