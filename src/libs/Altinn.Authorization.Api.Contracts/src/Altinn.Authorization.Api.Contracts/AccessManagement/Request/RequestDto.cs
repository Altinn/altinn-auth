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
    /// Request status (e.g. draft, pending, approved, rejected, withdrawn)
    /// </summary>
    public RequestStatus Status { get; set; }

    /// <summary>
    /// Type of request
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Requested resource
    /// </summary>
    public RequestRefrenceDto Resource { get; set; }

    /// <summary>
    /// Requested package
    /// </summary>
    public RequestRefrenceDto Package { get; set; }

    /// <summary>
    /// Relevant links for the request (e.g. confirm, check status)
    /// </summary>
    public RequestLinks Links { get; set; }

    /// <summary>
    /// Connection from one party to another that is requested
    /// </summary>
    public RequestConnectionDto Connection { get; set; }
}
