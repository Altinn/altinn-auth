using System.Runtime.CompilerServices;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Utilities;
using Microsoft.Data.SqlClient;

namespace Altinn.AccessMgmt.Persistence.Core.Executors;

/// <summary>
/// Responsible for executing SQL commands and queries.
/// </summary>
/// <param name="connection">SqlConnection</param>
/// <param name="dbConverter">IDbConverter</param>
public class MssqlDbExecutor(SqlConnection connection, IDbConverter dbConverter) : IDbExecutor
{
    /// <inheritdoc/>
    public async Task<int> ExecuteCommand(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.Parameters.AddRange(parameters.ToArray());

            connection.Open();
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(query);
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = ConsoleColor.White;
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> ExecuteCommand(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            connection.Open();
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(query);
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = ConsoleColor.White;
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<int> ExecuteMigrationCommand(string query, List<GenericParameter> parameters = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<T>> ExecuteMigrationQuery<T>(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        where T : new()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<QueryResponse<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        where T : new()
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        cmd.Parameters.AddRange(parameters.ToArray());
        return dbConverter.ConvertToResult<T>(await cmd.ExecuteReaderAsync(cancellationToken));
    }

    /// <inheritdoc/>
    public async Task<QueryResponse<T>> ExecuteQuery<T>(string query, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        where T : new()
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        return dbConverter.ConvertToResult<T>(await cmd.ExecuteReaderAsync(cancellationToken));
    }
}
