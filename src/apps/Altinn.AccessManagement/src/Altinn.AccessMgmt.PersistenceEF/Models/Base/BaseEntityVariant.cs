using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// EntityVariant
/// </summary>
[NotMapped]
public class BaseEntityVariant : BaseAudit
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }
}
