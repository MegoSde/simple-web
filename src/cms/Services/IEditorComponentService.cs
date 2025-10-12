using System.Text.Json;
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
    
    string JavascriptPath { get; }
    
    Task<(bool Ok, List<JsonValidationError> Errors)> SaveAsync(ApplicationDbContext db, string templateName, JsonDocument body, CancellationToken ct);
}