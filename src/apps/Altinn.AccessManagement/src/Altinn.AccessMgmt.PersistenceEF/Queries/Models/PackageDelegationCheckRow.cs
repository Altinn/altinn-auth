namespace Altinn.AccessMgmt.PersistenceEF.Queries.Models;

/// <summary>
/// Flat row representation of the package delegation check query.
/// Matches exactly the columns returned by the SQL query.
/// </summary>
public class PackageDelegationCheckRow
{
    public Guid PackageId { get; set; }
    public string PackageUrn { get; set; } = null!;
    public Guid AreaId { get; set; }

    public bool IsAssignable { get; set; }
    public bool IsDelegable { get; set; }

    public Guid? RoleId { get; set; }
    public string? RoleUrn { get; set; }

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

    public bool? IsAssignmentPackage { get; set; }
    public bool? IsRolePackage { get; set; }
    public bool? IsKeyRolePackage { get; set; }
    public bool? IsMainUnitPackage { get; set; }
    public bool? IsMainAdminPackage { get; set; }
}
