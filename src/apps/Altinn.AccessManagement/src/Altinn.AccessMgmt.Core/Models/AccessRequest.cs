using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// An access request (tilgangsforespørsel) as the service layer sees it, independent of API version.
/// Each API version maps it to its own response model, so values that only one version shows,
/// such as <see cref="RequestedRightKeys"/>, never end up in the contract of another.
/// </summary>
public class AccessRequest
{
    /// <summary>
    /// Request id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Request status, with every status a request can have
    /// </summary>
    public RequestStatusV2 Status { get; set; }

    /// <summary>
    /// Type of request: resource or package
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
    /// Requested resource. Not set for package requests.
    /// </summary>
    public RequestReferenceDto? Resource { get; set; }

    /// <summary>
    /// The rights requested for the resource, as right keys from the resource policy.
    /// Null when no specific rights were requested.
    /// </summary>
    public IReadOnlyList<string>? RequestedRightKeys { get; set; }

    /// <summary>
    /// Requested package. Not set for resource requests.
    /// </summary>
    public RequestReferenceDto? Package { get; set; }

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
