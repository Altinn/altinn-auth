namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Represents the result of a lease operation.
/// Encapsulates leased data and provides methods to read or update the data safely.
/// Implements asynchronous disposal to ensure proper release of resources (such as locks or leases)
/// when the lease is no longer needed.
/// </summary>
public interface ILease : IAsyncDisposable
{
    /// <summary>
    /// Retrieves the data associated with the lease.
    /// </summary>
    /// <typeparam name="T">The type of the data to retrieve. Must be a reference type with a parameterless constructor.</typeparam>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Default is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task{T}"/> that represents the asynchronous operation.
    /// The task result contains the leased data of type <typeparamref name="T"/>.
    /// </returns>
    Task<T> Get<T>(CancellationToken cancellationToken = default)
        where T : class, new();

    /// <summary>
    /// Updates the leased data with a new value.
    /// </summary>
    /// <typeparam name="T">The type of the data to update. Must be a reference type with a parameterless constructor.</typeparam>
    /// <param name="data">The new data to store in the lease.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Default is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous update operation.</returns>
    Task Update<T>(T data, CancellationToken cancellationToken = default)
        where T : class, new();

    /// <summary>
    /// Updates the leased data by applying a configuration action to it.
    /// This allows in-place modification without replacing the entire object.
    /// </summary>
    /// <typeparam name="T">The type of the data to update. Must be a reference type with a parameterless constructor.</typeparam>
    /// <param name="configureData">An action that receives the current data instance to modify it.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Default is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous update operation.</returns>
    Task Update<T>(Action<T> configureData, CancellationToken cancellationToken = default)
        where T : class, new();
}
