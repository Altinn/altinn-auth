using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessManagement.HostedServices.Contracts
{
    /// <summary>
    /// Service for synchronizing Altinn client roles.
    /// </summary>
    public interface IAltinnAdminRoleSyncService
    {
        /// <summary>
        /// Synchronizes altinn role data by first acquiring a remote lease and streaming altinn role entries.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="lease">The lease result containing the lease data and status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncAdminRoles(ILease lease, CancellationToken cancellationToken);
    }
}
