using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Packages available on connections
/// </summary>
[NotMapped]
public class BaseConnectionResource : BaseAudit
{
    /// <summary>
    /// Identifier, AssignmentId or DelegationId
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; set; }
}
