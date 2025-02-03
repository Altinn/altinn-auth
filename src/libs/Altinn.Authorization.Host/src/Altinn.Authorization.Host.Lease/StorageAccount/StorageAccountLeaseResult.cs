using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace Altinn.Authorization.Host.Lease.StorageAccount;

/// <summary>
/// Represents the result of a lease acquisition operation for a blob in the storage account.
/// Contains information about the lease, the associated blob, and the lease client.
/// </summary>
/// <typeparam name="T">The type of data associated with the lease.</typeparam>
internal class StorageAccountLeaseResult<T> : LeaseResult<T>
    where T : class
{
    private bool _disposed = false;

    /// <inheritdoc/>
    public override bool HasLease => !string.IsNullOrEmpty(Response?.LeaseId);

    /// <summary>
    /// Gets the <see cref="BlobClient"/> instance used to interact with the associated blob.
    /// </summary>
    internal BlobClient BlobClient { get; init; }

    /// <summary>
    /// Gets the <see cref="BlobLeaseClient"/> instance used to manage the lease for the associated blob.
    /// </summary>
    internal BlobLeaseClient LeaseClient { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="BlobLease"/> representing the lease on the associated blob.
    /// </summary>
    internal BlobLease Response { get; set; }

    /// <summary>
    /// Gets the date and time when the lease was acquired.
    /// </summary>
    internal DateTime Acquired { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the <see cref="IAltinnLease"/> implementation associated with this lease result.
    /// </summary>
    internal IAltinnLease Implementation { get; init; }

    /// <summary>
    /// Releases the lease synchronously when the object is disposed.
    /// </summary>
    public override void Dispose()
    {
        if (!_disposed)
        {
            Implementation.Release(this, default).GetAwaiter().GetResult();
        }

        _disposed = true;
    }

    /// <summary>
    /// Releases the lease asynchronously when the object is disposed.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Implementation.Release(this, default);
        }

        _disposed = true;
    }
}
