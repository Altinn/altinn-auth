using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessManagement.HostedServices.Contracts;

/// <summary>
/// Service for syncronizing parties
/// </summary>
public interface IPartySyncService
{
    /// <summary>
    /// Sync parties
    /// </summary>
    Task SyncParty(LeaseResult<RegisterLease> ls, CancellationToken cancellationToken);
}
