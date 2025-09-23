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
            .AddSignInManager();

        // VIGTIGT: brug Identity.Application som scheme
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
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Prod: kræv HTTPS
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.SlidingExpiration = true;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                
                static bool IsApi(HttpRequest r) =>
                    r.Path.StartsWithSegments("/auth") ||
                    r.Path.StartsWithSegments("/api")  ||
                    (r.Headers.Accept.Any(a => a.Contains("application/json", StringComparison.OrdinalIgnoreCase)));

                // API: returnér 401/403 i stedet for redirects
                o.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
                        if (IsApi(ctx.Request))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }
                        ctx.Response.Redirect(ctx.RedirectUri);
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = ctx =>
                    {
                        if (IsApi(ctx.Request))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                        ctx.Response.Redirect(ctx.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
