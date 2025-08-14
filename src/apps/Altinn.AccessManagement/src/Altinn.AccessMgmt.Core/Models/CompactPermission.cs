using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Permission
/// </summary>
public class CompactPermission
{
    /// <summary>
    /// From party
    /// </summary>
    public CompactEntity From { get; set; }

    /// <summary>
    /// To party
    /// </summary>
    public CompactEntity To { get; set; }
}
