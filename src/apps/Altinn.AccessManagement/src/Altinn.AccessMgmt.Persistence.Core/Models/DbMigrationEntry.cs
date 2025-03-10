namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// Migration information
/// </summary>
public class DbMigrationEntry
{
    /// <summary>
    /// Object to migrate
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// Key for object script
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Version of the script
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Migrationscript
    /// </summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// When status was last set
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; }
}
