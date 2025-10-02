using System.Text.RegularExpressions;
using cms.Data;
using cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cms.Controllers;
[Authorize] 
[Route("template")]
public class TemplateController : Controller
{
    private readonly ApplicationDbContext _db;

    public TemplateController(ApplicationDbContext db) => _db = db;

    [HttpGet()]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var templates = await _db.Templates.OrderBy(x => x.Name).ToListAsync();
        return View("Index", templates);
    }
    
    [HttpGet("new")]
    public IActionResult New()
    {
        return View("New", new TemplateNewForm
        {
            Name = "",
        });
    }
    
    [ValidateAntiForgeryToken]
    [HttpPost("new")]
    public async Task<IActionResult> Create([FromForm] TemplateNewForm vm, CancellationToken ct)
    {
        NormalizeVm(vm);

        if (!ValidateSlug(vm.Name, out var err)) ModelState.AddModelError(nameof(vm.Name), err!);
        if (vm.Name.Equals("new", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Name), "Navnet 'new' er reserveret.");

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
    
} 