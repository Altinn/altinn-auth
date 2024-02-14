#nullable enable

using Altinn.ResourceRegistry.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ResourceRegistryAuthAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the authorization handlers for resource registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddResourceRegistryAuthorizationHandlers(this IServiceCollection services)
    {
        services.AddOwnedResourceAuthorizationHandler();
        services.AddAdminAuthorizationHandler();

        return services;
    }

    /// <summary>
    /// Adds the authorization handlers for resource registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddOwnedResourceAuthorizationHandler(this IServiceCollection services)
    {
        services.TryAddAuthroizationHandler<OwnedResourceAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Adds the authorization handlers for resource registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddAdminAuthorizationHandler(this IServiceCollection services)
    {
        services.TryAddAuthroizationHandler<AdminAuthorizationHandler>();

        return services;
    }

    private static IServiceCollection TryAddAuthroizationHandler<T>(this IServiceCollection services)
        where T : class, IAuthorizationHandler
    {
        var existing = services.FirstOrDefault(s => s.ServiceType == typeof(IAuthorizationHandler) && s.ImplementationType == typeof(T));
        if (existing is null)
        {
            services.AddSingleton<IAuthorizationHandler, T>();
        }

        return services;
    }
}
