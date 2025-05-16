using System.Runtime.CompilerServices;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;

namespace Altinn.AccessMgmt.Persistence.Core.Contracts;

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
    Task<int> ExecuteCommand(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="query">Command to execute</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> ExecuteMigrationCommand(string query, List<GenericParameter> parameters = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command
    /// </summary>
    /// <param name="query">Command to execute</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> ExecuteCommand(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<QueryResponse<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    where T : new();

    /// <summary>
    /// Execute a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<QueryResponse<T>> ExecuteQuery<T>(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    where T : new();

    /// <summary>
    /// Execute a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<T>> ExecuteMigrationQuery<T>(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    where T : new();
}
