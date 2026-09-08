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
    Task SyncParty(ILease ls, bool isInit = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// One-off cleanup of assignments given to system users that were deleted before deletion handling was introduced.
    /// Runs once and records completion in the lease.
    /// </summary>
    Task ClearAssignmentsForDeletedSystemUsers(ILease ls, CancellationToken cancellationToken = default);
}
