using Altinn.Authorization.Hosting.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Altinn.Authorization.Hosting.Extensions;

/// <summary>
/// Provides extension methods for configuring the Altinn host defaults in an ASP.NET Core application.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the Altinn host defaults by adding health check services.
    /// </summary>
    /// <param name="builder">The instance of <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance for method chaining.</returns>
    public static IHostApplicationBuilder AddAltinnHostDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<AliveProbe>("Alive");

        return builder;
    }
}
