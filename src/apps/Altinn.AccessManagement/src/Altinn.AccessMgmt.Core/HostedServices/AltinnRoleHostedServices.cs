using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Services;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices
{
    /// <summary>
    /// Hosted service for synchronizing Altinn roles.
    /// </summary>
    public partial class AltinnRoleHostedService(
        ILeaseService leaseService,
        IFeatureManager featureManager,
        ILogger<AltinnRoleHostedService> logger,
        IAllAltinnRoleSyncService allAltinnRoleSyncService,
        IAltinnClientRoleSyncService altinnClientRoleSyncService,
        //// IAltinnBankruptcyEstateRoleSyncService altinnBankruptcyEstateRoleSyncService,
        IAltinnAdminRoleSyncService altinnAdminRoleSyncService
        ) : IHostedService, IDisposable
    {
        private readonly ILeaseService _leaseService = leaseService;
        private readonly ILogger<AltinnRoleHostedService> _logger = logger;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly IAllAltinnRoleSyncService _allAltinnRoleSyncService = allAltinnRoleSyncService;
        private readonly IAltinnClientRoleSyncService _altinnClientRoleSyncService = altinnClientRoleSyncService;
        //// private readonly IAltinnBankruptcyEstateRoleSyncService _altinnBankruptcyEstateRoleSyncService = altinnBankruptcyEstateRoleSyncService;
        private readonly IAltinnAdminRoleSyncService _altinnAdminRoleSyncService = altinnAdminRoleSyncService;
        private Timer _timer = null;
        private readonly CancellationTokenSource _stop = new();
        private int _isRunning = 0;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.StartRegisterSync(_logger);

            _timer = new Timer(async state => await SyncAltinnRoleDispatcher(state), _stop.Token, TimeSpan.Zero, TimeSpan.FromMinutes(2));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispatches the Altinn roles synchronization process in a separate task.
        /// </summary>
        /// <param name="state">Cancellation token for stopping execution.</param>
        private async Task SyncAltinnRoleDispatcher(object state)
        {
            if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            {
                return;
            }

            try
            {
                var cancellationToken = (CancellationToken)state;
                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesAllAltinnRoleSync))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_allaltinnrole_sync", cancellationToken);
                    if (lease is null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAllAltinnRoles(lease, cancellationToken);
                }

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesAltinnClientRoleSync))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_altinnclientrole_sync", cancellationToken);
                    if (lease is null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAltinnClientRoles(lease, cancellationToken);
                }               

                if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesAltinnAdminRoleSync))
                {
                    await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_altinnadminrole_sync", cancellationToken);
                    if (lease is null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAltinnAdminRoles(lease, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
        }

        private async Task SyncAllAltinnRoles(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _allAltinnRoleSyncService.SyncAllAltinnRoles(lease, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }

        }

        private async Task SyncAltinnClientRoles(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _altinnClientRoleSyncService.SyncClientRoles(lease, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }
        }

        private async Task SyncAltinnAdminRoles(ILease lease, CancellationToken cancellationToken)
        {
            try
            {
                await _altinnAdminRoleSyncService.SyncAdminRoles(lease, cancellationToken);
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
