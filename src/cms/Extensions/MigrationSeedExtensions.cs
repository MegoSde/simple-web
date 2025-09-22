using cms.Models;
using cms.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace cms.Extensions;


public static class MigrationSeedExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var r in new[] { "Admin", "User" })
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new ApplicationRole { Name = r });

        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var u = await users.FindByNameAsync("admin");
        if (u is null)
        {
            u = new ApplicationUser { UserName = "admin", Email = "admin@example.com", EmailConfirmed = true };
            await users.CreateAsync(u, "Secret123!");
            await users.AddToRolesAsync(u, new[] { "Admin" });
        }
    }
}