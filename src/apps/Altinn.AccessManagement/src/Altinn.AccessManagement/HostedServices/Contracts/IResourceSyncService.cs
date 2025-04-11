using Altinn.Authorization.AccessManagement;
using Altinn.Authorization.AccessManagement.HostedServices;
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
    /// Sync resources
    /// </summary>
    Task<bool> SyncResources(CancellationToken cancellationToken);

    /// <summary>
    /// Sync resource mapping
    /// </summary>
    Task SyncResourceMapping(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken);
}
