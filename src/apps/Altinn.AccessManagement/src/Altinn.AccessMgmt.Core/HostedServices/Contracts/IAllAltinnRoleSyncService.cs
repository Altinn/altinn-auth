using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts
{
    /// <summary>
    /// Service for syncronizing altinn roles
    /// </summary>
    public interface IAllAltinnRoleSyncService
    {
        /// <summary>
        /// Sync roles
        /// </summary>
        Task SyncAllAltinnRoles(ILease lease, CancellationToken cancellationToken);
    }
}
