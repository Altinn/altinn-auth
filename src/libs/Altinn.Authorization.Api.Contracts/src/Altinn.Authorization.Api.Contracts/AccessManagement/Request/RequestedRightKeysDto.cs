using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// The rights requested in a resource request. Sent as the body when creating the request;
/// the parties and the resource are given as query parameters.
/// </summary>
/// <remarks>
/// Unknown fields are rejected, so a misspelled field gives an error instead of a request for the whole resource.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RequestedRightKeysDto
{
    /// <summary>
    /// The rights being requested, as right keys from the policy of the resource.
    /// Leave out to request the resource without naming specific rights.
    /// </summary>
    public IEnumerable<string>? RequestedRightKeys { get; set; }
}
