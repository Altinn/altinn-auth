using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Integration;
using Altinn.ResourceRegistry.Integration.Clients;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods <see cref="IServiceCollection"/>.
/// </summary>
public static class ResourceRegistryIntegrationDependencyInjectionExtensions
{
    /// <summary>
    /// Add the register client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>.</returns>
    public static IServiceCollection AddAltinnRegisterClient(this IServiceCollection services)
    {
        services.AddOptions<RegisterClientOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddTransient<PlatformAccessTokenHandler>();
        services.AddHttpClient<IRegisterClient, RegisterClient>()
            .ConfigureHttpClient(static (s, client) =>
            {
                var options = s.GetRequiredService<IOptions<RegisterClientOptions>>().Value;
                
                client.BaseAddress = options.Uri;
            })
            .AddHttpMessageHandler<PlatformAccessTokenHandler>();

        return services;
    }
}
