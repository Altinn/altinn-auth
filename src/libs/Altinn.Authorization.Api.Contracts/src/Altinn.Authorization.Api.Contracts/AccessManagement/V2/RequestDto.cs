using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ModelUtils;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Response dto for requests, version 2.
/// Differs from <see cref="Request.RequestDto"/> in that <see cref="Status"/> is non-exhaustive,
/// so new statuses can be added without breaking clients.
/// </summary>
public class RequestDto
{
    /// <summary>
    /// Request id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Request status (e.g. draft, pending, approved, partial approve, rejected, withdrawn).
    /// Clients must accept values they do not know.
    /// </summary>
    public NonExhaustiveEnum<RequestStatus> Status { get; set; }

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
    /// Requested resource, with the rights requested for it
    /// </summary>
    public RequestResourceReferenceDto Resource { get; set; }

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
