namespace cms.Models;

public class PageNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string FullPath { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public int Depth { get; set; }
    public bool InMenu { get; set; }
    public bool InSitemap { get; set; }
    public int LatestVersionNo { get; set; }
    public int PublishedVersionNo { get; set; }
    
    public Guid LatestTemplateId { get; set; }
    public bool HasPublished { get; set; }
    public bool HasNewerDraft { get; set; }
}