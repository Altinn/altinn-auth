namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Represents the result of a lease operation, encapsulating the leased data and its status.
/// This abstract class ensures proper resource management through both synchronous and asynchronous disposal.
/// </summary>
/// <typeparam name="T">The type of data being leased.</typeparam>
public abstract class LeaseResult<T> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the data associated with the lease.
    /// The data is set when acquiring the lease or by calling
    /// <see cref="IAltinnLease.Put{T}(LeaseResult{T}, T, CancellationToken)"/>.
    /// </summary>
    public T Data { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the lease is currently held.
    /// </summary>
    public abstract bool HasLease { get; }

    /// <summary>
    /// Gets the unique name of the lease.
    /// This is used to identify and manage leased resources.
    /// </summary>
    internal string LeaseName { get; init; }

    /// <summary>
    /// Releases the lease synchronously, if applicable.
    /// This method should be implemented in derived classes to ensure proper resource cleanup.
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// Releases the lease asynchronously.
    /// This method should be implemented in derived classes to support proper async cleanup.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
    public abstract ValueTask DisposeAsync();
}
