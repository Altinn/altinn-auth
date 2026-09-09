namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

/// <summary>
/// Database roles and table privileges that every application table must grant.
/// Single source of truth shared by the migration SQL generator
/// (<see cref="CustomMigrationsSqlGenerator"/>) and the FFB Grant Check tool, so the two cannot drift
/// out of sync.
/// </summary>
public static class TableGrantConstants
{
    /// <summary>Roles that are granted access to every application table.</summary>
    public static readonly IReadOnlyList<string> Roles = ["platform_authorization", "platform_authorization_admin"];

    /// <summary>Privileges granted to each role on every application table.</summary>
    public static readonly IReadOnlyList<string> Privileges = ["SELECT", "INSERT", "UPDATE", "DELETE", "TRIGGER", "REFERENCES"];
}
