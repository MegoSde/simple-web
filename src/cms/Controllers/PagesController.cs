using System.Text.Json;
using System.Text.Json.Nodes;
using cms.Common;
using cms.Data;
using cms.Models;
using cms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

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
                    var settings = compDef["settings"]?.AsObject()  ?? new JsonObject();
                    data.Add("settings", settings.DeepClone());
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
            var templateName = _db.Templates.FirstOrDefault(t => t.Id == input.TemplateId).Name ?? "(ukendt)";
            
            if(templateName == "(ukendt)" )
                ModelState.AddModelError("", "Kunne finde den angivet template.");
            if (templateName != input.TemplateConfirm)
                ModelState.AddModelError("CurrentTemplateName", "Du skal bekræfte at du vil skifte template");

            if (ModelState.ErrorCount > 0)
            {
                input.Id = node.Id;
                input.Title = node.Title;
                input.FullPath = node.FullPath;
                input.CurrentTemplateName = _db.Templates.FirstOrDefault(t => t.Id == node.LatestTemplateId).Name ?? "(ukendt)";
                input.Templates = await _db.Templates.OrderBy(x => x.Name).ToListAsync();

                return View(input);
            }

            if (node.InMenu != input.InMenu || node.InSitemap != input.InSitemap)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    SELECT cms.change_settings(
                        {id}, NULL::text, NULL::uuid, {input.InMenu}, {input.InSitemap}
                    );
                ");
            }

            if (templateName != "(ukendt)" && templateName == input.TemplateConfirm)
            {
                var componentJson = await InitJsonAsync(input.TemplateId);
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    SELECT cms.save_page(
                        {id}, {componentJson}, {input.TemplateId}
                    );
                ");
            }
            return Redirect($"/pages");
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
        [HttpGet("/pages/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var node = await _db.SiteNodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (node == null) return NotFound();

            ViewData["PageTitle"] = node.Title;
            ViewData["PagePath"] = node.FullPath;
            ViewData["PageId"] = node.Id;
            return View(); // simple TBDL
        }
        
        [HttpGet("/pages/{id:guid}.json")]
        public async Task<IActionResult> GetJson([FromRoute] Guid id, CancellationToken ct)
        {
            // Tjek at siden findes (brug din SiteNodes som du allerede gør)
            var node = await _db.SiteNodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (node == null) return NotFound(new { error = "PAGE_NOT_FOUND" });

            // Kald DB-funktionen som returnerer JSON
            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "select cms.cms_get_page_for_edit(@p_page_id, @p_version)::text";
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "p_page_id";
            p1.Value = id;
            if (p1 is NpgsqlParameter np1) np1.NpgsqlDbType = NpgsqlDbType.Uuid;
            cmd.Parameters.Add(p1);

            var p2 = cmd.CreateParameter();
            p2.ParameterName = "p_version";
            p2.Value = DBNull.Value;
            if (p2 is NpgsqlParameter np2) np2.NpgsqlDbType = NpgsqlDbType.Text;
            cmd.Parameters.Add(p2);

            try
            {
                var json = (string?)await cmd.ExecuteScalarAsync(ct);
                if (string.IsNullOrWhiteSpace(json))
                    return NotFound(new { error = "NOT_FOUND" });

                // Returnér rå JSON fra DB (allerede valid)
                return Content(json, "application/json; charset=utf-8");
            }
            catch (PostgresException ex) when (ex.SqlState == "P0001")
            {
                // Mappér RAISE EXCEPTION fra funktionen til 404
                return NotFound(new { error = ex.MessageText });
            }
        }
        
        [ValidateAntiForgeryToken]
        [HttpPost("/pages/{id:guid}.json")]
        public async Task<IActionResult> SavePageJson(
            [FromRoute] Guid id,
            [FromBody] SavePayload payload,
            CancellationToken ct)
        {
            if (id != payload.PageId)
                return BadRequest(new { error = "PAGE_ID_MISMATCH" });

            // 1) Find siden + template-id
            var node = await _db.SiteNodes
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Id, x.LatestTemplateId })
                .FirstOrDefaultAsync(ct);

            if (node is null) return NotFound(new { error = "PAGE_NOT_FOUND" });

            // 2) Hent seneste version_no og template.definition
            Guid templateId = node.LatestTemplateId;

            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            int? latestVersion = null;
            JsonObject? tplDef = null;

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    select pv.version_no as latest_version, t.root as template_def_json
                    from cms.page_versions pv
                    inner join cms.templates t on (pv.template_id = t.id)
                    where page_id = @p_page_id
                    order by pv.version_no desc
                    limit 1;
                ";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "p_page_id"; p1.Value = id; cmd.Parameters.Add(p1);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    latestVersion = reader.IsDBNull(0) ? null : reader.GetInt32(0);

                    if (!reader.IsDBNull(1))
                    {
                        var tplText = reader.GetString(1);
                        var nodeJson = JsonNode.Parse(tplText);
                        tplDef = nodeJson?.AsObject();
                    }
                }
            }

            if (latestVersion is null)
                return NotFound(new { error = "VERSION_NOT_FOUND" }); // siden har ingen versioner endnu

            // 3) Optimistic concurrency: kræv at klientens version matcher seneste
            if (payload.Version != latestVersion.Value)
                return Conflict(new { error = "VERSION_CONFLICT", serverVersion = latestVersion.Value });

            // 4) Udtræk defaults pr. component-type fra template.definition
            // Forventet struktur: { "components": [ { "type": "hero", "settings": { ... } }, ... ] }
            var defaultsByType = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
            var tplComponents = tplDef?["components"] as JsonArray;
            if (tplComponents is not null)
            {
                foreach (var n in tplComponents)
                {
                    var o = n as JsonObject;
                    var t = o?["type"]?.GetValue<string>();
                    var s = o?["settings"] as JsonObject;
                    if (!string.IsNullOrWhiteSpace(t) && s is not null)
                    {
                        defaultsByType[t!] = s;
                    }
                }
            }

            // 5) Indsæt/merge settings ind i hver component
            foreach (var comp in payload.Content.Components)
            {
                comp.Settings ??= new JsonObject();
                if (defaultsByType.TryGetValue(comp.Type, out var defSettings))
                {
                    var cloneDefaults = JsonHelpers.DeepClone(defSettings);
                    // Merge kun manglende nøgler – clientens settings vinder, hvis den skulle have nogen
                    JsonHelpers.MergeMissing(comp.Settings, cloneDefaults);
                }
            }

            // 6) Byg det content-JSON der skal gemmes i DB
            var contentToStore = new JsonObject
            {
                ["components"] = new JsonArray(payload.Content.Components.Select(c =>
                {
                    var obj = new JsonObject
                    {
                        ["v"]      = c.V,
                        ["type"]   = c.Type,
                        ["data"]   = c.Data ?? new JsonObject(),
                        ["settings"] = c.Settings ?? new JsonObject()
                    };
                    return obj;
                }).ToArray()),
                ["settings"] = payload.Content.Settings is null
                    ? new JsonObject()
                    : JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(payload.Content.Settings))!.AsObject()
            };

            // 7) Gem via stored procedure cms.save_page (…) som opretter ny version_no
            Guid? versionId = null;
            int? newVersionNo = null;

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    select version_id, version_no
                    from cms.save_page(@p_page_id, @p_content::jsonb, @p_template_id)
                ";

                var p1 = cmd.CreateParameter(); p1.ParameterName = "p_page_id";     p1.Value = id;           cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "p_content";     p2.Value = contentToStore.ToJsonString(); cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.ParameterName = "p_template_id"; p3.Value = templateId;   cmd.Parameters.Add(p3);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    versionId   = !reader.IsDBNull(0) ? reader.GetGuid(0) : null;
                    newVersionNo = !reader.IsDBNull(1) ? reader.GetInt32(1) : (int?)null;
                }
            }

            if (newVersionNo is null)
                return StatusCode(500, new { error = "SAVE_FAILED" });

            return Ok(new { ok = true, pageId = id, version = newVersionNo, versionId });
        }
        
        [ValidateAntiForgeryToken]
        [HttpPost("/pages/{id:guid}/publish")]
        public async Task<IActionResult> Publish(
            [FromRoute] Guid id,
            [FromQuery] string? v,
            CancellationToken ct)
        {
            // 1) Find version_no (seneste hvis v er tom)
            int versionNo;
            if (string.IsNullOrWhiteSpace(v))
            {
                
                var node = await _db.SiteNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (node is null)
                    return NotFound(new { error = "PAGE_not_FOUND" });
                var latest = node?.LatestVersionNo;
                
                if (latest is null)
                    return NotFound(new { error = "NO_VERSIONS_FOUND" });

                versionNo = latest.Value;
            }
            else if (!int.TryParse(v, out versionNo))
            {
                return BadRequest(new { error = "INVALID_VERSION" });
            }

            // 2) Kald SP: cms.publish_page(page_id, version_no)
            await using var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "select cms.publish_page(@p_page_id, @p_version_no)";
            var p1 = cmd.CreateParameter(); p1.ParameterName = "p_page_id"; p1.Value = id; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "p_version_no"; p2.Value = versionNo; cmd.Parameters.Add(p2);

            try
            {
                await cmd.ExecuteScalarAsync(ct);
            }
            catch (PostgresException ex)
            {
                return UnprocessableEntity(new { error = ex.MessageText, code = ex.SqlState });
            }

            return Redirect($"/pages/{id}");
        }
        
        [HttpGet("/pages/editor.js")]
        public IActionResult EditorJs()
            => Content(_editorComponentService.GetEditorJavascript(), "application/javascript; charset=utf-8");

        [HttpGet("/pages/editor.{hash}.js")]
        public IActionResult EditorJsHashed([FromRoute] string hash)
            => hash.Equals(_editorComponentService.GetEditorHash(), StringComparison.OrdinalIgnoreCase)
                ? Content(_editorComponentService.GetEditorJavascript(), "application/javascript; charset=utf-8")
                : NotFound();
}