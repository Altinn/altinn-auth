using Altinn.Authorization.AccessManagement.HostedServices;
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
    Task SyncParty(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken);
}
