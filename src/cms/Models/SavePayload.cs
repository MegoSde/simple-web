using System.Text.Json.Nodes;
namespace cms.Models;

public sealed class SavePayload
{
    public Guid PageId { get; set; }
    public int Version { get; set; }
    public SaveContent Content { get; set; } = new();
}

public sealed class SaveContent
{
    public List<ComponentDto> Components { get; set; } = new();
    public Dictionary<string, object>? Settings { get; set; } 
}

public sealed class ComponentDto
{
    public int V { get; set; }
    public string Type { get; set; } = "";
    public JsonObject? Settings { get; set; }
    public JsonObject? Data { get; set; }
}