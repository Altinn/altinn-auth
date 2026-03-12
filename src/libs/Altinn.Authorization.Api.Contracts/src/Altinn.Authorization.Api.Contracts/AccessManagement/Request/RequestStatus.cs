using System.Reflection;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Request status
/// </summary>
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
