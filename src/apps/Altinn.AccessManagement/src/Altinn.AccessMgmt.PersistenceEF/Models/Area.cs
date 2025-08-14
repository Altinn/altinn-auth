using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Area
/// </summary>
public class Area : BaseArea
{
    /// <summary>
    /// EntityGroup
    /// </summary>
    public AreaGroup Group { get; set; }
}
