using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;

namespace Altinn.AccessMgmt.Persistence.Core.Executors;

/// <summary>
/// Interface for executing database commands and queries.
/// </summary>
public interface IDbExecutor
{
    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="query">Command to execute</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> ExecuteCommand(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="query">Command to execute</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> ExecuteMigrationCommand(string query, List<GenericParameter> parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="query">Command to execute</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> ExecuteCommand(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
    where T : new();

    /// <summary>
    /// Execute a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<T>> ExecuteQuery<T>(string query, CancellationToken cancellationToken = default)
    where T : new();

    /// <summary>
    /// Execute a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<T>> ExecuteMigrationQuery<T>(string query, CancellationToken cancellationToken = default)
    where T : new();

    /// <summary>
    /// Performs a bulk ingest operation by importing a list of entities into the database.
    /// </summary>
    /// <param name="data">The list of entities to import.</param>
    /// <param name="definition">DbDefinition</param>
    /// <param name="queryBuilder">IDbQueryBuilder</param>
    /// <param name="batchSize">Batch size (default: 1000)</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the number of entities successfully ingested.
    /// </returns>
    Task<int> Ingest<T>(List<T> data, DbDefinition definition, IDbQueryBuilder queryBuilder, int batchSize = 1000, CancellationToken cancellationToken = default)
    where T : new();

    /// <summary>
    /// Performs a bulk ingest operation by importing a list of entities into the database and then running a MERGE statement.
    /// </summary>
    /// <param name="data">The list of entities to import.</param>
    /// <param name="definition">DbDefinition</param>
    /// <param name="queryBuilder">IDbQueryBuilder</param>
    /// <param name="batchSize">Batch size (default: 1000)</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the number of entities successfully ingested.
    /// </returns>
    Task<int> IngestAndMerge<T>(List<T> data, DbDefinition definition, IDbQueryBuilder queryBuilder, int batchSize = 1000, CancellationToken cancellationToken = default)
    where T : new();
}
