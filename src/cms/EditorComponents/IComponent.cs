using cms.Models;

namespace cms.EditorComponents;

internal interface IEditorComponent
{
    string Type { get; }      // "section", "hero", ...
    int Version { get; }      // 1, 2, ...

    ValidationResult ValidateSettings(System.Text.Json.JsonElement settings);
    object Migrate(object props, int fromVersion); // no-op hvis latest
    
    string GetJavascript(string javascriptPath);
    string GetTemplateJavascript(string javascriptPath);
}

