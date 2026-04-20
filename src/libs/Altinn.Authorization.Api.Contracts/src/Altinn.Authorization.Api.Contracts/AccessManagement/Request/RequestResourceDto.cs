namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Request a resource
/// </summary>
public class RequestResourceDto
{
    /// <summary>
    /// The request is from user wanting the package
    /// Identify with valid urn see _meta/urns/party
    /// </summary>
    public string From { get; set; }

    /// <summary>
    /// The request is sent to the organization giving the package
    /// Identify with valid urn see _meta/urns/party
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// The resource identified with resourceregistry resourceId
    /// </summary>
    public string Resource { get; set; }

    /// <summary>
    /// The specific keys for the resource
    /// </summary>
    public string[]? RightKeys { get; set; }
}
