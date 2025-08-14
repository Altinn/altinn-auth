using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// EntityVariantRole
/// </summary>
[NotMapped]
public class BaseEntityVariantRole : BaseAudit
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// VariantId
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// RoleId
    /// </summary>
    public Guid RoleId { get; set; }
}
