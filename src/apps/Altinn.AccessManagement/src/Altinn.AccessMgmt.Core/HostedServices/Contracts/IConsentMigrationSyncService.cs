namespace Altinn.AccessMgmt.Core.HostedServices.Contracts;

/// <summary>
/// Service for synchronizing/migrating consents from old application
/// </summary>
public interface IConsentMigrationSyncService
{
    /// <summary>
    /// Processes a batch of consents for migration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of consents processed in the batch</returns>
    Task<int> ProcessBatch(CancellationToken cancellationToken);
}
