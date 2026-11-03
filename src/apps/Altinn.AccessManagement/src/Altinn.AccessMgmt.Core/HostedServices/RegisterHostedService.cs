using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.HostedServices.Services;
using Altinn.Authorization.Host.Lease;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices;

/// <summary>
/// A hosted service responsible for synchronizing register data using leases.
/// </summary>
/// <param name="leaseService">Lease provider for distributed locking.</param>
/// <param name="logger">Logger for logging service activities.</param>
/// <param name="featureManager">for reading feature flags</param>
/// <param name="resourceSyncService">Service for syncing resources</param>
/// <param name="partySyncService">Service for syncing parties</param>
/// <param name="roleSyncService">Service for syncing roles</param>
public partial class RegisterHostedService(
    ILeaseService leaseService,
    ILogger<RegisterHostedService> logger,
    IFeatureManager featureManager,
    IResourceSyncService resourceSyncService,
    IPartySyncService partySyncService,
    IRoleSyncService roleSyncService
    ) : IHostedService, IDisposable
{
    private readonly ILeaseService _leaseService = leaseService;
    private readonly ILogger<RegisterHostedService> _logger = logger;
    private readonly IFeatureManager _featureManager = featureManager;
    private readonly IResourceSyncService resourceSyncService = resourceSyncService;
    private readonly IPartySyncService partySyncService = partySyncService;
    private readonly IRoleSyncService roleSyncService = roleSyncService;
    private Timer _timer = null;
    private readonly CancellationTokenSource _stop = new();
    private readonly Lock _syncLock = new();
    private Task _sync = Task.CompletedTask;
    private bool _stopping;
    private bool _disposed;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.StartRegisterSync(_logger);

        _timer = new Timer(DispatchSync, _stop.Token, TimeSpan.Zero, TimeSpan.FromMinutes(2));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts a sync unless one is already running or the service is stopping, and keeps its task so that
    /// <see cref="StopAsync"/> can wait for it.
    /// </summary>
    /// <param name="state">Cancellation token for stopping execution.</param>
    private void DispatchSync(object state)
    {
        // The timer can still run a callback queued before it was stopped, so the check and the assignment
        // share a lock with StopAsync.
        lock (_syncLock)
        {
            if (_stopping || !_sync.IsCompleted)
            {
                return;
            }

            _sync = SyncRegisterDispatcher(state);
        }
    }

    public async Task EnsureDbIsIngestWithRegisterData(CancellationToken cancellationToken)
    {
        var isDbIngested = false;
        do
        {
            if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesRegisterSyncImport, cancellationToken))
            {
                await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_register_sync", cancellationToken);
                if (lease is { })
                {
                    var data = await lease.Get<RegisterLease>(cancellationToken);
                    if (data is { })
                    {
                        if (!data.IsDbIngested)
                        {
                            await partySyncService.SyncParty(lease, true, cancellationToken);
                            await roleSyncService.SyncRoles(lease, true, cancellationToken);

                            data = await lease.Get<RegisterLease>(cancellationToken);
                            data.IsDbIngested = true;
                            await lease.Update(data, cancellationToken);
                        }

                        isDbIngested = data.IsDbIngested;
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            else
            {
                isDbIngested = true;
            }
        }
        while (!isDbIngested);
    }

    /// <summary>
    /// Dispatches the register synchronization process in a separate task.
    /// </summary>
    /// <param name="state">Cancellation token for stopping execution.</param>
    private async Task SyncRegisterDispatcher(object state)
    {
        try
        {
            var cancellationToken = (CancellationToken)state;
            if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesResourceRegistrySync, cancellationToken))
            {
                await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_resource_registry_sync", cancellationToken);
                if (lease is null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await SyncResourceRegistry(lease, cancellationToken);
            }

            if (await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesRegisterSync))
            {
                await using var lease = await _leaseService.TryAcquireNonBlocking("access_management_register_sync", cancellationToken);

                if (lease is null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await SyncRegisterParty(lease, cancellationToken);
                await SyncRegisterRoles(lease, cancellationToken);
            }

            _logger.LogInformation("Register sync completed!");
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
        }
    }

    private async Task SyncRegisterRoles(ILease lease, CancellationToken cancellationToken)
    {
        try
        {
            await roleSyncService.SyncRoles(lease, false, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
        }
    }

    private async Task SyncResourceRegistry(ILease lease, CancellationToken cancellationToken)
    {
        try
        {
            await resourceSyncService.SyncResourceOwners(cancellationToken);
            await resourceSyncService.SyncResources(lease, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
        }
    }

    private async Task SyncRegisterParty(ILease lease, CancellationToken cancellationToken)
    {
        try
        {
            await partySyncService.SyncParty(lease, false, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task sync;
        lock (_syncLock)
        {
            _stopping = true;
            sync = _sync;
        }

        try
        {
            Log.QuitRegisterSync(_logger);
        }
        finally
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        // A sync still running when the host disposes the logger would fail to log, and crash the process.
        try
        {
            await _stop.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Dispose has already run, and it cancels the sync itself.
        }

        try
        {
            await sync.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The host stopped waiting; the sync has been asked to stop.
        }
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
            lock (_syncLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _stopping = true;
            }

            _timer?.Dispose();
            _stop.Cancel();
            _stop.Dispose();
        }
    }
}
