using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Provider
/// </summary>
public class Provider : BaseProvider
{
    /// <summary>
    /// The type of provider
    /// </summary>
    public ProviderType Type { get; set; }
}
