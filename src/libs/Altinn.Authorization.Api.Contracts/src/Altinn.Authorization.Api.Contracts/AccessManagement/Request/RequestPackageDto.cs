namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Request a package
/// </summary>
public class RequestPackageDto
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
    /// The package identified with uuid or urn
    /// </summary>
    public string Package { get; set; }
}
