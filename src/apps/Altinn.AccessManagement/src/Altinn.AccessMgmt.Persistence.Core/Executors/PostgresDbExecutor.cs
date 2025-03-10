using System.Data;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Utilities;
using Altinn.Authorization.Host.Database;
using Altinn.Authorization.Host.Startup;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.AccessMgmt.Persistence.Core.Executors;

/// <summary>
/// Responsible for executing SQL commands and queries.
/// </summary>
public class PostgresDbExecutor(IAltinnDatabase databaseFactory, NpgsqlDataSource connection, IDbConverter dbConverter) : IDbExecutor
{
    /// <summary>
    /// Logger instance for logging database configuration messages.
    /// </summary>
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(PostgresDbExecutor));

    private readonly IAltinnDatabase _databaseFactory = databaseFactory;
    private readonly NpgsqlDataSource _connection = connection;
    private readonly IDbConverter _dbConverter = dbConverter;

    /// <summary>
    /// Executes a query and maps the result to objects of type T.
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteMigrationQuery<T>(string query, CancellationToken cancellationToken = default)
        where T : new()
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        conn.Open();
        return _dbConverter.ConvertToObjects<T>(await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken));
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteMigrationCommand(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
            var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }

            conn.Open();
            return await cmd.ExecuteNonQueryAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(query);
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteCommand(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        try
        {
            conn.Open();
            foreach (var parameter in parameters)
            {
                cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }

            return await cmd.ExecuteNonQueryAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(query);
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteCommand(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
            var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            conn.Open();
            return await cmd.ExecuteNonQueryAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(query);
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes a query and maps the result to objects of type T.
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
        where T : new()
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        conn.Open();
        foreach (var parameter in parameters)
        {
            cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }

        return _dbConverter.ConvertToObjects<T>(await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken));
    }

    /// <summary>
    /// Executes a query and maps the result to objects of type T.
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteQuery<T>(string query, CancellationToken cancellationToken = default)
        where T : new()
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.App);
        var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        conn.Open();
        return _dbConverter.ConvertToObjects<T>(await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken));
    }
}
