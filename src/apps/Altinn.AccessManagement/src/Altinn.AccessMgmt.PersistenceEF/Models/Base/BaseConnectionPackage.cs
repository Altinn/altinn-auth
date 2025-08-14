namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Packages available on connections
/// </summary>
public class BaseConnectionPackage
{
    /// <summary>
    /// Identifier, AssignmentId or DelegationId
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Guid? FacilitatorId { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public Guid? FacilitatorRoleId { get; set; }

    /// <summary>
    /// Text hint for reason
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Indicate that connection is a direct assignment
    /// </summary>
    public bool IsDirect { get; set; }

    /// <summary>
    /// Indicate that connection is from a parent/child relation
    /// </summary>
    public bool IsParent { get; set; }

    /// <summary>
    /// Indicate that connection is a result of a role to role mapping
    /// </summary>
    public bool IsRoleMap { get; set; }

    /// <summary>
    /// Indicate that connection is a inheirited with a keyrole
    /// </summary>
    public bool IsKeyRole { get; set; }

    /// <summary>
    /// Package identifier
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// Entity has access to resources in this package
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// Entity can add this package to an assignment
    /// </summary>
    public bool CanAssign { get; set; }

    /// <summary>
    /// Text hint for reason for package
    /// </summary>
    public string PackageSource { get; set; }
}
