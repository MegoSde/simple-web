namespace cms.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[ApiController]
[Route("health")]
public class HealthController(HealthCheckService health) : ControllerBase
{
    /// <summary>
    /// Liveness: returnerer 200 OK, så længe processen kører.
    /// </summary>
    [HttpGet("live")]
    public async Task<IActionResult> Live()
    {
        var report = await health.CheckHealthAsync(r => r.Name == "self");
        return report.Status == HealthStatus.Healthy
            ? Ok(new { status = "Healthy" })
            : StatusCode(503, new { status = report.Status.ToString() });
    }
    
    /// <summary>
    /// Readiness – inkluderer database
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Ready()
    {
        var report = await health.CheckHealthAsync(r => r.Tags.Contains("ready"));

        // Lavt, men læsbart JSON-resumé inkl. individuelle checks
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds
            })
        };

        return report.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
    }
}