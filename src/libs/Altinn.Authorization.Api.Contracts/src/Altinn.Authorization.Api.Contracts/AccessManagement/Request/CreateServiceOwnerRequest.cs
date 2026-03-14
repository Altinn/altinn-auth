namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Base input dto for creating a new request
/// </summary>
public class CreateServiceOwnerRequest
{
    /// <summary>
    /// Request connection
    /// </summary>
    [Obsolete("Use From and To directly")]
    public ConnectionRequestInputDto Connection { get; set; }

    /// <summary>
    /// Urn describing the party
    /// </summary>
    public string From { get; set; }

    /// <summary>
    /// Urn describing the party
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// Reference to the resource
    /// </summary>
    public RequestRefrenceDto Resource { get; set; }

    /// <summary>
    /// Reference to the access package
    /// </summary>
    public RequestRefrenceDto Package { get; set; }
}
