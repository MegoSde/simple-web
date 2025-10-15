using System.Text;
using System.Text.Json.Nodes;
using cms.Models;

namespace cms.EditorComponents;

internal class HeroEditor : IEditorComponent
{
    public string Type => "hero";
    public int Version => 1;
    
    public JsonObject InitJson() => new JsonObject
    {
        ["title"] = "Angiv title",
        ["imgId"] = ""
    };
    
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
        var heroJsPath = Path.Combine(javascriptPath, "hero.js");
        var heroJs = System.IO.File.Exists(heroJsPath)
            ? System.IO.File.ReadAllText(heroJsPath, Encoding.UTF8)
            : "// hero.js not found\n";
        return heroJs;
    }
    public string GetTemplateJavascript(string javascriptPath)
    {
        var heroJsPath = Path.Combine(javascriptPath, "template_hero.js");
        var heroJs = System.IO.File.Exists(heroJsPath)
            ? System.IO.File.ReadAllText(heroJsPath, Encoding.UTF8)
            : "// template_hero.js not found\n";
        return heroJs;
    }
}