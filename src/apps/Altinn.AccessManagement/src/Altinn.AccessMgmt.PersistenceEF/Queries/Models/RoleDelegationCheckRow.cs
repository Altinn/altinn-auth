namespace Altinn.AccessMgmt.PersistenceEF.Queries.Models;

/// <summary>
/// Flat row representation of the role delegation check query.
/// Matches exactly the columns returned by the SQL query.
/// </summary>
public class RoleDelegationCheckRow
{
    public Guid RoleId { get; set; }

    public string RoleUrn { get; set; }

    public string RoleLegacyUrn { get; set; }

    public bool IsAssignable { get; set; }

    public bool IsDelegable { get; set; }

    public Guid? AssignmentRoleId { get; set; }

    public string? AssignmentRoleUrn { get; set; }

    public Guid? FromId { get; set; }

    public string? FromName { get; set; }

    public Guid? ToId { get; set; }

    public string? ToName { get; set; }

    public Guid? ViaId { get; set; }

    public string? ViaName { get; set; }

    public Guid? ViaRoleId { get; set; }

    public string? ViaRoleUrn { get; set; }

    public bool? HasAccess { get; set; }

    public bool? CanDelegate { get; set; }

    public string? Reason { get; set; }

    public bool? IsAssignmentRole { get; set; }

    public bool? IsRoleMap { get; set; }

    public bool? IsThroughKeyRole { get; set; }

    public bool? IsThroughMainUnit { get; set; }

    public bool? IsMainAdminRole { get; set; }
}
