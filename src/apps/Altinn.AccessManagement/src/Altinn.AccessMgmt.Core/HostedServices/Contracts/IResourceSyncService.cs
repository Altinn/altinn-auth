using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts;

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
    Task SyncResources(ILease ls, CancellationToken cancellationToken);
}
