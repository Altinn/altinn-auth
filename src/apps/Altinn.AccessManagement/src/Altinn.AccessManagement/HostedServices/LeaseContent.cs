namespace Altinn.Authorization.AccessManagement.HostedServices;

/// <summary>
/// Lease content
/// </summary>
public class LeaseContent()
{
    /// <summary>
    /// The URL of the next page of Party data.
    /// </summary>
    public string PartyStreamNextPageLink { get; set; }

    /// <summary>
    /// The URL of the next page of AssignmentSuccess data.
    /// </summary>
    public string RoleStreamNextPageLink { get; set; }

    /// <summary>
    /// The URL of the next page of updates resourcs.
    /// </summary>
    public string ResourcesNextPageLink { get; set; }
}
