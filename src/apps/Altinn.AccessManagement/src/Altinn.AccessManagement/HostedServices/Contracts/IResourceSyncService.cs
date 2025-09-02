using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.Authorization.AccessManagement;
using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessManagement.HostedServices.Contracts;

/// <summary>
/// Service for syncronizing resources
/// </summary>
public interface IResourceSyncService
{
    /// <summary>
    /// Sync resource owners (ServiceOwner)
    /// </summary>
    Task<bool> SyncResourceOwners(CancellationToken cancellationToken);

    /// <summary>
    /// Sync resource mapping
    /// </summary>
    Task SyncResources(LeaseResult<ResourceRegistryLease> ls, CancellationToken cancellationToken);
}
