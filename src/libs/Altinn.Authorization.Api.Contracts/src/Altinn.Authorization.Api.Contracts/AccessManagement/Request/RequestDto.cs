using Altinn.Authorization.ModelUtils;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Base response dto for requests
/// </summary>
public class RequestDto
{
    /// <summary>
    /// Request id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Request status (e.g. draft, pending, approved, rejected, withdrawn).
    /// Statuses added later are reported as their closest version 1 value, such as
    /// <see cref="RequestStatusV2.PartialApprove"/> as <see cref="RequestStatus.Approved"/>; see <see cref="StatusV2"/>.
    /// </summary>
    public RequestStatus Status { get; set; }

    /// <summary>
    /// Request status, including values added after <see cref="Status"/> was fixed, such as
    /// <see cref="RequestStatusV2.PartialApprove"/>. Clients must accept values they do not know.
    /// </summary>
    public NonExhaustiveEnum<RequestStatusV2> StatusV2 { get; set; }

    /// <summary>
    /// Type of request
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Last updated
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Last updated by
    /// </summary>
    public PartyReferenceDto? LastUpdatedBy { get; set; }

    /// <summary>
    /// Requested resource
    /// </summary>
    public RequestReferenceDto Resource { get; set; }

    /// <summary>
    /// Requested package
    /// </summary>
    public RequestReferenceDto Package { get; set; }

    /// <summary>
    /// Relevant links for the request (e.g. confirm, check status)
    /// </summary>
    public RequestLinks Links { get; set; }

    /// <summary>
    /// Party that is requested to grant access
    /// </summary>
    public PartyEntityDto From { get; set; }

    /// <summary>
    /// Party that access is requested for
    /// </summary>
    public PartyEntityDto To { get; set; }

    /// <summary>
    /// Party that created the request
    /// </summary>
    public PartyEntityDto By { get; set; }
}
