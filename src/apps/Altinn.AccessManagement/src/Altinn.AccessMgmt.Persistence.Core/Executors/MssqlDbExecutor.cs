using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
    public async Task<int> ExecuteCommand(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
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
    public async Task<int> ExecuteCommand(string query, CancellationToken cancellationToken = default)
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

    public Task<int> ExecuteMigrationCommand(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<T>> ExecuteMigrationQuery<T>(string query, CancellationToken cancellationToken = default) where T : new()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> ExecuteQuery<T>(string query, List<GenericParameter> parameters, CancellationToken cancellationToken = default) 
        where T : new()
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        cmd.Parameters.AddRange(parameters.ToArray());
        return dbConverter.ConvertToObjects<T>(await cmd.ExecuteReaderAsync(cancellationToken));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> ExecuteQuery<T>(string query, CancellationToken cancellationToken = default) 
        where T : new()
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        return dbConverter.ConvertToObjects<T>(await cmd.ExecuteReaderAsync(cancellationToken));
    }
}
