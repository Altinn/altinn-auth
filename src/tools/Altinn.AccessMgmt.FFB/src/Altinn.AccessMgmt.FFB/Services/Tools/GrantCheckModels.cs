namespace Altinn.AccessMgmt.FFB.Services.Tools;

/// <summary>
/// Result of a grant check for a single environment.
/// </summary>
public sealed record GrantCheckEnvironmentResult(
    int OkCount,
    List<TableGrant> Missing,
    List<MissingTable> MissingFromDb);

/// <summary>
/// A table that exists in the EF model but not in the environment's database.
/// </summary>
public sealed record MissingTable(string Environment, string Schema, string Table);

/// <summary>
/// Grant state for one table with missing privileges. IsFixing/IsFixed/Error
/// are UI state mutated by the page.
/// </summary>
public sealed class TableGrant
{
    public string Environment { get; set; } = string.Empty;

    public string Schema { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public List<(string Role, string Privilege)> Missing { get; set; } = [];

    public bool IsFixing { get; set; }

    public bool IsFixed { get; set; }

    public string? Error { get; set; }
}
