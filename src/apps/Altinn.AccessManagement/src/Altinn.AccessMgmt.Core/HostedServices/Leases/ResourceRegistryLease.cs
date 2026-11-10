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

    /// <summary>
    /// The URL of the next page of the resource change feed (<c>resource/changes</c>).
    /// Kept when a page has no next link, since that only means the feed has no more changes yet.
    /// Null starts the feed from the beginning.
    /// </summary>
    public string ResourceChangesNextPageLink { get; set; }
}
