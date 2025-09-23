namespace Altinn.AccessMgmt.Core.HostedServices.Leases;

/// <summary>
/// Lease content
/// </summary>
internal class ResourceRegistryLease
{
    /// <summary>
    /// Latest element that was updated
    /// </summary>
    public DateTime Since { get; set; } = default;

    /// <summary>
    /// The URL of the next page of Resource data.
    /// </summary>
    public string ResourceNextPageLink { get; set; }
}
