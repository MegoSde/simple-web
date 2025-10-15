using System.Text.Json.Nodes;
using cms.Models;

namespace cms.EditorComponents;

internal interface IEditorComponent
{
    string Type { get; }
    int Version { get; }
    JsonObject InitJson();
    ValidationResult ValidateSettings(System.Text.Json.JsonElement settings);
    object Migrate(object props, int fromVersion); // no-op hvis latest
    
    string GetJavascript(string javascriptPath);
    string GetTemplateJavascript(string javascriptPath);
}

