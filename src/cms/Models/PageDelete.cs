using System.ComponentModel.DataAnnotations;

namespace cms.Models;

public class PageDelete
{
    public Guid Id { get; set; }
    public string FullPath { get; set; } = "";
    public string Title { get; set; } = "";
    [Required] public string? FullPathConfirm { get; set; }
}