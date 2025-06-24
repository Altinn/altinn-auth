using Altinn.Authorization.AccessManagement.HostedServices;
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
    Task SyncRoles(LeaseResult<RegisterLease> ls, CancellationToken cancellationToken);
}
