using cms.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace cms.Extensions;

public static class HealthSetupExtensions
{
    /// <summary>
    /// Tilføjer controllers og health checks til DI-containeren.
    /// Holdes som extension for at holde Program.cs minimal.
    /// </summary>
    public static WebApplicationBuilder AddAppHealth(this WebApplicationBuilder builder)
    {
        // En simpel "self" check som altid er Healthy (til liveness)
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddDbContextCheck<ApplicationDbContext>(
                name: "postgres",
                customTestQuery: (ctx, ct) => ctx.Database.CanConnectAsync(ct),
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "db" });

        return builder;
    }
}
