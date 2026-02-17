using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts
{
    /// <summary>
    /// Service for synchronizing Altinn client roles.
    /// </summary>
    public interface ISingleResourceRegistryRightSyncService
    {
        /// <summary>
        /// Synchronizes single ResourceRegistry rights by first acquiring a remote lease and streaming delegated data.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="lease">The lease result containing the lease data and status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncSingleResourceRegistryRights(ILease lease, CancellationToken cancellationToken);

        /// <summary>
        /// Sync all flagged elements from error queue
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns></returns>
        Task SyncFailedSingleResourceRegistryRights(CancellationToken cancellationToken);
    }
}
