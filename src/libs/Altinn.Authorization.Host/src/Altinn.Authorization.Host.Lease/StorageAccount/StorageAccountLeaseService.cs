using System.Text;
using Altinn.Authorization.Host.Lease.Telemetry;
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
/// <param name="Factory">The factory used to create <see cref="BlobServiceClient"/> instances.</param>
/// <param name="Logger">The logger used to log events related to lease operations.</param>
/// <remarks>
/// https://learn.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes
/// </remarks>
public partial class StorageAccountLeaseService(ILogger<StorageAccountLeaseService> Logger, IAzureClientFactory<BlobServiceClient> Factory) : ILeaseService
{
    private static TimeSpan MaxLeaseTime { get; } = TimeSpan.FromSeconds(60);

    /// <inheritdoc/>
    public async Task<ILease> TryAcquireNonBlocking(string leaseName, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(leaseName);
        await CreateEmptyFileIfNotExists(client, cancellationToken);
        var leaseClient = client.GetBlobLeaseClient();

        try
        {
            var lease = await LeaseTelemetry.RecordLeaseAcquire(Logger,leaseName,async () => await leaseClient.AcquireAsync(MaxLeaseTime, default, cancellationToken));
            return new StorageAccountLease(Logger, client, leaseClient, lease);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.LeaseAlreadyPresent)
        {
            return null;
        }
    }

    private static async Task CreateEmptyFileIfNotExists(BlobClient client, CancellationToken cancellationToken)
    {
        try
        {
            if (await client.ExistsAsync(cancellationToken))
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes("{}");
            using var stream = new MemoryStream(bytes);
            await client.UploadAsync(stream, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
        {
            // Blob already exists.
        }
    }

    internal BlobClient CreateClient(string blobName) =>
        Factory.CreateClient(AltinnLeaseOptions.StorageAccountLease.Name).GetBlobContainerClient(AltinnLeaseOptions.StorageAccountLease.Container).GetBlobClient(blobName);
}
