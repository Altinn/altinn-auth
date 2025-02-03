namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Defines the contract for managing leases, including acquiring, storing data, releasing, and refreshing leases.
/// This interface allows operations to be performed on leases in an asynchronous, non-blocking manner.
/// </summary>
public interface IAltinnLease
{
    /// <summary>
    /// Attempts to acquire a lease without blocking.
    /// This method checks for the availability of a lease and returns the result of the acquisition.
    /// </summary>
    /// <typeparam name="T">The type of data being leased.</typeparam>
    /// <param name="leaseName">The name of the lease to acquire.</param>
    /// <param name="cancellationToken">A token that can be used to observe and handle cancellation requests.</param>
    /// <returns>A task that represents the operation, containing the acquired lease.</returns>
    Task<LeaseResult<T>> TryAquireNonBlocking<T>(string leaseName, CancellationToken cancellationToken = default)
        where T : class, new();

    /// <summary>
    /// Attempts to store data in an existing lease.
    /// If the lease already exists, the data is updated; otherwise, the lease is created with the new data.
    /// </summary>
    /// <typeparam name="T">The type of data being stored in the lease.</typeparam>
    /// <param name="lease">The lease that will hold the data.</param>
    /// <param name="data">The data to be stored in the lease.</param>
    /// <param name="cancellationToken">A token to observe and handle cancellation requests.</param>
    /// <returns>A task that represents the operation, containing the updated lease.</returns>
    Task<LeaseResult<T>> Put<T>(LeaseResult<T> lease, T data, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Releases an existing lease, making it available for reuse by others.
    /// This operation ensures that the lease is no longer in use and can be re-acquired or reassigned.
    /// </summary>
    /// <typeparam name="T">The type of data being leased.</typeparam>
    /// <param name="lease">The lease to be released.</param>
    /// <param name="cancellationToken">A token to observe and handle cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation of releasing the lease.</returns>
    Task Release<T>(LeaseResult<T> lease, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Refreshes an existing lease to either extend its duration or re-validate its status.
    /// This operation ensures that the lease remains active and prevents premature expiration.
    /// If the lease is returned with <see cref="LeaseResult{T}.HasLease"/> set to <c>false</c>, it indicates that
    /// the lease has been lost and the refresh attempt was made after the lease expired.
    /// </summary>
    /// <typeparam name="T">The type of data being leased.</typeparam>
    /// <param name="lease">The lease to be refreshed.</param>
    /// <param name="cancellationToken">A token to observe and handle cancellation requests.</param>
    /// <returns>A task that represents the operation, containing the refreshed lease.</returns>
    Task<LeaseResult<T>> RefreshLease<T>(LeaseResult<T> lease, CancellationToken cancellationToken = default)
        where T : class;
}
