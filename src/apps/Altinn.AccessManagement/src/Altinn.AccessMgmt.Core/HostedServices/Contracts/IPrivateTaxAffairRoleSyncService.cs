using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts
{
    /// <summary>
    /// Service for synchronizing Altinn private tax affair roles.
    /// </summary>
    public interface IPrivateTaxAffairRoleSyncService
    {
        /// <summary>
        /// Synchronizes altinn private tax affair role data by first acquiring a remote lease and streaming altinn role entries.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="lease">The lease result containing the lease data and status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncPrivateTaxAffairRoles(ILease lease, CancellationToken cancellationToken);
    }
}
