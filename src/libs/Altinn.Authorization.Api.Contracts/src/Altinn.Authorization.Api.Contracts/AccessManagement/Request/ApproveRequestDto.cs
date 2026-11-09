namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// The rights to give when approving a resource request. Sent as the body when approving;
/// the party and the request are given as query parameters.
/// </summary>
public class ApproveRequestDto
{
    /// <summary>
    /// The rights to give, as right keys from the policy of the resource. May include rights that were not requested.
    /// Leave out to give what was requested. Only allowed for resource requests.
    /// </summary>
    public IEnumerable<string>? RightKeys { get; set; }
}
