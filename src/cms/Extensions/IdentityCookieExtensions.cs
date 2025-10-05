using cms.Data;
using cms.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace cms.Extensions;

public static class IdentityCookieExtensions
{
    public static IServiceCollection AddIdentityWithCookies(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 6;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireDigit = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme       = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme    = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, o =>
            {
                o.LoginPath = "/login";
                o.AccessDeniedPath = "/login";
                o.Cookie.Name = "simple_web_cms_auth";
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.SlidingExpiration = true;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);

                static bool IsApi(HttpRequest r) =>
                    r.Path.StartsWithSegments("/auth") ||
                    r.Path.StartsWithSegments("/api")  ||
                    (r.Headers.Accept.Any(a => (a ?? "").Contains("application/json",
                        StringComparison.OrdinalIgnoreCase)));

                o.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
                        if (IsApi(ctx.Request)) { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
                        ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = ctx =>
                    {
                        if (IsApi(ctx.Request)) { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
                        ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        
        return services;
    }
    
    public static async Task SeedAdminAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
       
        if (!users.Users.Any())
        {
            var adminRole = await roles.CreateAsync(new ApplicationRole { Name = "Admin" });
            await roles.CreateAsync(new ApplicationRole { Name = "Editor" });
            await roles.CreateAsync(new ApplicationRole { Name = "Developer" });
            
            var adminUser = new ApplicationUser { UserName = "Admin", Email = "admin@example.com", EmailConfirmed = true }; 
            await users.CreateAsync(adminUser, "ChangeThis!123");
            await users.AddToRoleAsync(adminUser, "Admin");
        }
    }
}