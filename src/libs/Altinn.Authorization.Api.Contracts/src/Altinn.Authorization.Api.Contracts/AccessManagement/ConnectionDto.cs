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
