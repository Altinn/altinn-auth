using Npgsql;

namespace Altinn.Authorization.Host.Database;

/// <summary>
/// Represents the configuration options for the Altinn host database.
/// </summary>
public class AltinnHostDatabaseOptions
{
    /// <summary>
    /// Specifies if DB connection is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the data source for application-related database connections.
    /// </summary>
    public PgsqlDataSourceOptions AppSource { get; set; }

    /// <summary>
    /// Gets or sets the data source for migration-related database connections.
    /// </summary>
    public PgsqlDataSourceOptions MigrationSource { get; set; }

    /// <summary>
    /// Gets or sets the telemetry options for monitoring database interactions.
    /// </summary>
    public TelemetryOptions Telemetry { get; set; } = new TelemetryOptions();

    /// <summary>
    /// Represents telemetry configuration options for monitoring database performance and activity.
    /// </summary>
    public class TelemetryOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether metrics collection is enabled.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// </summary>
        public bool EnableTraces { get; set; } = true;
    }

    /// <summary>
    /// Represents a PostgreSQL data source with a connection string and an optional builder action.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PgsqlDataSourceOptions"/> class.
    /// </remarks>
    /// <param name="connectionString">The connection string for the PostgreSQL database.</param>
    public class PgsqlDataSourceOptions(string connectionString)
    {
        /// <summary>
        /// Gets the Npgsql connection string builder initialized with the provided connection string.
        /// </summary>
        public NpgsqlConnectionStringBuilder ConnectionString { get; } = new NpgsqlConnectionStringBuilder(connectionString);

        /// <summary>
        /// Gets or sets an optional action to configure the Npgsql data source builder.
        /// </summary>
        public Action<NpgsqlDataSourceBuilder> Builder { get; set; } = _ => { };
    }
}

/// <summary>
/// Defines the types of data sources available.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// Represents a migration-related database source.
    /// </summary>
    Migration,

    /// <summary>
    /// Represents an application-related database source.
    /// </summary>
    App,
}
