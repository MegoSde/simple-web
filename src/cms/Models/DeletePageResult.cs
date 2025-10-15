namespace cms.Models;

public class DeletePageResult
{
    public Guid Deleted_Page_Id { get; set; }  // maps to deleted_page_id
    public string Deleted_Path { get; set; } = "";
    public int Deleted_Count { get; set; }     // number of nodes removed (incl. root)
}