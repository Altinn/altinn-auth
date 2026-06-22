namespace Altinn.AccessMgmt.FFB.Config;

public class EnvironmentsConfig
{
    public List<EnvironmentEntry> Environments { get; set; } = [];
}

public class EnvironmentEntry
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Connection string for the AccessMgmt database (used by AppDbContext / EF Core).</summary>
    public string AccessMgmt { get; set; } = string.Empty;

    /// <summary>Connection string for the Register database (plain config value, no EF binding).</summary>
    public string Register { get; set; } = string.Empty;

    /// <summary>
    /// UUID of the system/service account used as changed_by and changed_by_system in the
    /// assignment sync audit context (session_audit_context table + SET LOCAL app.*).
    /// </summary>
    public string SystemAccountId { get; set; } = string.Empty;
}
