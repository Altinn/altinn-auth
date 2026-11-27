#nullable enable

using Altinn.Authorization.Integration.Platform.Samarbeidsportalen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.Authorization.Integration.Platform.Extensions;

/// <summary>
/// Registers the Samarbeidsportalen client and the Maskinporten scope prefix validator.
/// </summary>
public static class SamarbeidsportalenServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ISamarbeidsportalenClient"/> and <see cref="IScopePrefixValidator"/>.
    /// Independent of <see cref="ServiceCollectionExtensions.AddAltinnPlatformIntegration"/>,
    /// since the Samarbeidsportalen API needs no platform access token.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="SamarbeidsportalenOptions"/>.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSamarbeidsportalen(this IServiceCollection services, Action<SamarbeidsportalenOptions> configureOptions)
    {
        services.AddOptions<SamarbeidsportalenOptions>()
            .Validate(
                opts => opts.PrefixCacheStaleAge <= opts.PrefixCacheExpiredAge,
                $"'{nameof(SamarbeidsportalenOptions.PrefixCacheStaleAge)}' can't be longer than '{nameof(SamarbeidsportalenOptions.PrefixCacheExpiredAge)}'")
            .Configure(configureOptions);

        services.AddHttpClient(SamarbeidsportalenClient.HttpClientName);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISamarbeidsportalenClient, SamarbeidsportalenClient>();
        services.TryAddSingleton<IScopePrefixValidator, ScopePrefixValidator>();

        return services;
    }
}
