namespace cms.Models;

public class DeletePageInput
{
    public Guid PageId { get; set; }
    public string FullPath { get; set; } = "";
    public string? ConfirmPath { get; set; }
}