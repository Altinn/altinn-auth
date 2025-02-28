using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Altinn.Authorization.Host.Database;

/// <summary>
/// Provides a factory for creating PostgreSQL database connections within the Altinn authorization host.
/// </summary>
internal class AltinnHostDatabaseFactory(IServiceProvider serviceProvider) : IAltinnDatabase
{
    /// <summary>
    /// The service provider used to resolve dependencies.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <inheritdoc/>
    public NpgsqlConnection CreatePgsqlConnection(SourceType sourceType)
    {
        if (ServiceProvider.GetKeyedService<NpgsqlConnection>(sourceType) is var source && source == null)
        {
            throw new InvalidOperationException($"Data source {sourceType} is not initialized. Ensure {nameof(AltinnHostDatabase.AddAltinnDatabase)} is called with the appropriate configuration.");
        }
        return source;
    }
}

/// <summary>
/// Defines a contract for Altinn database operations.
/// </summary>
public interface IAltinnDatabase
{
    /// <summary>
    /// Creates a new PostgreSQL connection for the specified data source type.
    /// </summary>
    /// <param name="sourceType">The type of data source (Migration or App).</param>
    /// <returns>An initialized <see cref="NpgsqlConnection"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="SourceType"/> is not configured using <see cref="AltinnHostDatabase.AddAltinnDatabase(Microsoft.Extensions.Hosting.IHostApplicationBuilder, Action{AltinnHostDatabaseOptions})"/>.
    /// </exception>
    NpgsqlConnection CreatePgsqlConnection(SourceType sourceType);
}
