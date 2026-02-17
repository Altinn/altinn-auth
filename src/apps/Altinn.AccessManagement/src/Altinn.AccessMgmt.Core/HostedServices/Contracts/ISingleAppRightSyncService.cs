using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts
{
    /// <summary>
    /// Service for synchronizing Altinn client roles.
    /// </summary>
    public interface ISingleAppRightSyncService
    {
        /// <summary>
        /// Synchronizes app delegations by first acquiring a remote lease and streaming delegations.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="lease">The lease result containing the lease data and status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncSingleAppRights(ILease lease, CancellationToken cancellationToken);

        /// <summary>
        /// Synchronizes app delegations from errorqueue
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncFailedSingleAppRights(CancellationToken cancellationToken);
    }
}
