using System.Text.Json;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// One entry in the activity log over assignments, delegations and requests. Names are
/// snapshots taken when the event happened.
/// </summary>
public class ActivityLogDto
{
    /// <summary>
    /// Unique identifier for the entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The main category of the record the entry describes.
    /// </summary>
    public ActivityLogType Type { get; set; }

    /// <summary>
    /// The child record category, or null when the entry describes the main record itself.
    /// </summary>
    public ActivityLogSubtype? Subtype { get; set; }

    /// <summary>
    /// The operation that produced the entry.
    /// </summary>
    public ActivityLogTrigger Trigger { get; set; }

    /// <summary>
    /// The (new) request status for request entries.
    /// </summary>
    public RequestStatus? Status { get; set; }

    /// <summary>
    /// When the event happened.
    /// </summary>
    public DateTimeOffset When { get; set; }

    /// <summary>
    /// The entity that performed the change.
    /// </summary>
    public Guid? ById { get; set; }

    /// <summary>
    /// Name of the entity that performed the change.
    /// </summary>
    public string ByName { get; set; }

    /// <summary>
    /// The system/channel the change came through.
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Name of the system/channel the change came through.
    /// </summary>
    public string SourceName { get; set; }

    /// <summary>
    /// The change operation identifier shared by all entries written by one operation.
    /// </summary>
    public string OperationId { get; set; }

    /// <summary>
    /// The party the access relation is from.
    /// </summary>
    public Guid? FromId { get; set; }

    /// <summary>
    /// Name of the from-party.
    /// </summary>
    public string FromName { get; set; }

    /// <summary>
    /// Entity type name of the from-party.
    /// </summary>
    public string FromType { get; set; }

    /// <summary>
    /// The party the access relation is to.
    /// </summary>
    public Guid? ToId { get; set; }

    /// <summary>
    /// Name of the to-party.
    /// </summary>
    public string ToName { get; set; }

    /// <summary>
    /// Entity type name of the to-party.
    /// </summary>
    public string ToType { get; set; }

    /// <summary>
    /// The facilitator party for delegation entries.
    /// </summary>
    public Guid? ViaId { get; set; }

    /// <summary>
    /// Name of the facilitator party.
    /// </summary>
    public string ViaName { get; set; }

    /// <summary>
    /// Entity type name of the facilitator party.
    /// </summary>
    public string ViaType { get; set; }

    /// <summary>
    /// The role held towards the from-party.
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// Name of the role.
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// The agent role on the to-side of a delegation.
    /// </summary>
    public Guid? ViaRoleId { get; set; }

    /// <summary>
    /// Name of the agent role.
    /// </summary>
    public string ViaRoleName { get; set; }

    /// <summary>
    /// The access package the entry concerns.
    /// </summary>
    public Guid? PackageId { get; set; }

    /// <summary>
    /// Name of the access package.
    /// </summary>
    public string PackageName { get; set; }

    /// <summary>
    /// The resource the entry concerns.
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Name of the resource.
    /// </summary>
    public string ResourceName { get; set; }

    /// <summary>
    /// The instance URN the entry concerns.
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// Identifier of the affected row.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Identifier of the affected row's main record, or null when the entry describes a main record.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Additional event data (previous status, request action, provenance references etc.).
    /// </summary>
    public JsonElement? Details { get; set; }

    /// <summary>
    /// The activity type catalog entry for the event, resolved with the most-specific-wins
    /// rule (exact status match, else the status-null fallback). Name and description come
    /// from the types endpoint.
    /// </summary>
    public Guid? ActivityTypeId { get; set; }
}
