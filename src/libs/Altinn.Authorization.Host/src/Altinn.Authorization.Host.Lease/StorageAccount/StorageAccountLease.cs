using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Altinn.Authorization.Host.Lease.Telemetry;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

namespace Altinn.Authorization.Host.Lease.StorageAccount;

/// <summary>
/// Represents the result of a lease acquisition operation for a blob in the storage account.
/// Contains information about the lease, the associated blob, and the lease client.
/// </summary>
internal sealed class StorageAccountLease : ILease
{
    internal StorageAccountLease(
        ILogger logger,
        BlobClient blobClient,
        BlobLeaseClient blobLeaseClient,
        BlobLease blobLease)
    {
        Logger = logger;
        BlobClient = blobClient;
        BlobLeaseClient = blobLeaseClient;
        BlobLease = blobLease;

        if (blobLease is { })
        {
            Stopwatch = Stopwatch.StartNew();
            RenewalTask = LeaseRefresher();
        }
        else
        {
            Cancel();
        }
    }

    private ILogger Logger { get; set; }

    private BlobLease BlobLease { get; set; }

    private BlobClient BlobClient { get; init; }

    private BlobLeaseClient BlobLeaseClient { get; init; }

    private SemaphoreSlim Semaphore { get; init; } = new(1, 1);

    private Stopwatch Stopwatch { get; init; }

    private CancellationTokenSource LeaseRefresherCancellation { get; init; } = new();

    private Task? RenewalTask { get; init; }

    private int _disposed = 0;

    [DoesNotReturn]
    private void Cancel()
    {
        LeaseRefresherCancellation.Cancel();
        LeaseRefresherCancellation.Token.ThrowIfCancellationRequested();
    }

    /// <inheritdoc/>
    public async Task<T> Get<T>(CancellationToken cancellationToken = default)
        where T : class, new()
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            LeaseRefresherCancellation.Token.ThrowIfCancellationRequested();
            var content = await BlobClient.DownloadContentAsync(cancellationToken);
            return content.Value.Content.ToObjectFromJson<T>();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task Update<T>(T data, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            LeaseRefresherCancellation.Token.ThrowIfCancellationRequested();
            var content = JsonSerializer.Serialize(data);
            var options = new BlobUploadOptions()
            {
                Conditions = new()
                {
                    LeaseId = BlobLease.LeaseId,
                },
            };

            await LeaseTelemetry.RecordLeaseUpdate(Logger, BlobClient.Name, async () =>
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                return await BlobClient.UploadAsync(stream, options, cancellationToken);
            });
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.LeaseLost || ex.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
        {
            Cancel();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task Update<T>(Action<T> configureData, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        if (configureData is { })
        {
            var data = new T();
            configureData(data);
            await Update(data, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            await LeaseRefresherCancellation.CancelAsync();
            if (RenewalTask is { })
            {
                await RenewalTask.WaitAsync(TimeSpan.FromSeconds(5));
            }

            var release = ReleaseLease(CancellationToken.None);
            await release.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            Semaphore?.Dispose();
            LeaseRefresherCancellation?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private async Task ReleaseLease(CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            if (BlobLease is { })
            {
                await LeaseTelemetry.RecordReleaseLease(Logger, BlobClient.Name, async () => await BlobClient.GetBlobLeaseClient(BlobLease.LeaseId).ReleaseAsync(default, cancellationToken));
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.LeaseLost || ex.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
        {
            Cancel();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task RefreshLease(CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            LeaseRefresherCancellation.Token.ThrowIfCancellationRequested();
            BlobLease = await LeaseTelemetry.RecordRefreshLease(Logger, BlobClient.Name, async () => await BlobLeaseClient.RenewAsync(default, cancellationToken));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.LeaseLost || ex.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
        {
            Cancel();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task LeaseRefresher()
    {
        try
        {
            var refreshDelay = TimeSpan.FromSeconds(30);
            while (!LeaseRefresherCancellation.IsCancellationRequested)
            {
                await Task.Delay(refreshDelay, LeaseRefresherCancellation.Token);
                await RefreshLease(LeaseRefresherCancellation.Token);
            }
        }
        finally
        {
            Stopwatch.Stop();
            LeaseTelemetry.RecordLeaseDuration(BlobClient.Name, Stopwatch.Elapsed);
        }
    }
}
