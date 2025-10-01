using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Entity
/// </summary>
[NotMapped]
public class BaseEntity : BaseAudit, IEntityId, IEntityName
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
    /// VariantId
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// RefId
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// Parent identifier
    /// </summary>
    public Guid? ParentId { get; set; }
}
