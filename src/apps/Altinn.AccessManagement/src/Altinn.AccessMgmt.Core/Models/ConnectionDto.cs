using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Connection from one party to another
/// </summary>
public class ConnectionDto
{
    /// <summary>
    /// Party
    /// </summary>
    public Entity Party { get; set; } = new();

    /// <summary>
    /// Roles the party has for given filter
    /// </summary>
    public List<Role> Roles { get; set; } = new();

    /// <summary>
    /// Connections the party has
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
    public Entity Party { get; set; } = new();

    /// <summary>
    /// Roles the party has for given filter
    /// </summary>
    public List<Role> Roles { get; set; } = new();

    /// <summary>
    /// Connections the party has
    /// </summary>
    public List<ConnectionDto> Connections { get; set; } = new();

    /// <summary>
    /// Packages the party has
    /// </summary>
    public List<Package> Packages { get; set; } = new();
}
