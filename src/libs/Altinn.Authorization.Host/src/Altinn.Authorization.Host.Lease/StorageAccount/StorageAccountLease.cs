using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Altinn.Authorization.Host.Lease.StorageAccount;

/// <summary>
/// Represents a lease management implementation for a storage account.
/// Provides functionality for acquiring, releasing, and refreshing leases on blobs in a storage account.
/// </summary>
/// <param name="factory">The factory used to create <see cref="BlobServiceClient"/> instances.</param>
/// <param name="logger">The logger used to log events related to lease operations.</param>
/// <remarks>
/// https://learn.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes
/// </remarks>
public partial class StorageAccountLease(IAzureClientFactory<BlobServiceClient> factory, ILogger<StorageAccountLease> logger) : IAltinnLease
{
    private ILogger<StorageAccountLease> Logger { get; } = logger;

    private IAzureClientFactory<BlobServiceClient> Factory { get; } = factory;

    /// <inheritdoc/>
    public async Task<LeaseResult<T>> TryAquireNonBlocking<T>(string leaseName, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var client = CreateClient(leaseName);
        await CreateEmptyFileIfNotExists<T>(client, cancellationToken);
        var leaseClient = client.GetBlobLeaseClient();
        var content = await client.DownloadContentAsync(cancellationToken);
        var data = content.Value.Content.ToObjectFromJson<T>();

        try
        {
            var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(60), default, cancellationToken);
            Log.AcquiredLease(Logger, lease.Value.LeaseId);
            return new StorageAccountLeaseResult<T>()
            {
                LeaseName = leaseName,
                BlobClient = client,
                LeaseClient = leaseClient,
                Data = data,
                Response = lease.Value,
                Implementation = this,
            };
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "LeaseAlreadyPresent")
        {
            Log.LeaseAlreadyTaken(Logger);
            return new StorageAccountLeaseResult<T>()
            {
                LeaseName = leaseName,
                BlobClient = client,
                LeaseClient = leaseClient,
                Data = data,
                Response = default,
                Implementation = this,
            };
        }
    }

    /// <inheritdoc/>
    public async Task<LeaseResult<T>> Put<T>(LeaseResult<T> lease, T data, CancellationToken cancellationToken = default)
        where T : class
    {
        if (lease.HasLease && lease is StorageAccountLeaseResult<T> castedLease)
        {
            var content = JsonSerializer.Serialize(data);
            using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await castedLease.BlobClient.UploadAsync(
            contentStream,
            new BlobUploadOptions()
            {
                Conditions = new()
                {
                    LeaseId = castedLease.Response.LeaseId,
                },
            },
            cancellationToken);

            lease.Data = data;
            return lease;
        }

        return lease;
    }

    /// <inheritdoc/>
    public async Task Release<T>(LeaseResult<T> lease, CancellationToken cancellationToken = default)
          where T : class
    {
        if (lease.HasLease && lease is StorageAccountLeaseResult<T> castedLease)
        {
            try
            {
                await castedLease.BlobClient.GetBlobLeaseClient(castedLease.Response.LeaseId).ReleaseAsync(default, cancellationToken);
                castedLease.Response = null;
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "LeaseLost" || ex.ErrorCode == "LeaseAlreadyPresent")
            {
                Log.FailedToReleaseLease(Logger, ex);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<LeaseResult<T>> RefreshLease<T>(LeaseResult<T> lease, CancellationToken cancellationToken = default)
        where T : class
    {
        if (lease.HasLease && lease is StorageAccountLeaseResult<T> castedLease)
        {
            var leaseExpirationTime = castedLease.Acquired.AddSeconds(Convert.ToDouble(castedLease.Response.LeaseTime));
            if (leaseExpirationTime < DateTime.UtcNow.AddSeconds(20))
            {
                try
                {
                    var result = await castedLease.LeaseClient.RenewAsync(default, cancellationToken);
                    castedLease.Response = result;
                    return castedLease;
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == "LeaseLost" || ex.ErrorCode == "LeaseAlreadyPresent")
                {
                    Log.CantRefreshLease(Logger, ex);
                    castedLease.Response = null;
                    return castedLease;
                }
            }
        }

        return lease;
    }

    private async Task CreateEmptyFileIfNotExists<T>(BlobClient client, CancellationToken cancellationToken)
        where T : class, new()
    {
        try
        {
            var content = JsonSerializer.Serialize(new T());
            if (!await client.ExistsAsync(cancellationToken))
            {
                await client.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), cancellationToken);
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobAlreadyExists")
        {
            Log.BlobAlreadyExists(Logger, ex);
        }
    }

    /// <summary>
    /// Creates a blob client
    /// </summary>
    private BlobClient CreateClient(string blobName) =>
        Factory.CreateClient(AltinnLeaseOptions.StorageAccountLease.Name).GetBlobContainerClient(AltinnLeaseOptions.StorageAccountLease.Container).GetBlobClient(blobName);

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Can't refresh lease")]
        internal static partial void CantRefreshLease(ILogger logger, RequestFailedException ex);

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to create blob")]
        internal static partial void BlobAlreadyExists(ILogger logger, RequestFailedException ex);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to release lease")]
        internal static partial void FailedToReleaseLease(ILogger logger, RequestFailedException ex);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Lease is already taken")]
        internal static partial void LeaseAlreadyTaken(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Aquired lease with id {leaseId}")]
        internal static partial void AcquiredLease(ILogger logger, string leaseId);
    }
}
