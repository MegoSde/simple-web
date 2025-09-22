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
}