using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Altinn.Authorization.Host.Database;

/// <summary>
/// Provides extension methods for configuring the Altinn host database.
/// </summary>
public static class AltinnHostDatabase
{
    /// <summary>
    /// Adds database to the host application builder using Altinn authorization default setup.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configureOptions">An action to configure the database options.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostApplicationBuilder AddAltinnDatabase(this IHostApplicationBuilder builder, Action<AltinnHostDatabaseOptions> configureOptions)
    {
        var options = new AltinnHostDatabaseOptions();
        configureOptions?.Invoke(options);
        builder.Services.TryAddSingleton<IAltinnDatabase, AltinnHostDatabaseFactory>();
        if (!options.Enabled)
        {
            return builder;
        }

        if (!builder.Services.Contains(Markers.MigrationSource.ServiceDescriptor) && options.MigrationSource != null)
        {
            builder.Services.AddNpgsqlDataSource(options.MigrationSource.ConnectionString.ToString(), options.MigrationSource.Builder, serviceKey: SourceType.Migration);
            builder.Services.Add(Markers.MigrationSource.ServiceDescriptor);
        }

        if (!builder.Services.Contains(Markers.AppSource.ServiceDescriptor) && options.AppSource != null)
        {
            builder.Services.AddNpgsqlDataSource(options.AppSource.ConnectionString.ToString(), options.AppSource.Builder, serviceKey: SourceType.App);
            builder.Services.Add(Markers.AppSource.ServiceDescriptor);
        }

        if (!builder.Services.Contains(Markers.ServiceDescriptor))
        {
            if (options.Telemetry.EnableTraces)
            {
                builder.Services.AddOpenTelemetry()
                    .WithTracing(builder => builder.AddNpgsql());
            }

            if (options.Telemetry.EnableMetrics)
            {
                builder.Services.AddOpenTelemetry()
                    .WithMetrics(builder => builder.AddNpgsqlInstrumentation());
            }

            builder.Services.Add(Markers.ServiceDescriptor);
        }

        return builder;
    }

    /// <summary>
    /// Contains marker classes for identifying service descriptors.
    /// </summary>
    private sealed class Markers
    {
        /// <summary>
        /// Marker class for the migration source service descriptor.
        /// </summary>
        public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton<Markers, Markers>();

        /// <summary>
        /// Marker class for the migration source service descriptor.
        /// </summary>
        internal sealed class MigrationSource
        {
            /// <summary>
            /// Gets the singleton service descriptor for the migration source.
            /// </summary>
            public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton<MigrationSource, MigrationSource>();
        }

        /// <summary>
        /// Marker class for the application source service descriptor.
        /// </summary>
        internal sealed class AppSource
        {
            /// <summary>
            /// Gets the singleton service descriptor for the application source.
            /// </summary>
            public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton<AppSource, AppSource>();
        }
    }
}
