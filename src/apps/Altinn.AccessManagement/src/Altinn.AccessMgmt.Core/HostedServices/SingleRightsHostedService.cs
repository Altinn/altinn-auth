using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Services;
using Altinn.Authorization.Host.Lease;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices
{
    /// <summary>
    /// Hosted service for synchronizing Altinn roles.
    /// </summary>
    public partial class SingleRightsHostedService(
        ILeaseService leaseService,
        IFeatureManager featureManager,
        ILogger<SingleRightsHostedService> logger,
        ISingleAppRightSyncService singleAppRightSyncService,
        ISingleResourceRegistryRightSyncService singleResourceRightSyncService,
        ISingleInstanceRightSyncService singleInstanceRightSyncService
        ) : IHostedService, IDisposable
    {
        private readonly ILeaseService _leaseService = leaseService;
        private readonly ILogger<SingleRightsHostedService> _logger = logger;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly ISingleAppRightSyncService _singleAppRightSyncService = singleAppRightSyncService;
        private readonly ISingleResourceRegistryRightSyncService _singleResourceRightSyncService = singleResourceRightSyncService;
        private readonly ISingleInstanceRightSyncService _singleInstanceRightSyncService = singleInstanceRightSyncService;
        private Timer _timer = null;
        private readonly CancellationTokenSource _stop = new();
        private int _isRunning = 0;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.StartRegisterSync(_logger);

            _timer = new Timer(async state => await SyncSingleRightsDispatcher(state), _stop.Token, TimeSpan.Zero, TimeSpan.FromMinutes(2));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispatches the Altinn roles synchronization process in a separate task.
        /// </summary>
        /// <param name="state">Cancellation token for stopping execution.</param>
        private async Task SyncSingleRightsDispatcher(object state)
        {
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            {
                return;
            }

            try
            {
                var cancellationToken = (CancellationToken)state;
                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesSingleAppRightSync))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_singleappright_sync", cancellationToken);
                    if (lease is not null && !cancellationToken.IsCancellationRequested)
                    {
                        await SyncSingleAppRights(lease, cancellationToken);
                    }
                }

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesSingleResourceRightSync))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_singleresourceregistryright_sync", cancellationToken);
                    if (lease is not null && !cancellationToken.IsCancellationRequested)
                    {
                        await SyncSingleResourceRegistryRights(lease, cancellationToken);
                    }
                }

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesSingleInstanceRightSync))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_singleinstanceright_sync", cancellationToken);
                    if (lease is not null && !cancellationToken.IsCancellationRequested)
                    {
                        await SyncSingleInstanceRights(lease, cancellationToken);
                    }
                }

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesSingleAppRightSyncFromErrorQueue))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_singleappright_fromerrorqueue_sync", cancellationToken);
                    if (lease is not null && !cancellationToken.IsCancellationRequested)
                    {
                        await SyncFailedSingleAppRights(lease, cancellationToken);
                    }
                }

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesSingleResourceRightSyncFromErrorQueue))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_singleresourceregistryright_fromerrorqueue_sync", cancellationToken);
                    if (lease is not null && !cancellationToken.IsCancellationRequested)
                    {
                        await SyncFailedSingleResourceRegistryRights(cancellationToken);
                    }
                }

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesSingleInstanceRightSyncFromErrorQueue))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_singleinstanceright_fromerrorqueue_sync", cancellationToken);
                    if (lease is not null && !cancellationToken.IsCancellationRequested)
                    {
                        await SyncFailedSingleInstanceRights(lease, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
            finally
            {
                _isRunning = 0;
            }
        }

        private async Task SyncSingleAppRights(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _singleAppRightSyncService.SyncSingleAppRights(lease, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }

        }

        private async Task SyncSingleResourceRegistryRights(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _singleResourceRightSyncService.SyncSingleResourceRegistryRights(lease, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
        }

        private async Task SyncSingleInstanceRights(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _singleInstanceRightSyncService.SyncSingleInstanceRights(lease, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
        }

        private async Task SyncFailedSingleAppRights(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _singleAppRightSyncService.SyncFailedSingleAppRights(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }

        }

        private async Task SyncFailedSingleResourceRegistryRights(CancellationToken cancellationToken)
        {
            try
            {
                await _singleResourceRightSyncService.SyncFailedSingleResourceRegistryRights(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
        }

        private async Task SyncFailedSingleInstanceRights(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _singleInstanceRightSyncService.SyncFailedSingleInstanceRights(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.QuitRegisterSync(_logger);
            }
            finally
            {
                _timer?.Change(Timeout.Infinite, 0);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _stop?.Cancel();
                _stop?.Dispose();
            }
        }
    }
}
