using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cms.Controllers;

[AllowAnonymous]
[Route("error")]
public class ErrorController : Controller
{
    [HttpGet("{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code; // behold 404/403/… som status

        // Hvis API (Accept: application/json), så giv ProblemDetails
        if (Request.Headers.Accept.Any(a => a.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            return new ObjectResult(new ProblemDetails {
                Title = code == 404 ? "Not Found" : "Error",
                Status = code,
                Detail = code == 404 ? "The requested resource was not found." : null,
                Extensions = { ["path"] = HttpContext.Items["originalPath"] ?? Request.Path.ToString() }
            }) { StatusCode = code };
        }

        // HTML (view)
        if (code == 404) return View("NotFound");
        return View("Status", code); // generisk fallback, hvis du vil
    }
}