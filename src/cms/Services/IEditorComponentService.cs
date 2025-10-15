using System.Text.Json;
using System.Text.Json.Nodes;
using cms.Data;
using cms.Models;

namespace cms.Services;

public interface IEditorComponentService
{
    /// <summary>The in-memory editor bundle (shared + components) built at startup.</summary>
    string GetEditorJavascript();

    string GetEditorHash();
    
    string GetTemplateJavascript();
    
    string GetTemplateHash();

    string[] GetEditorComponents();

    public JsonObject InitJson(string component);
    
    string JavascriptPath { get; }
    
    Task<(bool Ok, List<JsonValidationError> Errors)> SaveAsync(ApplicationDbContext db, string templateName, JsonDocument body, CancellationToken ct);
}