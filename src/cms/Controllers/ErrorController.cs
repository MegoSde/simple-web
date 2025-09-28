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
        Response.StatusCode = code; // behold 404/403/… som status'
        
        var wantsJson =
            Request.GetTypedHeaders().Accept.Any(mt =>
                // mt.MediaType er en StringSegment
                mt.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                || (mt.Suffix.HasValue && mt.Suffix.Value.Equals("json", StringComparison.OrdinalIgnoreCase)) // application/*+json
                || mt.MediaType.ToString().EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            );

        if (wantsJson)
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