using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Define the types of Providers
/// </summary>
[NotMapped]
public class BaseProviderType : BaseAudit, IEntityId, IEntityName
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Provider type name
    /// </summary>
    public string Name { get; set; }
}
