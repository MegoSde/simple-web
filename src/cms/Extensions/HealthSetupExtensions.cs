namespace cms.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
            .AddCheck("self", () => HealthCheckResult.Healthy());

        return builder;
    }
}
