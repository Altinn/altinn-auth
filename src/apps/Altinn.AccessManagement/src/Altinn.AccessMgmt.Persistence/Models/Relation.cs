namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// New Connection
/// </summary>
public class Relation
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Guid? ViaId { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public Guid? ViaRoleId { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; }
}

/// <summary>
/// New Connection
/// </summary>
public class ExtRelation
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public CompactEntity From { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public CompactRole Role { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public CompactEntity Via { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public CompactRole ViaRole { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public CompactEntity To { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public CompactPackage Package { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public CompactResource Resource { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; }
}

/// <summary>
/// New Connection
/// </summary>
public class CompactRelation
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Guid? ViaId { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public Guid? ViaRoleId { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; }
}

/// <summary>
/// New Connection
/// </summary>
public class ExtCompactRelation
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public CompactEntity From { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public CompactRole Role { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public CompactEntity Via { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public CompactRole ViaRole { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public CompactEntity To { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; }
}
