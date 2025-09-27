using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using cms.Data;
using cms.Models;
using Microsoft.AspNetCore.Identity;

namespace cms.Extensions;

public static class MigrationSeedExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MigrateSeed");

        try
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();

            // 1) Kør migrations (safe ved gentagelser)
            await db.Database.MigrateAsync();

            // 2) Seed idempotent
            await SeedIdentityAsync(sp, logger);
            await SeedMediaAsync(db, logger);

            logger.LogInformation("Migrations + seed completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migrations/seed failed.");
            throw; // lad appen fejle hard hvis ønsket
        }
    }

    private static async Task SeedIdentityAsync(IServiceProvider sp, ILogger logger)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // Eksempel: seed rolle "admin" kun hvis den ikke findes
        const string roleName = "admin";
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new ApplicationRole { Name = roleName };
            var r = await roleManager.CreateAsync(role);
            if (!r.Succeeded) logger.LogWarning("Could not create role {Role}: {Err}", roleName, string.Join(",", r.Errors.Select(e => e.Description)));
        }

        // Eksempel: seed admin-bruger kun hvis den ikke findes
        const string adminEmail = "admin@example.com";
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user is null)
        {
            user = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var create = await userManager.CreateAsync(user, "ChangeThis!123");
            if (!create.Succeeded)
                logger.LogWarning("Could not create admin user: {Err}", string.Join(",", create.Errors.Select(e => e.Description)));
            else
                await userManager.AddToRoleAsync(user, roleName);
        }
    }

    private static async Task SeedMediaAsync(ApplicationDbContext db, ILogger logger)
    {
        // Kun et eksempel—seed kun hvis tom tabel.
        // Hvis du *altid* vil sikre en bestemt række, så tjek "Findes hash?" før insert.
        if (await db.MediaAssets.AnyAsync()) return;

        db.MediaAssets.Add(new MediaAsset
        {
            Hash = "dummyhash",
            OriginalUrl = "/originals/du/mmy/dummyhash.jpg",
            Mime = "image/jpeg",
            Width = 1,
            Height = 1,
            Bytes = 123,
            UploadedBy = "system",
            AltText = "Seed example",
            Meta = "{}"
        });
        await db.SaveChangesAsync();
    }
}
