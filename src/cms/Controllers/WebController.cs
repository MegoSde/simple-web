using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cms.Controllers;

[Authorize]
[AllowAnonymous]
public class WebController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // Viser Views/Web/Index.cshtml
        return View();
    }

    [HttpGet("/Pages")]
    public IActionResult Pages()
    {

        return View();
    }
}