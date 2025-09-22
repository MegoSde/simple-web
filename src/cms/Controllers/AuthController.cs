using cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cms.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public AuthController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    public record LoginRequest(string Username, string Password, bool RememberMe = false);

    /// <summary>Login – sætter auth-cookie ved succes.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest body)
    {
        var result = await _signIn.PasswordSignInAsync(
            body.Username, body.Password,
            isPersistent: body.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid username or password" });

        // Cookie er nu sat i svaret (Set-Cookie header)
        return Ok(new { ok = true });
    }

    /// <summary>Nuværende bruger (kræver login).</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var name = User.Identity?.Name;
        if (string.IsNullOrEmpty(name)) return Unauthorized();

        var user = await _users.FindByNameAsync(name);
        if (user is null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new { user = user.UserName, roles });
    }

    /// <summary>Log ud (rydder cookie)</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new { ok = true });
    }
}