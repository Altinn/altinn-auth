using Altinn.AccessMgmt.PersistenceEF.Contexts;

namespace Altinn.AccessMgmt.FFB.Services.Tools;

/// <summary>
/// Comparison result for one constants class against its database table.
/// <see cref="Expanded"/> is UI state mutated by the page.
/// </summary>
public sealed class CheckResult
{
    public string Name { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public int ConstantsCount { get; set; }

    public int DbCount { get; set; }

    public List<FixableIssue> Missing { get; set; } = [];

    public List<FixableIssue> Mismatches { get; set; } = [];

    public List<FixableIssue> Extra { get; set; } = [];

    public bool Expanded { get; set; }

    /// <summary>
    /// When true, rows that exist in DB but have no matching constant are considered
    /// deletable — the table is fully controlled by constants and nothing else writes to it.
    /// <br />
    /// Set to false (default) for tables that mix constant-defined rows with environment-specific
    /// data (e.g. Provider, Role, Package — rows also arrive via the platform at runtime).
    /// Extra rows on append-only tables are shown as informational only, with no delete button.
    /// </summary>
    public bool AllowDeleteExtra { get; set; } = false;

    public bool IsOk =>
        Missing.Count == 0 &&
        Mismatches.Count == 0 &&
        (!AllowDeleteExtra || Extra.Count == 0);
}

/// <summary>
/// A single deviation between a constant and the database, optionally carrying a fix
/// delegate that <c>ConstantsCheckService.ExecuteFixAsync</c> runs against a fresh context.
/// IsFixed/IsFixing/Error are UI state mutated by the page.
/// </summary>
public sealed class FixableIssue
{
    public Guid EntityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public Func<AppDbContext, Task>? Fix { get; set; }

    public bool IsFixed { get; set; }

    public bool IsFixing { get; set; }

    public string? Error { get; set; }
}
