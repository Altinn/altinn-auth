using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Package
/// </summary>
[NotMapped]
public class BasePackage : BaseAudit, IEntityId, IEntityName, IEntityUrn
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
    /// EntityTypeId
    /// </summary>
    public Guid EntityTypeId { get; set; }

    /// <summary>
    /// AreaId
    /// </summary>
    public Guid AreaId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Can be assigned
    /// </summary>
    public bool IsAssignable { get; set; }

    /// <summary>
    /// Can be delegate
    /// </summary>
    public bool IsDelegable { get; set; }

    /// <summary>
    /// Has resources
    /// </summary>
    public bool HasResources { get; set; }

    /// <summary>
    /// Is available for ServiceOwners
    /// </summary>
    public bool IsAvailableForServiceOwners { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Code
    /// </summary>
    public string Code { get; set; }
}
