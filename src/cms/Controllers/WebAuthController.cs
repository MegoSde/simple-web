using cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cms.Controllers;

public class WebAuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;

    public WebAuthController(SignInManager<ApplicationUser> signIn)
    {
        _signIn = signIn;
    }

    [HttpGet("/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    public record LoginVm(string Username, string Password, bool RememberMe = false, string? ReturnUrl = null);

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPost([FromForm] LoginVm vm)
    {
        var result = await _signIn.PasswordSignInAsync(vm.Username, vm.Password, vm.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Ugyldigt brugernavn eller password");
            ViewData["ReturnUrl"] = vm.ReturnUrl;
            return View("Login", vm);
        }

        // Redirect til lokal returnUrl eller /
        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        return Redirect("/");
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Redirect("/login");
    }
}