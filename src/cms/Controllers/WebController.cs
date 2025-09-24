using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cms.Controllers;

[Authorize]
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

    [HttpGet("/Media")]
    public IActionResult Media()
    {
        return View();
    }
}