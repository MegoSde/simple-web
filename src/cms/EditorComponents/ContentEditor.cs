using System.Text;
using cms.Models;

namespace cms.EditorComponents;

internal class ContentEditor : IEditorComponent
{
    public string Type => "content";
    public int Version => 1;
    
    public ValidationResult ValidateSettings(System.Text.Json.JsonElement settings)
    {
        return ValidationResult.Success();
    }

    public object Migrate(object props, int fromVersion)
    {
        throw new NotImplementedException();
    }
    
    public string GetJavascript(string javascriptPath)
    {
        var jsPath = Path.Combine(javascriptPath, "content.js");
        var js = System.IO.File.Exists(jsPath)
            ? System.IO.File.ReadAllText(jsPath, Encoding.UTF8)
            : "// content.js not found\n";
        return js;
    }
    public string GetTemplateJavascript(string javascriptPath)
    {
        var jsPath = Path.Combine(javascriptPath, "template_content.js");
        var js = System.IO.File.Exists(jsPath)
            ? System.IO.File.ReadAllText(jsPath, Encoding.UTF8)
            : "// template_content.js not found\n";
        return js;
    }
}