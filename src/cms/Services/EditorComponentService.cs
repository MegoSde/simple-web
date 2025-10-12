using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using cms.Data;
using cms.EditorComponents;
using cms.Models;
using Microsoft.EntityFrameworkCore;

namespace cms.Services;


public sealed class EditorComponentService : IEditorComponentService
{
    private readonly Dictionary<string, IEditorComponent> _editorComponents;
    private readonly string _javascriptPath;
    
    private readonly string _editorJavascriptBundle;
    private readonly string _editorJavascriptHash;
    private readonly string _templateJavascriptBundle;
    private readonly string _templateJavascriptHash;

    public EditorComponentService(string javascriptPath, IEnumerable<Assembly>? scanAssemblies = null)
    {
        _javascriptPath = javascriptPath;
        
        // 2) Discover IComponent via reflection
        var assemblies = (scanAssemblies?.ToArray() ?? AppDomain.CurrentDomain.GetAssemblies());
        var editorComponentType = typeof(IEditorComponent);
        _editorComponents = new Dictionary<string, IEditorComponent>();

        foreach (var asm in assemblies)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }

            foreach (var t in types)
            {
                if ( t.IsAbstract || t.IsInterface) continue;
                if (!editorComponentType.IsAssignableFrom(t)) continue;
                if (t.GetConstructor(Type.EmptyTypes) is null) continue;

                if (Activator.CreateInstance(t) is IEditorComponent instance)
                    _editorComponents.Add(instance.Type, instance);
            }
        }

        // 3) Build bundle: shared + each component JS (deterministic order)
        
        var editorJsPath = Path.Combine(JavascriptPath, "editor.js");
        // 1) Load shared.js
        var editorJavascript = System.IO.File.Exists(editorJsPath)
            ? System.IO.File.ReadAllText(editorJsPath, Encoding.UTF8)
            : "// editor.js not found\n";
        
        var editorBuilder = new StringBuilder();
        editorBuilder.AppendLine("/* editor bundle - built at startup */");
        editorBuilder.AppendLine(editorJavascript);
        
        var templateJsPath = Path.Combine(JavascriptPath, "template.js");
        // 1) Load shared.js
        var templateJavascript = System.IO.File.Exists(templateJsPath)
            ? System.IO.File.ReadAllText(templateJsPath, Encoding.UTF8)
            : "// template.js not found\n";
        
        var templateBuilder = new StringBuilder();
        templateBuilder.AppendLine("/* template bundle - built at startup */");
        templateBuilder.AppendLine(templateJavascript);

        foreach (var c in _editorComponents.OrderBy(x => x.Key))
        {
            editorBuilder.AppendLine($"\n/* ===== {c.Value.Type}@{c.Value.Version} ===== */");
            editorBuilder.AppendLine(c.Value.GetJavascript(JavascriptPath));
            templateBuilder.AppendLine($"\n/* ===== {c.Value.Type}@{c.Value.Version} ===== */");
            templateBuilder.AppendLine(c.Value.GetTemplateJavascript(JavascriptPath));
        }
        
        _editorJavascriptBundle = editorBuilder.ToString();
        //templateBuilder.AppendLine("(function(){const nameInput=document.querySelector('input[name=\"OriginalName\"]');const templateName=nameInput?nameInput.value:\"\";const templateRoot=document.getElementById('template');if(!templateName||!templateRoot){return}fetch(`/template/${encodeURIComponent(templateName)}.json`,{headers:{Accept:'application/json'}}).then(r=>r.ok?r.json():Promise.reject(r)).then(data=>{window.Editor.loadTemplate(data,templateRoot);console.log(\"test1\");window.enableEditorControls?.();console.log(\"test2\")}).catch(console.error)})();");
        _templateJavascriptBundle = templateBuilder.ToString();

        // 4) Content hash (cache-busting)
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(_editorJavascriptBundle));
        _editorJavascriptHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var hashTemplateBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(_templateJavascriptBundle));
        _templateJavascriptHash = Convert.ToHexString(hashTemplateBytes).ToLowerInvariant();
    }

    public string[] GetEditorComponents()
    {
        return _editorComponents.Keys.Order().ToArray();
    }

    public string JavascriptPath => _javascriptPath;

    public string GetEditorJavascript() => _editorJavascriptBundle;
    public string GetEditorHash() => _editorJavascriptHash;
    public string GetTemplateJavascript() => _templateJavascriptBundle;
    public string GetTemplateHash() => _templateJavascriptHash;
    
    public async Task<(bool Ok, List<JsonValidationError> Errors)> SaveAsync(ApplicationDbContext db, string templateName, JsonDocument body, CancellationToken ct)
    {
        var errors = new List<JsonValidationError>();

        var entity = await db.Templates.FirstOrDefaultAsync(x => x.Name == templateName, ct);
        if (entity is null)
        {
            errors.Add(new("$.template", "template_not_found", $"Template '{templateName}' not found.", null));
            return (false, errors);
        }

        var root = body.RootElement;

        if (!root.TryGetProperty("components", out var componentsEl) || componentsEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new("$.components", "components_missing", "Body must contain a 'components' array.", null));
            return (false, errors);
        }

        int idx = 0;
        foreach (var el in componentsEl.EnumerateArray())
        {
            var path = $"$.components[{idx}]";

            // Læs id (kan være string eller number). Vi svarer altid som string.
            string? componentId = null;
            if (el.TryGetProperty("id", out var idEl))
            {
                componentId = idEl.ValueKind switch
                {
                    JsonValueKind.String => idEl.GetString(),
                    JsonValueKind.Number => idEl.TryGetInt64(out var n) ? n.ToString() : idEl.GetRawText(),
                    _ => null
                };
            }

            if (!el.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            { errors.Add(new(path, "type_missing", "Component.type is required.", componentId)); idx++; continue; }

            if (!el.TryGetProperty("v", out var vEl) || vEl.ValueKind != JsonValueKind.Number)
            { errors.Add(new(path, "version_missing", "Component.v is required.", componentId)); idx++; continue; }

            var type = typeEl.GetString()!;
            var v = vEl.GetInt32();

            var desc = _editorComponents[type];
            if (desc is null)
            {
                errors.Add(new(path, "component_unknown", $"Unknown component '{type}@{v}'.", componentId));
                idx++;
                continue;
            }

            // settings (tillad tomt)
            JsonElement settingsEl;
            if (!el.TryGetProperty("settings", out settingsEl))
                settingsEl = JsonDocument.Parse("{}").RootElement.Clone();

            try
            {
                var result = desc.ValidateSettings(settingsEl);
                if (!result.Ok)
                {
                    // tag alle fejl og påfør componentId + forankr path under denne komponent
                    foreach (var e in result.Errors)
                    {
                        var subPath = e.Path.StartsWith("$.") ? e.Path : $"$.{e.Path}";
                        errors.Add(new($"{path}{subPath.TrimStart('$')}", e.Code, e.Message, componentId));
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new(path, "validator_exception", $"Validator threw an exception: {ex.Message}", componentId));
            }

            idx++;
        }

        if (errors.Count > 0) return (false, errors);

        // Gem components som { "components": [...] }
        var newRoot = new { components = JsonSerializer.Deserialize<JsonElement>(componentsEl.GetRawText()) };
        entity.Root = JsonSerializer.Serialize(newRoot);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
        {
            var newName = nameEl.GetString()!.Trim();
            if (!string.IsNullOrEmpty(newName) && !string.Equals(newName, entity.Name, StringComparison.Ordinal))
                entity.Name = newName;
        }

        await db.SaveChangesAsync(ct);
        return (true, errors);
    }
}