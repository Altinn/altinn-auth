using Altinn.AccessManagement;
using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessManagement.HostedServices.Services;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.Authorization.AccessManagement.HostedServices;
using Altinn.Authorization.Host.Lease;
using Microsoft.FeatureManagement;

namespace Altinn.Authorization.AccessManagement;

/// <summary>
/// A hosted service responsible for synchronizing register data using leases.
/// </summary>
/// <param name="lease">Lease provider for distributed locking.</param>
/// <param name="logger">Logger for logging service activities.</param>
/// <param name="featureManager">for reading feature flags</param>
/// <param name="statusService">Status service</param>
/// <param name="resourceSyncService">Service for syncing resources</param>
/// <param name="partySyncService">Service for syncing parties</param>
/// <param name="roleSyncService">Service for syncing roles</param>
public partial class RegisterHostedService(
    IAltinnLease lease,
    ILogger<RegisterHostedService> logger,
    IFeatureManager featureManager,
    IStatusService statusService,
    IResourceSyncService resourceSyncService,
    IPartySyncService partySyncService,
    IRoleSyncService roleSyncService
    ) : IHostedService, IDisposable
{
    private readonly IAltinnLease _lease = lease;
    private readonly ILogger<RegisterHostedService> _logger = logger;
    private readonly IFeatureManager _featureManager = featureManager;
    private readonly IStatusService statusService = statusService;
    private readonly IResourceSyncService resourceSyncService = resourceSyncService;
    private readonly IPartySyncService partySyncService = partySyncService;
    private readonly IRoleSyncService roleSyncService = roleSyncService;
    private Timer _timer = null;
    private readonly CancellationTokenSource _stop = new();

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.StartRegisterSync(_logger);

        _timer = new Timer(async state => await SyncRegisterDispatcher(state), _stop.Token, TimeSpan.Zero, TimeSpan.FromMinutes(2));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches the register synchronization process in a separate task.
    /// </summary>
    /// <param name="state">Cancellation token for stopping execution.</param>
    private async Task SyncRegisterDispatcher(object state)
    {
        var cancellationToken = (CancellationToken)state;
        try
        {
            var options = new ChangeRequestOptions()
            {
                ChangedBy = AuditDefaults.RegisterImportSystem,
                ChangedBySystem = AuditDefaults.RegisterImportSystem
            };

            if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesResourceRegistrySync, cancellationToken))
            {
                await using var ls = await _lease.TryAquireNonBlocking<ResourceRegistryLease>("access_management_resource_registry_sync", cancellationToken);
                if (!ls.HasLease || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await SyncResourceRegistry(ls, options, cancellationToken);
            }

            if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesRegisterSync))
            {
                await using var ls = await _lease.TryAquireNonBlocking<RegisterLease>("access_management_register_sync", cancellationToken);
                if (!ls.HasLease || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await SyncRegisterParty(ls, options, cancellationToken);
                await SyncRegisterRoles(ls, options, cancellationToken);
            }

            _logger.LogInformation("Register sync completed!");
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
        }
    }

    private async Task SyncRegisterRoles(LeaseResult<RegisterLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
    {
        var roleStatus = await statusService.GetOrCreateRecord(Guid.Parse("84E9726D-E61B-4DFF-91D7-9E17C8BB41A6"), "accessmgmt-sync-register-role", options, 5);
        var canRunRoleSync = await statusService.TryToRun(roleStatus, options);

        try
        {
            if (canRunRoleSync)
            {
                await roleSyncService.SyncRoles(ls, cancellationToken);
                await statusService.RunSuccess(roleStatus, options);
            }
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
            await statusService.RunFailed(roleStatus, ex, options);
        }
    }

    private async Task SyncResourceRegistry(LeaseResult<ResourceRegistryLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
    {
        var resourceStatus = await statusService.GetOrCreateRecord(Guid.Parse("BEF7E6C8-2928-423E-9927-225488A5B08B"), "accessmgmt-sync-register-resource", options, 5);
        var canRunResourceSync = await statusService.TryToRun(resourceStatus, options);

        try
        {
            if (canRunResourceSync)
            {
                await resourceSyncService.SyncResourceOwners(cancellationToken);
                await _lease.RefreshLease(ls, cancellationToken);
                await resourceSyncService.SyncResources(ls, cancellationToken);
                await statusService.RunSuccess(resourceStatus, options);
            }
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
            await statusService.RunFailed(resourceStatus, ex, options);
        }
    }

    private async Task SyncRegisterParty(LeaseResult<RegisterLease> ls, ChangeRequestOptions options, CancellationToken cancellationToken)
    {
        var partyStatus = await statusService.GetOrCreateRecord(Guid.Parse("C18B67F6-B07E-482C-AB11-7FE12CD1F48D"), "accessmgmt-sync-register-party", options, 5);
        var canRunPartySync = await statusService.TryToRun(partyStatus, options);

        try
        {
            if (canRunPartySync)
            {
                await partySyncService.SyncParty(ls, cancellationToken);
                await statusService.RunSuccess(partyStatus, options);
            }
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
            await statusService.RunFailed(partyStatus, ex, options);
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

    /// <summary>
    /// Releases unmanaged resources.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
            _stop?.Cancel();
            _stop?.Dispose();
        }
    }
}
