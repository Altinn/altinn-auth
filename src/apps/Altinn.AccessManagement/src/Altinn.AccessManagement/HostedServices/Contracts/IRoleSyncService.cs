using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessManagement.HostedServices.Contracts;

/// <summary>
/// Service for syncronizing roles
/// </summary>
public interface IRoleSyncService
{
    /// <summary>
    /// Sync roles
    /// </summary>
    Task SyncRoles(ILease ls, CancellationToken cancellationToken);
}
