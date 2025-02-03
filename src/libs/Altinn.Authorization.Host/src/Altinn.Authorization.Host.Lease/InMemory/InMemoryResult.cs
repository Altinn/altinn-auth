namespace Altinn.Authorization.Host.Lease.InMemory;

/// <summary>
/// Represents the result of an in-memory lease operation.
/// This class ensures that leases are properly released when disposed.
/// </summary>
/// <typeparam name="T">The type of data being leased.</typeparam>
internal class InMemoryResult<T> : LeaseResult<T>
    where T : class
{
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the lease is currently held.
    /// </summary>
    public override bool HasLease => SetLease;

    /// <summary>
    /// Indicates whether the lease is set.
    /// </summary>
    internal bool SetLease { set; get; }

    /// <summary>
    /// The lease implementation that manages the lease lifecycle.
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
