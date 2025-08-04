using System.Data;
using System.Runtime.CompilerServices;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Utilities;
using Altinn.Authorization.Host.Database;
using Altinn.Authorization.Host.Database.Extensions;
using Altinn.Authorization.Host.Startup;
using Altinn.Authorization.ServiceDefaults.Npgsql;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.AccessMgmt.Persistence.Core.Executors;

/// <summary>
/// Responsible for executing SQL commands and queries.
/// </summary>
public class PostgresDbExecutor(IAltinnDatabase databaseFactory, IDbConverter dbConverter) : IDbExecutor
{
    /// <summary>
    /// Logger instance for logging database configuration messages.
    /// </summary>
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(PostgresDbExecutor));

    private readonly IAltinnDatabase _databaseFactory = databaseFactory;
    private readonly IDbConverter _dbConverter = dbConverter;

    /// <summary>
    /// Temp Connection Exposure for Ingest
    /// </summary>
    /// <param name="sourceType">SourceType (App, Migrate)</param>
    /// <returns></returns>
    public NpgsqlConnection GetConnection(SourceType sourceType)
    {
        return _databaseFactory.CreatePgsqlConnection(sourceType);
    }

    /// <summary>
    /// Executes a query and maps the result to objects of type T.
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteMigrationQuery<T>(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        where T : new()
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        await openConnection;
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync(CommandBehavior.SingleResult, callerName, cancellationToken);
        return _dbConverter.ConvertToResult<T>(reader).Data;
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteMigrationCommand(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }

        await openConnection;
        return await cmd.ExecuteNonQueryWithSpanNameAsync(callerName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteCommand(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        foreach (var parameter in parameters)
        {
            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }

        await openConnection;
        return await cmd.ExecuteNonQueryWithSpanNameAsync(callerName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteCommand(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        await openConnection;
        return await cmd.ExecuteNonQueryWithSpanNameAsync(callerName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a query and maps the result to objects of type T.
    /// </summary>
    public async Task<QueryResponse<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        where T : new()
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        foreach (var parameter in parameters)
        {
            if (parameter.Value is null)
            {
                cmd.Parameters.Add(new NpgsqlParameter(parameter.Key, DBNull.Value));
            }
            else
            {
                cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }

        await openConnection;
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync(CommandBehavior.SingleResult, callerName, cancellationToken);
        return _dbConverter.ConvertToResult<T>(reader);
    }

    /// <summary>
    /// Execute a custom query and return data mapped to return type using the provided mapping function.
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="map">Function to map the data reader to the specific return type</param>
    /// <param name="callerName">Used for setting span name.</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task<IEnumerable<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, Func<NpgsqlDataReader, ValueTask<T>> map, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        foreach (var parameter in parameters)
        {
            if (parameter.Value is null)
            {
                cmd.Parameters.Add(new NpgsqlParameter(parameter.Key, DBNull.Value));
            }
            else
            {
                cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }

        await openConnection;

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync(CommandBehavior.SingleResult, callerName, cancellationToken);

        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(await map(reader));
        }

        return results;
    }

    /// <summary>
    /// Executes a query and maps the result to objects of type T.
    /// </summary>
    public async Task<QueryResponse<T>> ExecuteQuery<T>(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        where T : new()
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var openConnection = conn.OpenAsync(cancellationToken);
        var cmd = conn.CreateCommand(query);

        await openConnection;
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync(CommandBehavior.SingleResult, callerName, cancellationToken);
        return _dbConverter.ConvertToResult<T>(reader);
    }

    /// <summary>
    /// Formats the parameters for App Insights logging
    /// </summary>
    private string FormatParameters(List<GenericParameter> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return "None";
        }

        var formattedParameters = parameters.Select(p => $"{p.Key}: {p.Value}");
        return string.Join(", ", formattedParameters);
    }
}
