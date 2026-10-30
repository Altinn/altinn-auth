using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Resource
/// </summary>
public class Resource : BaseResource
{
    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public ResourceType Type { get; set; }

    /// <summary>
    /// Maskinporten scopes, only set for resources of type MaskinportenSchema
    /// </summary>
    public ICollection<ResourceScope> Scopes { get; set; } = [];
}
