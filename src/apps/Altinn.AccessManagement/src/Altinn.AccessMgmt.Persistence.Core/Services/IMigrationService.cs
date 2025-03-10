namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <summary>
/// Keep track of migrations for database
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Initialize the service
    /// </summary>
    /// <returns></returns>
    Task Init(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs that a migration is completed
    /// </summary>
    /// <returns></returns>
    Task LogMigration(string objectName, string key, string script, int version = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs that a migration is completed
    /// </summary>
    /// <returns></returns>
    Task LogMigration(Type type, string key, string script, int version = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs that a migration is completed
    /// </summary>
    /// <returns></returns>
    Task LogMigration<T>(string key, string script, int version = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if any key for any type is needed
    /// </summary>
    /// <returns></returns>
    bool NeedAnyMigration(Dictionary<Type, List<string>> typeKeys);
    
    /// <summary>
    /// Check if any key for type is needed
    /// </summary>
    /// <returns></returns>
    bool NeedAnyMigration(Type type, List<string> keys);

    /// <summary>
    /// Check if migration is needed
    /// </summary>
    bool NeedMigration(string objectName, string key, int version = 1);

    /// <summary>
    /// Check if migration is needed
    /// </summary>
    bool NeedMigration(Type type, string key, int version = 1);

    /// <summary>
    /// Check if migration is needed
    /// </summary>
    bool NeedMigration<T>(string key, int version = 1);
}
