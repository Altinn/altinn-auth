using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts
{
    /// <summary>
    /// Service for synchronizing Altinn client roles.
    /// </summary>
    public interface ISingleInstanceRightSyncService
    {
        /// <summary>
        /// Synchronizes resourceregistry instances first acquiring a remote lease and streaming delegation entries.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="lease">The lease result containing the lease data and status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncSingleInstanceRights(ILease lease, CancellationToken cancellationToken);

        /// <summary>
        /// Synchronizes resourceregistry instances from error queue.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncFailedSingleInstanceRights(CancellationToken cancellationToken);
    }
}
