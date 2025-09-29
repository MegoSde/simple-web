using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using cms.Data;
using cms.Models;
using cms.Services;
using cms.Common.Errors;
using Microsoft.EntityFrameworkCore;

namespace cms.Controllers;

[ApiController]
[Authorize]
public class MediaController : Controller {
  private readonly MediaService _svc;
  private readonly ApplicationDbContext _db;

  public MediaController(MediaService svc, ApplicationDbContext db)
  {
    _svc = svc; _db = db;
  }

  // GET /media  -> returnerer HTML (browser) eller JSON (API) afhængigt af Accept/form
  [HttpGet("/media")]
  public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 24)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var q = _db.MediaAssets.AsNoTracking().OrderByDescending(x => x.CreatedAt);
    var total = await q.CountAsync();
    var items = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new MediaListItem(
      x.Id, x.Hash, x.OriginalUrl, x.Width, x.Height, x.Mime, x.Bytes, x.AltText, x.CreatedAt
    )).ToListAsync();

    var model = new PagedMediaResponse(items, page, pageSize, total);
    
    var wantsHtml =
      Request.Headers.TryGetValue("Accept", out var acc) &&
      acc.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    if (wantsHtml) return View("Index", model);

    return Ok(model); // JSON fallback
  }
  
  // GET /media/new -> HTML form
  [HttpGet("/media/new")]
  public IActionResult New()
  {
    return View("New");
  }
  
  // POST /media/new -> JSON som default; hvis form (expect_html=1) => redirect til /media
  [HttpPost("/media/new")]
  [RequestSizeLimit(20_000_000)]
  public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? altText, [FromForm] string? meta, [FromForm] string? expectHtml, CancellationToken ct)
  {
      if ( file.Length == 0)
          return expectHtml == "1" ? RedirectWithTemp("Ingen fil valgt.", "/media/new", error:true)
              : Problem(statusCode:400, title:"missing_file", detail:"Ingen fil modtaget.");

      try
      {
          var mime = file.ContentType.ToLowerInvariant();
          var uploadedBy = User.Identity?.Name ?? "system";

          using var ms = new MemoryStream();
          await file.OpenReadStream().CopyToAsync(ms, ct);
          ms.Position = 0;

          var res = await _svc.UploadAsync(ms, file.FileName, mime, altText, uploadedBy, meta, ct);

          if (expectHtml == "1") return RedirectWithTemp("Billede uploadet.", "/media");
          return Ok(res);
      }
      catch (ApiError ex)
      {
          return expectHtml == "1" ? RedirectWithTemp(ex.Message, "/media/new", error:true)
              : Problem(statusCode: ex.StatusCode, title: ex.Code, detail: ex.Message);
      }
      catch (SixLabors.ImageSharp.UnknownImageFormatException)
      {
          return expectHtml == "1" ? RedirectWithTemp("Filen er ikke et understøttet billede.", "/media/new", error:true)
              : Problem(statusCode:415, title:"unsupported_mime", detail:"Filen er ikke et understøttet billede.");
      }

      IActionResult RedirectWithTemp(string msg, string to, bool error=false)
      { if (error) TempData["error"]=msg; else TempData["ok"]=msg; return Redirect(to); }
  }
  
  // GET /media/{aa}/{bb}/{hash}
  [HttpGet("/media/{a:length(2)}/{b:length(2)}/{hash:length(64)}")]
  public async Task<IActionResult> Editor([FromRoute] string a, [FromRoute] string b, [FromRoute] string hash, CancellationToken ct)
  {
      var asset = await _db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Hash == hash, ct);
      if (asset is null) return NotFound();

      var presets = await _db.MediaPresets.AsNoTracking()
          .OrderBy(x => x.RatioKey).ThenBy(x => x.Name)
          .ToListAsync(ct);

      var crops = await _db.MediaAssetCrops.AsNoTracking()
          .Where(x => x.AssetHash == hash)
          .ToListAsync(ct);

      var cropMap = crops.ToDictionary(c => c.PresetName, c => new[] { c.X, c.Y, c.W, c.H });

      var groups = presets
          .GroupBy(p => p.RatioKey)
          .Select(g => new RatioGroupVm { RatioKey = g.Key, Count = g.Count() })
          .OrderBy(g => g.RatioKey == "free" ? 1 : 0) // "free" til sidst
          .ThenBy(g => g.RatioKey)
          .ToList();

      var vm = new CropEditorVm
      {
          Hash = hash,
          A = a, B = b,
          WorkSrc = $"/cmsimg/work/{a}/{b}/{hash}.webp",
          Presets = presets.Select(p => new CropPresetVm
          {
              Name = p.Name,
              Width = p.Width,
              Height = p.Height,
              RatioKey = p.RatioKey,
              Types = p.Types,
              Existing = cropMap.TryGetValue(p.Name, out var rect) ? rect : null
          }).ToList(),
          Groups = groups
      };

      return View("Editor", vm);
  }
  
  // POST /media/crop/{preset}/{hash}  -> gem EN crop
    [HttpPost("/media/crop/{preset}/{hash:length(64)}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromRoute] string preset, [FromRoute] string hash,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double x,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double y,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double w,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double h,
        CancellationToken ct)
    {
        if (!ValidRect(x, y, w, h)) return BadRequest(new { error = "invalid_rect" });
        var existsPreset = await _db.MediaPresets.AnyAsync(p => p.Name == preset, ct);
        if (!existsPreset) return NotFound(new { error = "preset_not_found" });

        var user = User.Identity?.Name ?? "system";
        var entity = await _db.MediaAssetCrops.FirstOrDefaultAsync(c => c.AssetHash == hash && c.PresetName == preset, ct);
        if (entity is null)
        {
            entity = new MediaAssetCrop
            {
                Id = Guid.NewGuid(),
                AssetHash = hash,
                PresetName = preset,
                X = x, Y = y, W = w, H = h,
                UpdatedBy = user, UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.MediaAssetCrops.Add(entity);
        }
        else
        {
            entity.X = x; entity.Y = y; entity.W = w; entity.H = h;
            entity.UpdatedBy = user; entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    // POST /media/crop-group/{ratioKey}/{hash}  -> gem SAMME crop for ALLE presets i ratio
    [HttpPost("/media/crop-group/{ratioKey}/{hash:length(64)}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGroup([FromRoute] string ratioKey, [FromRoute] string hash,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double x,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double y,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double w,
        [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))] double h,
        CancellationToken ct)
    {
        if (!ValidRect(x, y, w, h)) return BadRequest(new { error = "invalid_rect" });

        var targetPresets = await _db.MediaPresets
            .Where(p => p.RatioKey == ratioKey)
            .Select(p => p.Name)
            .ToListAsync(ct);

        if (targetPresets.Count == 0) return NotFound(new { error = "ratio_not_found" });

        var user = User.Identity?.Name ?? "system";

        // Hent eksisterende crops for (hash, alle presets i ratio)
        var existing = await _db.MediaAssetCrops
            .Where(c => c.AssetHash == hash && targetPresets.Contains(c.PresetName))
            .ToListAsync(ct);

        var existingMap = existing.ToDictionary(c => c.PresetName, c => c);

        foreach (var preset in targetPresets)
        {
            if (!existingMap.TryGetValue(preset, out var ent))
            {
                ent = new MediaAssetCrop
                {
                    Id = Guid.NewGuid(),
                    AssetHash = hash,
                    PresetName = preset,
                    X = x, Y = y, W = w, H = h,
                    UpdatedBy = user, UpdatedAt = DateTimeOffset.UtcNow
                };
                _db.MediaAssetCrops.Add(ent);
            }
            else
            {
                ent.X = x; ent.Y = y; ent.W = w; ent.H = h;
                ent.UpdatedBy = user; ent.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, updated = targetPresets.Count });
    }
    
  private static bool ValidRect(double x, double y, double w, double h) =>
      x >= 0 && y >= 0 && w > 0 && h > 0 && x <= 1 && y <= 1 && x + w <= 1.000001 && y + h <= 1.000001;

  
}
