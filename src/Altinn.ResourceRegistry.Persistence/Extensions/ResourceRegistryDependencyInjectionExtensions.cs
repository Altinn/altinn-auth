using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Persistence;
using Altinn.ResourceRegistry.Persistence.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding resource registry services to the dependency injection container.
/// </summary>
public static class ResourceRegistryDependencyInjectionExtensions 
{
    /// <summary>
    /// Registers resource registry persistence services with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="services"/> for further chaining.</returns>
    public static IServiceCollection AddResourceRegistryPersistence(
        this IServiceCollection services)
    {
        services.AddResourceRegistryRepository();
        services.AddResourceRegistryPolicyRepository();

        return services;
    }

    /// <summary>
    /// Registers a <see cref="IResourceRegistryRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="services"/> for further chaining.</returns>
    public static IServiceCollection AddResourceRegistryRepository(
        this IServiceCollection services)
    {
        services.AddOptions<PostgreSQLSettings>()
            .Validate(s => !string.IsNullOrEmpty(s.ConnectionString), "connection string cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.AuthorizationDbPwd), "connection string password be null or empty");

        services.TryAddSingleton((IServiceProvider sp) =>
        {
            var settings = sp.GetRequiredService<IOptions<PostgreSQLSettings>>().Value;
            var connectionString = string.Format(
                settings.ConnectionString,
                settings.AuthorizationDbPwd);

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            builder.MapEnum<ResourceType>("resourceregistry.resourcetype");
            return builder.Build();
        });

        services.TryAddTransient((IServiceProvider sp) => sp.GetRequiredService<NpgsqlDataSource>().CreateConnection());
        services.TryAddTransient<IResourceRegistryRepository, ResourceRegistryRepository>();

        return services;
    }

    /// <summary>
    /// Registers a <see cref="IPolicyRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="services"/> for further chaining.</returns>
    public static IServiceCollection AddResourceRegistryPolicyRepository(
        this IServiceCollection services)
    {
        services.AddOptions<AzureStorageConfiguration>()
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryAccountKey), "account key cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryAccountName), "account name cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryContainer), "container cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryBlobEndpoint), "blob endpoint cannot be null or empty");

        services.TryAddSingleton<IPolicyRepository, PolicyRepository>();

        return services;
    }
}
