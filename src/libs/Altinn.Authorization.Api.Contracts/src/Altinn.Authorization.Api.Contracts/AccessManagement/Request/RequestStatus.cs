using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Request status, version 1. The values are fixed, since version 1 clients may check them
/// exhaustively; statuses added later are only in <see cref="RequestStatusV2"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
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
    Withdrawn = 5
}
