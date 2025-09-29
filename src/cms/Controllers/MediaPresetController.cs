using System.Text.RegularExpressions;
using cms.Data;
using cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cms.Controllers;

[Authorize] // justér roller efter behov
public class MediaPresetController : Controller
{
    private readonly ApplicationDbContext _db;
    private static readonly string[] AllowedTypesUniverse = new[] { "webp", "jpg", "png" }; // udvid evt.

    public MediaPresetController(ApplicationDbContext db) => _db = db;

    // GET /imgpreset  (liste)
    [HttpGet("/media/preset")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await _db.MediaPresets
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return View("Index", items);
    }

    // GET /media/preset/new  (opret-form)
    [HttpGet("/media/preset/new")]
    public IActionResult New()
    {
        return View("New", new PresetFormVm
        {
            Name = "",
            Width = 0,
            Height = 0,
            Types = new[] { "webp" },
            AllowedTypes = AllowedTypesUniverse
        });
    }

    // POST /media/preset/new  (opret)
    [ValidateAntiForgeryToken]
    [HttpPost("/media/preset/new")]
    public async Task<IActionResult> Create([FromForm] PresetFormVm vm, CancellationToken ct)
    {
        NormalizeVm(vm);

        if (!ValidateSlug(vm.Name, out var err)) ModelState.AddModelError(nameof(vm.Name), err!);
        if (vm.Name.Equals("new", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Name), "Navnet 'new' er reserveret.");

        if (!vm.Types!.Any()) ModelState.AddModelError(nameof(vm.Types), "Vælg mindst én type.");
        if (vm.Types!.Except(AllowedTypesUniverse, StringComparer.OrdinalIgnoreCase).Any())
            ModelState.AddModelError(nameof(vm.Types), "Ugyldig type valgt.");

        if (!ModelState.IsValid)
        {
            vm.AllowedTypes = AllowedTypesUniverse;
            return View("New", vm);
        }

        var exists = await _db.MediaPresets.AnyAsync(x => x.Name == vm.Name, ct);
        if (exists)
        {
            ModelState.AddModelError(nameof(vm.Name), "Et preset med dette navn findes allerede.");
            vm.AllowedTypes = AllowedTypesUniverse;
            return View("New", vm);
        }

        var entity = new MediaPreset
        {
            Name = vm.Name,
            Width = vm.Width,
            Height = vm.Height,
            Types = string.Join(',', vm.Types!.Select(t => t.ToLowerInvariant())),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.MediaPresets.Add(entity);
        await _db.SaveChangesAsync(ct);

        TempData["ok"] = "Preset oprettet.";
        return Redirect($"/media/preset/{Uri.EscapeDataString(entity.Name)}");
    }

    // GET /media/preset/{preset}  (edit-form)
    [HttpGet("/media/preset/{preset}")]
    public async Task<IActionResult> Edit([FromRoute] string preset, CancellationToken ct)
    {
        var entity = await _db.MediaPresets.FirstOrDefaultAsync(x => x.Name == preset, ct);
        if (entity is null) return NotFound();

        return View("Edit", new PresetFormVm
        {
            OriginalName = entity.Name,
            Name = entity.Name,
            Width = entity.Width,
            Height = entity.Height,
            Types = entity.Types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            AllowedTypes = AllowedTypesUniverse
        });
    }

    // POST /media/preset/{preset}  (update)
    [ValidateAntiForgeryToken]
    [HttpPost("/media/preset/{preset}")]
    public async Task<IActionResult> Update([FromRoute] string preset, [FromForm] PresetFormVm vm, CancellationToken ct)
    {
        NormalizeVm(vm);

        var entity = await _db.MediaPresets.FirstOrDefaultAsync(x => x.Name == preset, ct);
        if (entity is null) return NotFound();

        if (!ValidateSlug(vm.Name, out var err)) ModelState.AddModelError(nameof(vm.Name), err!);
        if (vm.Name.Equals("new", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Name), "Navnet 'new' er reserveret.");
        if (!vm.Types!.Any()) ModelState.AddModelError(nameof(vm.Types), "Vælg mindst én type.");
        if (vm.Types!.Except(AllowedTypesUniverse, StringComparer.OrdinalIgnoreCase).Any())
            ModelState.AddModelError(nameof(vm.Types), "Ugyldig type valgt.");

        // Hvis navnet ændres, må det ikke kollidere
        if (!vm.Name.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameTaken = await _db.MediaPresets.AnyAsync(x => x.Name == vm.Name, ct);
            if (nameTaken) ModelState.AddModelError(nameof(vm.Name), "Et preset med dette navn findes allerede.");
        }

        if (!ModelState.IsValid)
        {
            vm.AllowedTypes = AllowedTypesUniverse;
            vm.OriginalName = entity.Name;
            return View("Edit", vm);
        }

        entity.Name = vm.Name;
        entity.Width = vm.Width;
        entity.Height = vm.Height;
        entity.Types = string.Join(',', vm.Types!.Select(t => t.ToLowerInvariant()));
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        TempData["ok"] = "Preset opdateret.";

        // hvis slug blev ændret, redirect til den nye URL
        var redirectSlug = entity.Name;
        return Redirect($"/media/preset/{Uri.EscapeDataString(redirectSlug)}");
    }

    // ---------- helpers ----------
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

    private static void NormalizeVm(PresetFormVm vm)
    {
        vm.Name = vm.Name.Trim().ToLowerInvariant();
        vm.Types = (vm.Types ?? Array.Empty<string>())
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
