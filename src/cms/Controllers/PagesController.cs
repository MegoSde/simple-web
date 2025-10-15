using System.Text.Json;
using System.Text.Json.Nodes;
using cms.Data;
using cms.Models;
using cms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cms.Controllers;
public class PagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEditorComponentService _editorComponentService;

    public PagesController(ApplicationDbContext db, IEditorComponentService editorComponents)
    {
        _db = db;
        _editorComponentService = editorComponents;
    }

    [HttpGet("/pages")]
    public async Task<IActionResult> Index()
    {
        var nodes = await _db.SiteNodes
            .AsNoTracking()
            .OrderBy(x => x.FullPath)
            .ToListAsync();

        return View(nodes);
    }
    
    // GET /pages/{id}/append
        [HttpGet("/pages/{id:guid}/append")]
        public async Task<IActionResult> Append(Guid id)
        {
            var parent = await _db.SiteNodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (parent == null) return NotFound();

            var templates = await _db.Templates.OrderBy(x => x.Name).ToListAsync();

            var vm = new AddChildPageInput
            {
                ParentFullPath = parent.FullPath,
                ParentTitle = parent.Title,
                InMenu = true,
                InSitemap = true,
                Templates = templates
            };
            return View(vm);
        }

        // POST /pages/{id}/append
        [ValidateAntiForgeryToken]
        [HttpPost("/pages/{id:guid}/append")]
        public async Task<IActionResult> Append(Guid id, AddChildPageInput input)
        {
            var parent = await _db.SiteNodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (parent == null) return NotFound();

            if (!ModelState.IsValid)
            {
                input.ParentFullPath = parent.FullPath;
                input.ParentTitle = parent.Title;
                return View(input);
            }

            var componentJson = await InitJsonAsync(input.TemplateId);

            var addRes = await _db.AddPageResults
                            .FromSqlInterpolated($@"
                    SELECT new_page_id, new_version_no
                    FROM cms.add_page(
                        {parent.FullPath}, {input.Slug}, {input.Title},
                        {input.TemplateId}, {componentJson}
                    )
                ")
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (addRes is null)
            {
                // Giv en klar fejl – her ved vi præcist hvad der skete
                ModelState.AddModelError("", "Kunne ikke oprette siden. add_page returnerede ingen rækker.");
                input.ParentFullPath = parent.FullPath;
                input.ParentTitle = parent.Title;
                return View(input);
            }

            var newPageId = addRes.New_Page_Id;

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT cms.change_settings(
                    {newPageId}, NULL::text, NULL::uuid, {input.InMenu}, {input.InSitemap}
                );
            ");

            return Redirect($"/pages/{newPageId}/edit");
        }
        
        // Helper: byg initial content JSON ud fra template/components
        private async Task<JsonDocument> InitJsonAsync(Guid templateId)
        {
            var template = await _db.Templates.FirstOrDefaultAsync(x => x.Id == templateId);
            if (template == null)
                throw new InvalidOperationException($"Template {templateId} not found.");

            // Forventet format i template.Root: {"components":[{"v":1,"id":"1","type":"hero","settings":{...}}, ...]}
            if (string.IsNullOrWhiteSpace(template.Root))
                return JsonDocument.Parse("""{"components":[]}""");

            JsonObject? rootDef;
            try
            {
                rootDef = JsonNode.Parse(template.Root) as JsonObject;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Template.Root er ikke gyldig JSON.", ex);
            }

            var defComponents = rootDef?["components"] as JsonArray;
            var outComponents = new JsonArray();

            if (defComponents != null)
            {
                foreach (var n in defComponents)
                {
                    if (n is not JsonObject compDef) continue;
                    var type    = compDef["type"]?.GetValue<string>() ?? "";
                    var data = _editorComponentService.InitJson(type) ?? new JsonObject();
                    outComponents.Add(data);
                }
            }
            var doc = new JsonObject
            {
                ["components"] = outComponents
            };
            return JsonDocument.Parse(doc.ToJsonString());
        }
        
        [HttpGet("/pages/{id:guid}/settings")]
        public async Task<IActionResult> Settings(Guid id)
        {
            var node = await _db.SiteNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (node == null) return NotFound();
            
            

            var templates = await _db.Templates.OrderBy(x => x.Name).ToListAsync();
            var currentTemplateName = _db.Templates.FirstOrDefault(t => t.Id == node.LatestTemplateId).Name ?? "(ukendt)";

            var vm = new PageSettingsWm
            {
                Id = node.Id,
                FullPath = node.FullPath,
                Title = node.Title,
                TemplateId = node.LatestTemplateId,
                CurrentTemplateName = currentTemplateName,
                Templates = templates.ToList(),
                InMenu = node.InMenu,
                InSitemap = node.InSitemap
            };
            return View(vm);
        }
        
        [HttpPost("/pages/{id:guid}/settings")]
        public async Task<IActionResult> Settings(Guid id, PageSettingsWm input)
        {
            var node = await _db.SiteNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (node == null) return NotFound();
            if (node.LatestTemplateId == input.TemplateId &&
                (node.InMenu != input.InMenu || node.InSitemap != input.InSitemap))
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    SELECT cms.change_settings(
                        {id}, NULL::text, NULL::uuid, {input.InMenu}, {input.InSitemap}
                    );
                ");
                return Redirect($"/pages");
            }
            
            var templateName = _db.Templates.FirstOrDefault(t => t.Id == input.TemplateId).Name ?? "(ukendt)";
            if (templateName != "(ukendt)" && templateName == input.TemplateConfirm)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    SELECT cms.change_settings(
                        {id}, NULL::text, {input.TemplateId}, {input.InMenu}, {input.InSitemap}
                    );
                ");
                return Redirect($"/pages");
            }
            //Error:
            if(templateName == "(ukendt)" )
                ModelState.AddModelError("", "Kunne finde den angivet template.");
            
            if (templateName != input.TemplateConfirm)
            {
                ModelState.AddModelError("CurrentTemplateName", "Du skal bekræfte at du vil skifte template");
            }

            input.Id = node.Id;
            input.Title = node.Title;
            input.FullPath = node.FullPath;
            input.CurrentTemplateName = _db.Templates.FirstOrDefault(t => t.Id == node.LatestTemplateId).Name ?? "(ukendt)";
            input.Templates = await _db.Templates.OrderBy(x => x.Name).ToListAsync();
            
            return View(input);
        }

        [HttpGet("/pages/{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var node = await _db.SiteNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (node == null) return NotFound();

            var vm = new PageDelete
            {
                Id = node.Id,
                Title = node.Title,
                FullPath = node.FullPath
            };
            return View(vm);
        }

        [HttpPost("/pages/{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id, PageDelete input)
        {
            var node = await _db.SiteNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (node == null) return NotFound();

            input.Id = id;
            input.Title = node.Title;
            input.FullPath = node.FullPath;

            if (string.IsNullOrWhiteSpace(input.FullPathConfirm) ||
                !string.Equals(input.FullPathConfirm.Trim(), node.FullPath, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(input.FullPathConfirm), "Skriv præcist sidens fulde path for at bekræfte sletning.");
                return View(input);
            }

            var res = await _db.DeletePageResults
                .FromSqlInterpolated($@"
            SELECT deleted_page_id, deleted_path, deleted_count
            FROM cms.delete_page({id}, {input.FullPathConfirm})
        ")
                .AsNoTracking()
                .SingleAsync();

            TempData["ok"] = $"Slettede '{res.Deleted_Path}' (inkl. {res.Deleted_Count} node(r)).";
            return Redirect("/pages");
        }

        // GET /pages/{id}/edit (TBDL)
        [HttpGet("/pages/{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var node = await _db.SiteNodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (node == null) return NotFound();

            ViewData["PageTitle"] = node.Title;
            ViewData["PagePath"] = node.FullPath;
            return View(); // simple TBDL
        }
}