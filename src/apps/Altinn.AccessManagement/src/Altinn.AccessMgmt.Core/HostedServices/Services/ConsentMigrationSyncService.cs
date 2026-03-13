using System.Diagnostics;
using System.Diagnostics.Metrics;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <summary>
/// Migrates consents from Altinn2 to Altinn3 in batches.
/// - Processes consents with configurable status filter
/// - Updates Altinn2 migration status after processing
/// - Exports metrics for monitoring
/// </summary>
public class ConsentMigrationSyncService : BaseSyncService, IConsentMigrationSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ConsentMigrationSettings> _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConsentMigrationSyncService> _logger;
    private readonly Counter<long> _processedCounter;
    private readonly Counter<long> _migratedCounter;
    private readonly Counter<long> _failedCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentMigrationSyncService"/> class
    /// </summary>
    public ConsentMigrationSyncService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<ConsentMigrationSettings> settings,
        TimeProvider timeProvider,
        IMeterFactory meterFactory,
        ILogger<ConsentMigrationSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _timeProvider = timeProvider;
        _logger = logger;

        var meter = meterFactory.Create("Altinn.AccessManagement.ConsentMigration");
        _processedCounter = meter.CreateCounter<long>("consent_migration_processed_total", description: "Total number of consents processed");
        _migratedCounter = meter.CreateCounter<long>("consent_migration_migrated_total", description: "Total number of consents successfully migrated");
        _failedCounter = meter.CreateCounter<long>("consent_migration_failed_total", description: "Total number of consents that failed migration");
    }

    /// <inheritdoc/>
    public async Task<int> ProcessBatch(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            return await ProcessBatchWithScope(scope.ServiceProvider, _settings.CurrentValue, cancellationToken);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "Network error in batch processing. Will retry in next cycle.");
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch processing");
            return 0;
        }
    }

    private async Task<int> ProcessBatchWithScope(
        IServiceProvider scopedServices,
        ConsentMigrationSettings migrationSettings,
        CancellationToken cancellationToken)
    {
        var migrationClient = scopedServices.GetRequiredService<IAltinn2ConsentClient>();
        var migrationService = scopedServices.GetRequiredService<IConsentMigrationService>();

        var consentIds = await migrationClient.GetAltinn2ConsentListForMigration(
            migrationSettings.BatchSize,
            migrationSettings.ConsentStatus,
            migrationSettings.OnlyExpiredConsents,
            cancellationToken);

        if (consentIds == null || consentIds.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation("Processing batch of {Count} consents with status '{Status}'", consentIds.Count, _settings.CurrentValue.ConsentStatus);

        int successCount = 0;
        int failedCount = 0;

        // Start N parallel workers to process the batch
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = migrationSettings.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(consentIds, parallelOptions, async (consentId, ct) =>
        {
            bool success = await ProcessSingleConsent(migrationService, consentId, ct);
            if (success)
            {
                Interlocked.Increment(ref successCount);
            }
            else
            {
                Interlocked.Increment(ref failedCount);
            }
        });

        _logger.LogInformation("Batch completed: {Success} succeeded, {Failed} failed", successCount, failedCount);

        return consentIds.Count;
    }

    private async Task<bool> ProcessSingleConsent(
        IConsentMigrationService migrationService,
        Guid consentId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Migrate the consent request
            var result = await migrationService.MigrateConsent(consentId, cancellationToken);

            _processedCounter.Add(1);

            if (result.Success)
            {
                _migratedCounter.Add(1);
                return true;
            }
            else
            {
                _failedCounter.Add(1);
                _logger.LogWarning("Failed to migrate consent {ConsentId}: {Error}", consentId, result.ErrorMessage);
                return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _processedCounter.Add(1);
            _failedCounter.Add(1);

            // Only log unexpected errors
            if (ex is not HttpRequestException and not TaskCanceledException)
            {
                _logger.LogError(ex, "Unexpected error migrating consent {ConsentId}", consentId);
            }

            return false;
        }
    }
}
