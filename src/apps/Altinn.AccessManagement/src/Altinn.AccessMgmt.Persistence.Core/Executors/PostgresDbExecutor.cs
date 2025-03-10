using System.Data;
using System.Reflection;
using System.Text;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Altinn.AccessMgmt.Persistence.Core.Utilities;
using Altinn.Authorization.Host.Database;
using Altinn.Authorization.Host.Startup;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

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

    /// <inheritdoc />
    public async Task<int> Ingest<T>(List<T> data, DbDefinition definition, IDbQueryBuilder queryBuilder, int batchSize = 1000, CancellationToken cancellationToken = default) 
    where T : new()
    {
        var type = typeof(T);

        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        var dt = new DataTable();

        // Use a simple query to get a sample structure.
        var dataAdapter = new NpgsqlDataAdapter($"SELECT * FROM {queryBuilder.GetTableName(includeAlias: false)} LIMIT 10", conn);
        dataAdapter.Fill(dt);
        dt.Clear();

        var columns = new Dictionary<string, (NpgsqlDbType Type, PropertyInfo Property)>();

        foreach (DataColumn c in dt.Columns)
        {
            if (!definition.Properties.Exists(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }

            columns.Add(c.ColumnName, (GetPostgresType(c.DataType), definition.Properties.First(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)).Property));
        }

        string columnStatement = string.Join(',', columns.Keys);
        using var writer = await conn.BeginBinaryImportAsync($"COPY {queryBuilder.GetTableName(includeAlias: false)} ({columnStatement}) FROM STDIN (FORMAT BINARY)", cancellationToken: cancellationToken);
        writer.Timeout = TimeSpan.FromMinutes(10);
        int batchCompleted = 0;
        int completed = 0;
        foreach (var d in data)
        {
            writer.StartRow();
            foreach (var c in columns)
            {
                try
                {
                    writer.Write(c.Value.Property.GetValue(d), c.Value.Type);
                }
                catch (Exception ex)
                {
                    // Replace with proper logging.
                    Console.WriteLine($"Failed to write data in column '{c.Key}' for '{definition.ModelType.Name}'. Trying to write null. " + ex.Message);
                    try
                    {
                        writer.WriteNull();
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to write null in column '{c.Key}' for '{definition.ModelType.Name}'.");
                        throw;
                    }
                }
            }

            completed++;
            if (completed == batchSize)
            {
                batchCompleted++;
                completed = 0;
                Console.WriteLine($"Ingested {(batchCompleted * batchSize) + completed}");
            }
        }

        Console.WriteLine($"Ingested {(batchCompleted * batchSize) + completed}");
        writer.Complete();

        return (batchCompleted * batchSize) + completed;
    }

    /// <inheritdoc />
    public async Task<int> IngestAndMerge<T>(List<T> data, DbDefinition definition, IDbQueryBuilder queryBuilder, int batchSize = 1000, CancellationToken cancellationToken = default)
    where T : new()
    {
        var type = typeof(T);

        if (data.Count < batchSize)
        {
            batchSize = data.Count;
        }

        string tableName = queryBuilder.GetTableName(includeAlias: false); // dbo.Provider
        var ingestId = Guid.NewGuid().ToString().Replace("-", string.Empty);
        string ingestTableName = tableName + "_" + ingestId;

        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        var dt = new DataTable();

        // Use a simple query to get a sample structure.
        var dataAdapter = new NpgsqlDataAdapter($"SELECT * FROM {queryBuilder.GetTableName(includeAlias: false)} LIMIT 10", conn);
        dataAdapter.Fill(dt);
        dt.Clear();

        var columns = new Dictionary<string, (NpgsqlDbType Type, PropertyInfo Property)>();
        foreach (DataColumn c in dt.Columns)
        {
            if (!definition.Properties.Exists(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }

            columns.Add(c.ColumnName, (GetPostgresType(c.DataType), definition.Properties.First(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)).Property));
        }

        string columnStatement = string.Join(',', columns.Keys);

        var createIngestTable = $"CREATE TABLE IF NOT EXISTS {ingestTableName} AS SELECT {columnStatement} FROM {tableName} WITH NO DATA;";
        var dropIngestTable = $"DROP TABLE IF EXISTS {ingestTableName};";

        var mergeMatchStatement = string.Join(',', columns.Select(t => $"target.{t.Key} = source.{t.Key}")); // needed ?
        var mergeUpdateUnMatchStatement = string.Join(" OR ", columns.Select(t => $"target.{t.Key} <> source.{t.Key}"));
        var mergeUpdateStatement = string.Join(" , ", columns.Select(t => $"{t.Key} = source.{t.Key}"));
        var insertColumns = string.Join(" , ", columns.Select(t => $"{t.Key}"));
        var insertValues = string.Join(" , ", columns.Select(t => $"source.{t.Key}"));

        var sb = new StringBuilder();
        sb.AppendLine($"MERGE INTO {tableName} AS target USING {ingestTableName} AS source ON target.id = source.id"); // <= mergeMatchStatement ? 
        sb.AppendLine($"WHEN MATCHED AND ({mergeUpdateUnMatchStatement}) THEN ");
        sb.AppendLine($"UPDATE SET {mergeUpdateStatement}");
        sb.AppendLine($"WHEN NOT MATCHED THEN ");
        sb.AppendLine($"INSERT ({insertColumns}) VALUES ({insertValues});");
        string mergeStatement = sb.ToString();

        await ExecuteMigrationCommand(createIngestTable, null);

        using var writer = await conn.BeginBinaryImportAsync($"COPY {ingestTableName} ({columnStatement}) FROM STDIN (FORMAT BINARY)", cancellationToken: cancellationToken);
        writer.Timeout = TimeSpan.FromMinutes(10);
        int batchCompleted = 0;
        int completed = 0;
        foreach (var d in data)
        {
            writer.StartRow();
            foreach (var c in columns)
            {
                try
                {
                    writer.Write(c.Value.Property.GetValue(d), c.Value.Type);
                }
                catch (Exception ex)
                {
                    // Replace with better logging.
                    Console.WriteLine($"Failed to write data in column '{c.Key}' for '{definition.ModelType.Name}'. Trying to write null. " + ex.Message);
                    try
                    {
                        writer.WriteNull();
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to write null in column '{c.Key}' for '{definition.ModelType.Name}'.");
                        throw;
                    }
                }
            }

            completed++;
            if (completed == batchSize)
            {
                batchCompleted++;
                completed = 0;
                Console.WriteLine($"Ingested {(batchCompleted * batchSize) + completed}");
            }
        }

        writer.Complete();

        Console.WriteLine("Starting MERGE");
        await ExecuteMigrationCommand(mergeStatement, null);

        Console.WriteLine("Cleanup");
        await ExecuteMigrationCommand(dropIngestTable, null);

        Console.WriteLine($"Ingested {(batchCompleted * batchSize) + completed}");

        return (batchCompleted * batchSize) + completed;
    }

    private NpgsqlDbType GetPostgresType(Type type)
    {
        if (type == typeof(string))
        {
            return NpgsqlDbType.Text;
        }

        if (type == typeof(int))
        {
            return NpgsqlDbType.Integer;
        }

        if (type == typeof(DateTimeOffset))
        {
            return NpgsqlDbType.TimestampTz;
        }

        if (type == typeof(Guid))
        {
            return NpgsqlDbType.Uuid;
        }

        if (type == typeof(bool))
        {
            return NpgsqlDbType.Boolean;
        }

        Console.WriteLine($"Type converter not found for '{type.Name}'");
        return NpgsqlDbType.Text;
    }
}
