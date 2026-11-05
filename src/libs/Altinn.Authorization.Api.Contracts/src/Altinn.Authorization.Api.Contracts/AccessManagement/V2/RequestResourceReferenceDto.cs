using System.Text.Json.Serialization;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// The resource of a resource request, version 2, with the rights requested for it.
/// </summary>
public class RequestResourceReferenceDto : RequestReferenceDto
{
    /// <summary>
    /// The rights requested for the resource, as right keys from the resource policy.
    /// Not set when no specific rights were requested.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? RequestedRightKeys { get; set; }
}
