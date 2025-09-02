using System.Net;
using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.AccessManagement.HostedServices.Services;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.Host.Lease;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.HostedServices
{
    /// <summary>
    /// Hosted service for synchronizing Altinn roles.
    /// </summary>
    public partial class AltinnRoleHostedService(
        IAltinnLease lease,
        IFeatureManager featureManager,
        IStatusService statusService,
        ILogger<AltinnRoleHostedService> logger,
        IAllAltinnRoleSyncService allAltinnRoleSyncService,
        IAltinnClientRoleSyncService altinnClientRoleSyncService,
        IAltinnBankruptcyEstateRoleSyncService altinnBankruptcyEstateRoleSyncService,
        IAltinnAdminRoleSyncService altinnAdminRoleSyncService) : IHostedService, IDisposable
    {
        private readonly IAltinnLease _lease = lease;
        private readonly ILogger<AltinnRoleHostedService> _logger = logger;
        private readonly IFeatureManager _featureManager = featureManager;
        private readonly IStatusService _statusService = statusService;
        private readonly IAllAltinnRoleSyncService _allAltinnRoleSyncService = allAltinnRoleSyncService;
        private readonly IAltinnClientRoleSyncService _altinnClientRoleSyncService = altinnClientRoleSyncService;
        private readonly IAltinnBankruptcyEstateRoleSyncService _altinnBankruptcyEstateRoleSyncService = altinnBankruptcyEstateRoleSyncService;

        private Timer _timerAltinnRoles = null;
        private readonly CancellationTokenSource _stop = new();

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.StartAltinnRoleSync(_logger);

            _timerAltinnRoles = new Timer(async state => await SyncAltinnRoleDispatcher(state), _stop.Token, TimeSpan.Zero, TimeSpan.FromMinutes(2));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispatches the Altinn roles synchronization process in a separate task.
        /// </summary>
        /// <param name="state">Cancellation token for stopping execution.</param>
        private async Task SyncAltinnRoleDispatcher(object state)
        {
            var cancellationToken = (CancellationToken)state;
            try
            {
                var options = new ChangeRequestOptions()
                {
                    ChangedBy = AuditDefaults.Altinn2RoleImportSystem,
                    ChangedBySystem = AuditDefaults.Altinn2RoleImportSystem
                };

                if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesAllAltinnRoleSync))
                {
                    await using var ls = await _lease.TryAquireNonBlocking<AllAltinnRoleLease>("access_management_allaltinnrole_sync", cancellationToken);
                    if (!ls.HasLease || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAllAltinnRoles(ls, options, cancellationToken);
                }

                if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesAltinnClientRoleSync))
                {
                    await using var ls = await _lease.TryAquireNonBlocking<AltinnClientRoleLease>("access_management_altinnclientrole_sync", cancellationToken);
                    if (!ls.HasLease || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAltinnClientRoles(ls, options, cancellationToken);
                }

                if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesAltinnBancruptcyEstateRoleSync))
                {
                    await using var ls = await _lease.TryAquireNonBlocking<AltinnBankruptcyEstateRoleLease>("access_management_altinnbancruptcyestaterole_sync", cancellationToken);
                    if (!ls.HasLease || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAltinnBancruptcyEstateRoles(ls, options, cancellationToken);
                }

                if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesAltinnAdminRoleSync))
                {
                    await using var ls = await _lease.TryAquireNonBlocking<AltinnAdminRoleLease>("access_management_altinnadminrole_sync", cancellationToken);
                    if (!ls.HasLease || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SyncAltinnAdminRoles(ls, options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
            }            
        }

        private async Task SyncAltinnAdminRoles(LeaseResult<AltinnAdminRoleLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
        {
            var altinnAdminRoleStatus = await _statusService.GetOrCreateRecord(Guid.Parse("C8E97435-40F6-4C23-8886-29EBB3696DAC"), "accessmgmt-sync-sblbridge-adminrole", options, 5);
            var canRunAltinnAdminRoleSync = await _statusService.TryToRun(altinnAdminRoleStatus, options);

            try
            {
                if (canRunAltinnAdminRoleSync)
                {
                    await altinnAdminRoleSyncService.SyncAdminRoles(ls, cancellationToken);
                    await _statusService.RunSuccess(altinnAdminRoleStatus, options);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await _statusService.RunFailed(altinnAdminRoleStatus, ex, options);
            }
        }

        private async Task SyncAltinnBancruptcyEstateRoles(LeaseResult<AltinnBankruptcyEstateRoleLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
        {
            var altinnBankruptcyEstateRoleStatus = await _statusService.GetOrCreateRecord(Guid.Parse("90A9EF96-3488-43A0-9030-C88CEDC6D4A7"), "accessmgmt-sync-sblbridge-bancruptcyestaterole", options, 5);
            var canRunAltinnBankruptcyEstateRoleSync = await _statusService.TryToRun(altinnBankruptcyEstateRoleStatus, options);

            try
            {
                if (canRunAltinnBankruptcyEstateRoleSync)
                {
                    await altinnBankruptcyEstateRoleSyncService.SyncBankruptcyEstateRoles(ls, cancellationToken);
                    await _statusService.RunSuccess(altinnBankruptcyEstateRoleStatus, options);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await _statusService.RunFailed(altinnBankruptcyEstateRoleStatus, ex, options);
            }
        }

        private async Task SyncAltinnClientRoles(LeaseResult<AltinnClientRoleLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
        {
            var altinnClientRoleStatus = await _statusService.GetOrCreateRecord(Guid.Parse("3CB11D2A-AEC0-4895-91E0-9976C1BE84AF"), "accessmgmt-sync-sblbridge-clientrole", options, 5);
            var canRunAltinnClientRoleSync = await _statusService.TryToRun(altinnClientRoleStatus, options);

            try
            {
                if (canRunAltinnClientRoleSync)
                {
                    await _altinnClientRoleSyncService.SyncClientRoles(ls, cancellationToken);
                    await _statusService.RunSuccess(altinnClientRoleStatus, options);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await _statusService.RunFailed(altinnClientRoleStatus, ex, options);
            }
        }

        private async Task SyncAllAltinnRoles(LeaseResult<AllAltinnRoleLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
        {
            var allAltinnRoleStatus = await _statusService.GetOrCreateRecord(Guid.Parse("8B05CEB5-43D4-4FF1-A831-4790F5792B93"), "accessmgmt-sync-sblbridge-altinnrole", options, 5);
            var canRunAllAltinnRoleSync = await _statusService.TryToRun(allAltinnRoleStatus, options);

            try
            {
                if (canRunAllAltinnRoleSync)
                {
                    await _allAltinnRoleSyncService.SyncAllAltinnRoles(ls, cancellationToken);
                    await _statusService.RunSuccess(allAltinnRoleStatus, options);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await _statusService.RunFailed(allAltinnRoleStatus, ex, options);
            }
            
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.QuitAltinnRoleSync(_logger);
            }
            finally
            {
                _timerAltinnRoles?.Change(Timeout.Infinite, 0);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerAltinnRoles?.Dispose();
                _stop?.Cancel();
                _stop?.Dispose();
            }
        }
    }
}
