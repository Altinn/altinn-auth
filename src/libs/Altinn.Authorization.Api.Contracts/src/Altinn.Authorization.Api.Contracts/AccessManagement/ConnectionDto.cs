namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Connection from one party to another
/// </summary>
public class ConnectionDto
{
    /// <summary>
    /// The party for which the connection and access applies
    /// </summary>
    public CompactEntityDto Party { get; set; } = new();

    /// <summary>
    /// The roles this party holds in the party of the parent connection, which is why the
    /// access is inherited. Only populated on sub-connections, and only for relations that
    /// carry a role (key role or client delegation); a plain main-unit hierarchy leaves it
    /// empty. A party can hold several such roles at once, for example both dagl and styr,
    /// and every one of them is listed. Ordered by role code so the same connection always
    /// serialises the same way; the order carries no priority or semantics beyond that.
    /// </summary>
    public List<CompactRoleDto> ViaRoles { get; set; } = new();

    /// <summary>
    /// Role accesses for the given party
    /// </summary>
    public List<CompactRoleDto> Roles { get; set; } = new();

    /// <summary>
    /// Access packages for the given party
    /// </summary>
    public List<AccessPackageDto> Packages { get; set; } = new();

    /// <summary>
    /// Direct resource accesses for the given party
    /// </summary>
    public List<ResourceDto> Resources { get; set; } = new();

    /// <summary>
    /// Resource instance accesses for the given party
    /// </summary>
    public List<ConnectionInstanceDto> Instances { get; set; } = new();

    /// <summary>
    /// Sub-connections of the party where the same access applies
    /// </summary>
    public List<ConnectionDto> Connections { get; set; } = new();
}

/// <summary>
/// Connection from one party to another
/// </summary>
public class ConnectionPackageDto
{
    /// <summary>
    /// Party
    /// </summary>
    public CompactEntityDto Party { get; set; } = new();

    /// <summary>
    /// Roles the party has for given filter
    /// </summary>
    public List<CompactRoleDto> Roles { get; set; } = new();

    /// <summary>
    /// Connections the party has
    /// </summary>
    public List<ConnectionDto> Connections { get; set; } = new();

    /// <summary>
    /// Packages the party has
    /// </summary>
    public List<CompactPackageDto> Packages { get; set; } = new();
}
