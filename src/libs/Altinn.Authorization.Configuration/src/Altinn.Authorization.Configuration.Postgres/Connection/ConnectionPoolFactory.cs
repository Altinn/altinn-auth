using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace Altinn.Authorization.Configuration.Postgres.Connection;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public abstract class ConnectionPoolFactory : IPostgresConnectionPoolFactory
{
    /// <summary>
    /// Gets or sets the PostgreSQL connection pool represented by <see cref="NpgsqlDataSource"/>.
    /// This connection pool is used to manage multiple connections to the PostgreSQL database efficiently.
    /// </summary>
    protected NpgsqlDataSource ConnectionPool { get; set; }
    
    /// <summary>
    /// A semaphore to control concurrent access to the connection pool creation.
    /// Ensures that only one connection pool is created at a time.
    /// </summary>
    protected SemaphoreSlim Semaphore { get; } = new(1);

    /// <inheritdoc />
    public abstract Task<NpgsqlDataSource> Create(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface that defines the contract for creating and managing PostgreSQL connection pools.
/// Implementing classes are responsible for creating and maintaining connection pools
/// that are used to manage connections to PostgreSQL databases.
/// </summary>
public interface IPostgresConnectionPoolFactory
{
    /// <summary>
    /// Creates and returns a PostgreSQL connection pool represented by <see cref="NpgsqlDataSource"/>.
    /// This pool allows for efficient management of connections to the PostgreSQL database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation of creating a PostgreSQL connection pool.</returns>
    Task<NpgsqlDataSource> Create(CancellationToken cancellationToken = default);
}
