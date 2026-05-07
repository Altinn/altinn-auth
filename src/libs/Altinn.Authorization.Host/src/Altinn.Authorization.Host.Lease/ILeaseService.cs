namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Defines the contract for managing leases in a system.
/// Provides asynchronous, non-blocking operations for acquiring, updating, releasing, and refreshing leases.
/// This interface allows safe coordination of access to shared resources using lease semantics.
/// </summary>
public interface ILeaseService
{
    /// <summary>
    /// Attempts to acquire a lease for the specified resource without waiting.
    /// </summary>
    /// <param name="leaseName">
    /// The unique name of the lease to acquire. This typically corresponds to the resource or key to lock.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that returns the acquired <see cref="ILease"/>, or <see langword="null"/> if the lease
    /// is already held by another caller.
    /// </returns>
    Task<ILease?> TryAcquireNonBlocking(string leaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a lease for the specified resource, retrying every second until the lease is
    /// acquired or the operation is cancelled.
    /// </summary>
    /// <param name="leaseName">
    /// The unique name of the lease to acquire. This typically corresponds to the resource or key to lock.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that returns the acquired <see cref="ILease"/>.
    /// </returns>
    Task<ILease> AcquireBlocking(string leaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a lease for the specified resource, retrying at the specified interval until
    /// the lease is acquired or the operation is cancelled.
    /// </summary>
    /// <param name="leaseName">
    /// The unique name of the lease to acquire. This typically corresponds to the resource or key to lock.
    /// </param>
    /// <param name="retry">
    /// The delay between acquisition attempts.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that returns the acquired <see cref="ILease"/>.
    /// </returns>
    Task<ILease> AcquireBlocking(string leaseName, TimeSpan retry, CancellationToken cancellationToken = default);
}
