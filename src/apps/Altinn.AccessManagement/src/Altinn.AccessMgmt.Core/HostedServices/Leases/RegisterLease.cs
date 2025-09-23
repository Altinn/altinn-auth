namespace Altinn.AccessMgmt.Core.HostedServices.Leases;

/// <summary>
/// Lease content
/// </summary>
internal class RegisterLease()
{
    /// <summary>
    /// The URL of the next page of Party data.
    /// </summary>
    public string PartyStreamNextPageLink { get; set; }

    /// <summary>
    /// The URL of the next page of AssignmentSuccess data.
    /// </summary>
    public string RoleStreamNextPageLink { get; set; }
}
