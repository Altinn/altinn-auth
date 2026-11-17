using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Request status, version 2. Has every status a request can have, while <see cref="RequestStatus"/>
/// keeps the values version 1 was published with. The numbers are the same in both.
/// Clients must accept values they do not know, since statuses may be added.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatusV2
{
    /// <summary>
    /// Not set
    /// </summary>
    None = 0,

    /// <summary>
    /// Not ready to be processed
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Waiting for processing
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Request is approved
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Request is rejected
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Request is withdrawn by the requester, and will not be processed
    /// </summary>
    Withdrawn = 5,

    /// <summary>
    /// Request is approved, but what was granted does not match what was requested
    /// </summary>
    PartialApprove = 6
}
