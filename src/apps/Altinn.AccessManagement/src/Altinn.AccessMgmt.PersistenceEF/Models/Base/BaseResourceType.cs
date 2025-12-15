using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// ResourceType
/// </summary>
[NotMapped]
public class BaseResourceType : BaseAudit
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}
