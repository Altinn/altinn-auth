namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// Represents configuration options for Access Management Persistence.
/// </summary>
public class AccessMgmtPersistenceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the persistence layer is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the type of database used for persistence.
    /// </summary>
    public MgmtDbType DbType { get; set; } = MgmtDbType.Postgres;

    /// <summary>
    /// Gets or sets the default language code for translations (e.g., "nob").
    /// </summary>
    public string DefaultLanguage { get; set; } = "nob"; // no-NB?

    /// <summary>
    /// Gets or sets the name of the base schema where the entity's table is located.
    /// </summary>
    public string BaseSchema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets the name of the schema used for storing translations of the entity.
    /// </summary>
    public string TranslationSchema { get; set; } = "translation";

    /// <summary>
    /// Gets or sets the alias prefix used for translation views in queries.
    /// </summary>
    public string TranslationAliasPrefix { get; set; } = "t_"; // translation view name?

    /// <summary>
    /// Gets or sets the name of the schema used for storing historical records of the entity.
    /// </summary>
    public string BaseHistorySchema { get; set; } = "dbo_history";

    /// <summary>
    /// Gets or sets the alias prefix used for history views in queries.
    /// </summary>
    public string HistoryAliasPrefix { get; set; } = "h_";

    /// <summary>
    /// Gets or sets the name of the schema used for storing historical translations of the entity.
    /// </summary>
    public string TranslationHistorySchema { get; set; } = "translation_history";

    /// <summary>
    /// Gets or sets the name of the schema used for storing historical translations of the entity.
    /// </summary>
    public string DatabaseReadUser { get; set; } = "platform_authorization";

    /// <summary>
    /// A list of language codes (e.g., "en", "nb", "de") that should be considered
    /// when ingesting JSON-based data into the database.
    /// </summary>
    public List<string> JsonIngestLanguages { get; set; } // TODO: Ivar, remove

    /// <summary>
    /// The base path where JSON files used for ingestion are stored.
    /// Default location: "Ingest/JsonData/" within the application base directory.
    /// </summary>
    public string JsonBasePath { get; set; } = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Ingest/JsonData/"); // TODO: Ivar, remove

    /// <summary>
    /// A dictionary that determines whether JSON ingestion is enabled for specific data sets.
    /// The key represents a data category, and the value determines if ingestion is enabled.
    /// </summary>
    public Dictionary<string, bool> JsonIngestEnabled { get; set; } = new Dictionary<string, bool>(); // TODO: Ivar, Rename: IngestEnabled
}

/// <summary>
/// Enumerates the available database types for Access Management Persistence.
/// </summary>
public enum MgmtDbType
{
    /// <summary>
    /// PostgreSQL database.
    /// </summary>
    Postgres = default,

    /// <summary>
    /// Microsoft SQL Server database.
    /// </summary>
    MSSQL
}
