using System.Text.Json;
using System.Text.RegularExpressions;
using cms.Data;
using cms.Models;
using cms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cms.Controllers;
[Authorize] 
public class TemplateController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEditorComponentService _editorComponentService;

    public TemplateController(ApplicationDbContext db, IEditorComponentService editorComponents)
    {
        _db = db;
        _editorComponentService = editorComponents;
    }

    [HttpGet("/templates")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var templates = await _db.Templates.OrderBy(x => x.Name).ToListAsync();
        return View("Index", templates);
    }
    
    [HttpGet("/templates/new")]
    public IActionResult New()
    {
        return View("New", new TemplateNewForm
        {
            Name = "",
        });
    }
    
    [ValidateAntiForgeryToken]
    [HttpPost("/templates/new")]
    public async Task<IActionResult> Create([FromForm] TemplateNewForm vm, CancellationToken ct)
    {
        NormalizeVm(vm);

        if (!ValidateSlug(vm.Name, out var err)) ModelState.AddModelError(nameof(vm.Name), err!);
        
        if (!ModelState.IsValid)
        {
            return View("New", vm);
        }

        var exists = await _db.Templates.AnyAsync(x => x.Name == vm.Name, ct);
        if (exists)
        {
            ModelState.AddModelError(nameof(vm.Name), "En template med dette navn findes allerede.");
            return View("New", vm);
        }

        var entity = new Template
        {
            Name = vm.Name,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Templates.Add(entity);
        await _db.SaveChangesAsync(ct);

        TempData["ok"] = "Template er oprettet.";
        return Redirect($"/template/{Uri.EscapeDataString(entity.Name)}");
    }
    
    // GET /template/{template}  (edit-form)
    [HttpGet("/template/{template}")]
    public async Task<IActionResult> Edit([FromRoute] string template, CancellationToken ct)
    {
        var entity = await _db.Templates.FirstOrDefaultAsync(x => x.Name == template, ct);
        if (entity is null) return NotFound();

        return View("Edit", new TemplateEditFrom()
        {
            OriginalName = entity.Name,
            Name = entity.Name,
            EditorComponents = _editorComponentService.GetEditorComponents()
        });
    }
    
    [HttpGet("/template/{template}.json")]
    public async Task<IActionResult> GetJson([FromRoute] string template, CancellationToken ct)
    {
        var entity = await _db.Templates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == template, ct);
        if (entity is null) return NotFound();

        // Parse den lagrede root (forventet: {"components":[...]} )
        using var doc = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(entity.Root) ? "{}" : entity.Root);
        var rootEl = doc.RootElement;

        // Hent components eller brug tom []
        System.Text.Json.JsonElement componentsEl;
        if (rootEl.ValueKind == System.Text.Json.JsonValueKind.Object &&
            rootEl.TryGetProperty("components", out var comps) &&
            comps.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            componentsEl = comps.Clone();
        }
        else
        {
            using var empty = System.Text.Json.JsonDocument.Parse("[]");
            componentsEl = empty.RootElement.Clone();
        }

        // Version som string (falder tilbage til "1" hvis ikke sat)
        var versionString = (entity.Version > 0 ? entity.Version : 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        
        return new JsonResult(new
        {
            template = entity.Name,    // fx "forside"
            version = versionString,   // "1"
            components = componentsEl  // behold original JSON-array
        });
    }
    
    [HttpPost("/template/{template}.json")]
    public async Task<IActionResult> SaveJson([FromRoute] string template, [FromBody] JsonDocument body, CancellationToken ct)
    {
        var (ok, errors) = await _editorComponentService.SaveAsync(_db, template, body, ct);

        if (!ok)
            return UnprocessableEntity(new { errors });

        return NoContent();
    }
    
    private static bool ValidateSlug(string? slug, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(slug)) { error = "Navn er påkrævet."; return false; }
        if (!Regex.IsMatch(slug, "^[a-z0-9][a-z0-9_-]{1,63}$"))
        {
            error = "Kun lowercase bogstaver, tal, '-' og '_' (2-64 tegn; skal starte med bogstav/tal).";
            return false;
        }
        return true;
    }
    
    private static void NormalizeVm(TemplateNewForm vm)
    {
        vm.Name = vm.Name.Trim().ToLowerInvariant();
    }
    
    [HttpGet("/templates/template.js")]
    public IActionResult TemplateJs()
        => Content(_editorComponentService.GetTemplateJavascript(), "application/javascript; charset=utf-8");

    [HttpGet("/templates/template.{hash}.js")]
    public IActionResult TemplateJsHashed([FromRoute] string hash)
        => hash.Equals(_editorComponentService.GetTemplateHash(), StringComparison.OrdinalIgnoreCase)
            ? Content(_editorComponentService.GetTemplateJavascript(), "application/javascript; charset=utf-8")
            : NotFound();
    
    
} 