using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Catalog entry giving one valid activity log combination a display name and description.
/// The activity log itself keeps the raw dimensions; this catalog is read-side metadata
/// resolved with a most-specific-wins rule (exact status match, else the status-null row).
/// </summary>
[NotMapped]
public class BaseActivityType : BaseAudit, IEntityId, IEntityName
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The main record type the entry applies to.
    /// </summary>
    public ActivityLogType Type { get; set; }

    /// <summary>
    /// The child record type, or <see langword="null"/> for entries about the main record
    /// (matches entries where subtype is null).
    /// </summary>
    public ActivityLogSubtype? Subtype { get; set; }

    /// <summary>
    /// The operation the entry applies to.
    /// </summary>
    public ActivityLogTrigger Trigger { get; set; }

    /// <summary>
    /// The request status the entry applies to, or <see langword="null"/> for any status
    /// (the fallback row when no status-specific entry exists).
    /// </summary>
    public RequestStatus? Status { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// One-sentence description of what the event means.
    /// </summary>
    public string Description { get; set; }
}
