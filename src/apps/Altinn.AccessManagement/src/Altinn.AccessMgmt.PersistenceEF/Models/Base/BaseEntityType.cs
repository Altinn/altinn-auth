using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// EntityType
/// </summary>
[NotMapped]
public class BaseEntityType : BaseAudit
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ProviderId
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}
