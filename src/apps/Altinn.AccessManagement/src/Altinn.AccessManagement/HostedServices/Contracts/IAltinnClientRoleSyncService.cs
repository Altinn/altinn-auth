using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessManagement.HostedServices.Contracts
{
    /// <summary>
    /// Service for synchronizing Altinn client roles.
    /// </summary>
    public interface IAltinnClientRoleSyncService
    {
        /// <summary>
        /// Synchronizes altinn role data by first acquiring a remote lease and streaming altinn role entries.
        /// Returns if lease is already taken.
        /// </summary>
        /// <param name="ls">The lease result containing the lease data and status.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        Task SyncClientRoles(LeaseResult<AltinnClientRoleLease> ls, CancellationToken cancellationToken);
    }
}
