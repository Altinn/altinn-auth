using Altinn.Authorization.ServiceDefaults.Npgsql.Yuniql;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Persistence;
using Altinn.ResourceRegistry.Persistence.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding resource registry services to the dependency injection container.
/// </summary>
public static class ResourceRegistryDependencyInjectionExtensions 
{
    /// <summary>
    /// Registers resource registry persistence services with the dependency injection container of a host application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddResourceRegistryPersistence(
        this IHostApplicationBuilder builder)
    {
        builder.AddResourceRegistryRepository();
        builder.AddPartyRegistryRepository();
        builder.AddResourceRegistryPolicyRepository();

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IResourceRegistryRepository"/> with the dependency injection container of a host application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddResourceRegistryRepository(
        this IHostApplicationBuilder builder)
    {
        builder.AddDatabase();

        builder.Services.TryAddTransient<IResourceRegistryRepository, ResourceRegistryRepository>();

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IAccessListsRepository"/> with the dependency injection container of a host application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddPartyRegistryRepository(
        this IHostApplicationBuilder builder)
    {
        builder.AddDatabase();
        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services.TryAddTransient<IAccessListsRepository, AccessListsRepository>();

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IPolicyRepository"/> with the dependency injection container of a host application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddResourceRegistryPolicyRepository(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<AzureStorageConfiguration>()
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryAccountKey), "account key cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryAccountName), "account name cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryContainer), "container cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryBlobEndpoint), "blob endpoint cannot be null or empty");

        builder.Services.TryAddSingleton<IPolicyRepository, PolicyRepository>();

        return builder;
    }

    private static IHostApplicationBuilder AddDatabase(this IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(s => s.ServiceType == typeof(Marker)))
        {
            // already added
            return builder;
        }

        builder.Services.AddSingleton<Marker>();

        var fs = new ManifestEmbeddedFileProvider(typeof(ResourceRegistryDependencyInjectionExtensions).Assembly, "Migration");
        builder.AddAltinnPostgresDataSource()
            .MapEnum<ResourceType>("resourceregistry.resourcetype")
            .AddYuniqlMigrations(cfg =>
            {
                cfg.WorkspaceFileProvider = fs;
                cfg.Workspace = "/";
            });

        return builder;
    }

    private record Marker;
}
