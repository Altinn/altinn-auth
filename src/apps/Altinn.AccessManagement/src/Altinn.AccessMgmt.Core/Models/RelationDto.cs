using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Connection from one party to another
/// </summary>
public class RelationDto
{
    /// <summary>
    /// Party
    /// </summary>
    public CompactEntity Party { get; set; } = new();

    /// <summary>
    /// Roles the party has for given filter
    /// </summary>
    public List<CompactRole> Roles { get; set; } = new();

    /// <summary>
    /// Connections the party has
    /// </summary>
    public List<RelationDto> Connections { get; set; } = new();
}

/// <summary>
/// Connection from one party to another
/// </summary>
public class RelationPackageDto
{
    /// <summary>
    /// Party
    /// </summary>
    public CompactEntity Party { get; set; } = new();

    /// <summary>
    /// Roles the party has for given filter
    /// </summary>
    public List<CompactRole> Roles { get; set; } = new();

    /// <summary>
    /// Connections the party has
    /// </summary>
    public List<RelationDto> Connections { get; set; } = new();

    /// <summary>
    /// Packages the party has
    /// </summary>
    public List<CompactPackage> Packages { get; set; } = new();
}
