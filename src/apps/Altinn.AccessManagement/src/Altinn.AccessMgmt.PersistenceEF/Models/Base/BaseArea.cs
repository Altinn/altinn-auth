using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Area to organize accesspackages stuff
/// </summary>
[NotMapped]
public class BaseArea : BaseAudit
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }


    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// IconUrl
    /// </summary>
    public string IconUrl { get; set; }

    /// <summary>
    /// GroupId
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    public string Urn { get; set; }
}
