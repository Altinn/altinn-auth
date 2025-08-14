using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended AreaGroup
/// </summary>
public class AreaGroup : BaseAreaGroup
{
    /// <summary>
    /// EntityType
    /// </summary>
    public EntityType EntityType { get; set; }
}
