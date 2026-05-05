using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.Authorization.Host.Lease;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices;

/// <summary>
/// Hosted service for migrating consents from old application to new system
/// </summary>
public partial class ConsentMigrationHostedService : BackgroundService
{
    private readonly IConsentMigrationSyncService _syncService;
    private readonly IFeatureManager _featureManager;
    private readonly IOptionsMonitor<ConsentMigrationSettings> _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConsentMigrationHostedService> _logger;
    private readonly ILeaseService _leaseService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentMigrationHostedService"/> class
    /// </summary>
    public ConsentMigrationHostedService(
        IConsentMigrationSyncService syncService,
        IFeatureManager featureManager,
        IOptionsMonitor<ConsentMigrationSettings> settings,
        TimeProvider timeProvider,
        ILogger<ConsentMigrationHostedService> logger,
        ILeaseService leaseService)
    {
        _syncService = syncService;
        _featureManager = featureManager;
        _settings = settings;
        _timeProvider = timeProvider;
        _logger = logger;
        _leaseService = leaseService;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.StartConsentMigration(_logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var migrationSettings = _settings.CurrentValue;
                bool isEnabled = await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration);
                if (!isEnabled)
                {
                    Log.FeatureDisabled(_logger);
                    await _timeProvider.Delay(TimeSpan.FromMilliseconds(migrationSettings.FeatureDisabledDelayMs), stoppingToken);
                    continue;
                }

                // Try to acquire lease - only one pod will succeed
                await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_consent_migration", stoppingToken);
                if (lease is null)
                {
                    Log.LeaseUnavailable(_logger);

                    // Add jitter to avoid thundering herd when multiple pods retry simultaneously
                    var delayWithJitter = TimeSpan.FromMilliseconds(migrationSettings.EmptyFeedDelayMs) + RandomJitter();
                    await _timeProvider.Delay(delayWithJitter, stoppingToken);
                    continue;
                }

                Log.LeaseAcquired(_logger);

                int processedCount = await _syncService.ProcessBatch(stoppingToken);

                if (processedCount == 0)
                {
                    await _timeProvider.Delay(TimeSpan.FromMilliseconds(migrationSettings.EmptyFeedDelayMs), stoppingToken);
                }
                else
                {
                    await _timeProvider.Delay(TimeSpan.FromMilliseconds(migrationSettings.NormalDelayMs), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await _timeProvider.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private static TimeSpan RandomJitter()
    {
        // Add between 1 and 2 seconds of random jitter to avoid thundering herd issues
        var randomMs = Random.Shared.Next(1_000, 2_000);
        return TimeSpan.FromMilliseconds(randomMs);
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Consent migration feature flag is disabled. Waiting before rechecking.")]
        internal static partial void FeatureDisabled(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Consent Migration Service starting")]
        internal static partial void StartConsentMigration(ILogger logger);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Unexpected error in consent migration loop")]
        internal static partial void SyncError(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Lease unavailable, another pod is processing consent migration")]
        internal static partial void LeaseUnavailable(ILogger logger);

        [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Lease acquired successfully for consent migration")]
        internal static partial void LeaseAcquired(ILogger logger);
    }
}
