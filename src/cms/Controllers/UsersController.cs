using cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cms.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public UsersController(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    // LIST
    [HttpGet("/users")]
    public async Task<IActionResult> Index()
    {
        var list = await _users.Users.AsNoTracking()
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToListAsync();

        var model = new List<UserRow>(list.Count);
        foreach (var u in list)
        {
            var rs = await _users.GetRolesAsync(new ApplicationUser { Id = u.Id, UserName = u.UserName, Email = u.Email });
            model.Add(new UserRow(u.Id, u.UserName ?? "", u.Email ?? "", rs.ToArray()));
        }
        return View(model);
    }

    // CREATE
    [HttpGet("/users/create")]
    public async Task<IActionResult> Create()
    {
        var allRoles = await _roles.Roles.Select(r => r.Name!).ToListAsync();
        return View(new UserCreateVm { AllRoles = allRoles });
    }

    [HttpPost("/users/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UserCreateVm vm)
    {
        var allRoles = await _roles.Roles.Select(r => r.Name!).ToListAsync();
        vm.AllRoles = allRoles;

        if (string.IsNullOrWhiteSpace(vm.UserName))
            ModelState.AddModelError(nameof(vm.UserName), "Username er påkrævet.");
        if (string.IsNullOrWhiteSpace(vm.Email))
            ModelState.AddModelError(nameof(vm.Email), "Email er påkrævet.");
        if (string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError(nameof(vm.Password), "Password er påkrævet.");
        if (vm.Password != vm.ConfirmPassword)
            ModelState.AddModelError(nameof(vm.ConfirmPassword), "Passwords er ikke ens.");

        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser { UserName = vm.UserName.Trim(), Email = vm.Email.Trim(), EmailConfirmed = true };
        var result = await _users.CreateAsync(user, vm.Password!);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }

        var selected = (vm.SelectedRoles ?? Array.Empty<string>()).Intersect(allRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (selected.Length > 0)
            await _users.AddToRolesAsync(user, selected);

        return Redirect("/users");
    }

    // EDIT
    [HttpGet("/users/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var allRoles = await _roles.Roles.Select(r => r.Name!).ToListAsync();
        var userRoles = await _users.GetRolesAsync(user);

        var vm = new UserEditVm
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            SelectedRoles = userRoles.ToArray(),
            AllRoles = allRoles
        };
        return View(vm);
    }

    [HttpPost("/users/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [FromForm] UserEditVm vm)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        vm.AllRoles = await _roles.Roles.Select(r => r.Name!).ToListAsync();

        if (string.IsNullOrWhiteSpace(vm.UserName))
            ModelState.AddModelError(nameof(vm.UserName), "Username er påkrævet.");
        if (string.IsNullOrWhiteSpace(vm.Email))
            ModelState.AddModelError(nameof(vm.Email), "Email er påkrævet.");

        // Unik email check (hvis ønsket)
        var existing = await _users.FindByEmailAsync(vm.Email);
        if (existing is not null && existing.Id != user.Id)
            ModelState.AddModelError(nameof(vm.Email), "Email er allerede i brug.");

        if (!ModelState.IsValid) return View(vm);

        // Opdater basale felter
        user.UserName = vm.UserName.Trim();
        user.Email = vm.Email.Trim();
        var updateRes = await _users.UpdateAsync(user);
        if (!updateRes.Succeeded)
        {
            foreach (var e in updateRes.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }

        // Roller (diff: add/remove)
        var currentRoles = await _users.GetRolesAsync(user);
        var wanted = (vm.SelectedRoles ?? Array.Empty<string>())
            .Intersect(vm.AllRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        // Beskyt mod at fjerne Admin fra sig selv
        var isSelf = string.Equals(User.Identity?.Name, user.UserName, StringComparison.OrdinalIgnoreCase);
        if (isSelf && currentRoles.Contains("Admin") && !wanted.Contains("Admin"))
        {
            ModelState.AddModelError(string.Empty, "Du kan ikke fjerne din egen Admin-rolle.");
            vm.SelectedRoles = currentRoles.ToArray();
            return View(vm);
        }

        var toAdd = wanted.Except(currentRoles).ToArray();
        var toRemove = currentRoles.Except(wanted).ToArray();
        if (toAdd.Length > 0) await _users.AddToRolesAsync(user, toAdd);
        if (toRemove.Length > 0) await _users.RemoveFromRolesAsync(user, toRemove);

        // Skift adgangskode (valgfrit)
        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            if (vm.NewPassword != vm.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Passwords er ikke ens.");
                return View(vm);
            }
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var pwdRes = await _users.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!pwdRes.Succeeded)
            {
                foreach (var e in pwdRes.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }
        }

        return Redirect("/users");
    }

    // --- View models ---
    public record UserRow(string Id, string UserName, string Email, string[] Roles);

    public class UserCreateVm
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string[]? SelectedRoles { get; set; }
        public List<string> AllRoles { get; set; } = new();
    }

    public class UserEditVm
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string[]? SelectedRoles { get; set; }
        public List<string> AllRoles { get; set; } = new();

        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
