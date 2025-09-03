namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Defines the contract for managing leases in a system.
/// Provides asynchronous, non-blocking operations for acquiring, updating, releasing, and refreshing leases.
/// This interface allows safe coordination of access to shared resources using lease semantics.
/// </summary>
public interface ILeaseService
{
    /// <summary>
    /// Attempts to acquire a lease for a given resource without blocking.
    /// If the lease is already held by another caller, the method does not throw an exception
    /// but instead returns a lease object that indicates its acquisition status.
    /// </summary>
    /// <param name="leaseName">
    /// The unique name of the lease to acquire.
    /// Typically corresponds to the resource or key you want to lock.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Default is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task{ILease}"/> representing the asynchronous operation.
    /// The returned <see cref="ILease"/> instance contains the leased data and indicates
    /// whether the lease was successfully acquired.
    /// </returns>
    Task<ILease> TryAcquireNonBlocking(string leaseName, CancellationToken cancellationToken = default);
}
