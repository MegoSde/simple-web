// Controllers/MediaController.cs

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using cms.Data;
using cms.Services;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

[ApiController]
public class MediaController : Controller {
  private readonly MediaService _svc;
  private readonly ApplicationDbContext _db;
  private readonly IAmazonS3 _s3;
  private readonly IConfiguration _cfg;
  private readonly ILogger<MediaController> _logger;

  public MediaController(MediaService svc, ApplicationDbContext db, IAmazonS3 s3, IConfiguration cfg, ILogger<MediaController> logger)
  {
    _svc = svc; _db = db; _s3 = s3; _cfg = cfg; _logger = logger;
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

    // Hvis browser (Accept: text/html) => vis view
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
  public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? alt_text, [FromForm] string? meta, [FromForm] string? expect_html, CancellationToken ct)
  {
      if (file is null || file.Length == 0)
          return expect_html == "1" ? RedirectWithTemp("Ingen fil valgt.", "/media/new", error:true)
              : Problem(statusCode:400, title:"missing_file", detail:"Ingen fil modtaget.");

      try
      {
          var mime = file.ContentType?.ToLowerInvariant() ?? "application/octet-stream";
          var uploadedBy = User?.Identity?.Name ?? "system";

          using var ms = new MemoryStream();
          await file.OpenReadStream().CopyToAsync(ms, ct);
          ms.Position = 0;

          var res = await _svc.UploadAsync(ms, file.FileName, mime, alt_text, uploadedBy, meta, ct);

          if (expect_html == "1") return RedirectWithTemp("Billede uploadet.", "/media");
          return Ok(res);
      }
      catch (ApiError ex)
      {
          return expect_html == "1" ? RedirectWithTemp(ex.Message, "/media/new", error:true)
              : Problem(statusCode: ex.StatusCode, title: ex.Code, detail: ex.Message);
      }
      catch (SixLabors.ImageSharp.UnknownImageFormatException)
      {
          return expect_html == "1" ? RedirectWithTemp("Filen er ikke et understøttet billede.", "/media/new", error:true)
              : Problem(statusCode:415, title:"unsupported_mime", detail:"Filen er ikke et understøttet billede.");
      }

      IActionResult RedirectWithTemp(string msg, string to, bool error=false)
      { if (error) TempData["error"]=msg; else TempData["ok"]=msg; return Redirect(to); }
  }
}
