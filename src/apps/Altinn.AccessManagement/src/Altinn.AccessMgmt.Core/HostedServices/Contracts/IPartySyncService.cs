using Altinn.Authorization.Host.Lease;

namespace Altinn.AccessMgmt.Core.HostedServices.Contracts;

/// <summary>
/// Service for syncronizing parties
/// </summary>
public interface IPartySyncService
{
    /// <summary>
    /// Sync parties
    /// </summary>
    Task SyncParty(ILease ls, CancellationToken cancellationToken);
}
