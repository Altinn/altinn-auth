using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.ScopePrefixes;
using Altinn.ResourceRegistry.Core.ServiceOwners;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ResourceRegistryCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers core resource registry services in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddResourceRegistryCoreServices(this IServiceCollection services)
    {
        services.AddAccessListsService();
        services.AddResourceOwnerService();
        services.AddScopePrefixValidator();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IAccessListService"/> in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddAccessListsService(this IServiceCollection services)
    {
        services.TryAddTransient<IAccessListService, AccessListService>();

        return services;
    }

    /// <summary>
    /// Register the <see cref="IServiceOwnerService"/> in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddResourceOwnerService(this IServiceCollection services)
    {
        services.TryAddSingleton<IServiceOwnerService, ServiceOwnerService>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IScopePrefixValidator"/> in the <see cref="IServiceCollection"/>.
    /// It is a singleton so that the cached prefix list is shared by all requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <returns><paramref name="services"/></returns>
    public static IServiceCollection AddScopePrefixValidator(this IServiceCollection services)
    {
        services.TryAddSingleton<IScopePrefixValidator, ScopePrefixValidator>();

        return services;
    }
}
