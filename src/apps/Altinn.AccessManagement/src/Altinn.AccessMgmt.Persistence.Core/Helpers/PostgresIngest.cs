using System.Data;
using System.Reflection;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Executors;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.AccessMgmt.Persistence.Core.Helpers;

/// <summary>
/// Handles bulk importing of data using PostgreSQL's binary COPY.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PostgresIngest{T}"/> class.
/// </remarks>
/// <param name="options"><see cref="IOptions{TOptions}"/></param>
/// <param name="definition">DbDefinition</param>
public class PostgresIngest<T>(IOptions<AccessMgmtPersistenceOptions> options, DbDefinition definition) : IDbIngest<T>
    where T : class, new()
{
    private readonly IOptions<AccessMgmtPersistenceOptions> config = options;
    private readonly DbDefinition _definition = definition;

    /// <summary>
    /// Ingests a list of data into the database using PostgreSQL's binary COPY.
    /// </summary>
    /// <param name="data">Data</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public Task<int> Ingest(List<T> data, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        /*
        using var conn = await _connection.OpenConnectionAsync();
        var dt = new DataTable();

        // Use a simple query to get a sample structure.
        var dataAdapter = new NpgsqlDataAdapter($"SELECT * FROM {GetPostgresDefinition(includeAlias: false)} LIMIT 10", conn);
        dataAdapter.Fill(dt);
        dt.Clear();

        var columns = new Dictionary<string, (NpgsqlDbType Type, PropertyInfo Property)>();

        foreach (DataColumn c in dt.Columns)
        {
            if (!_definition.Properties.Exists(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }

            columns.Add(c.ColumnName, (GetPostgresType(c.DataType), _definition.Properties.First(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)).Property));
        }

        using var writer = await conn.BeginBinaryImportAsync(
            $"COPY {GetPostgresDefinition(includeAlias: false)} ({string.Join(',', columns.Keys)}) FROM STDIN (FORMAT BINARY)",
            cancellationToken: cancellationToken);
        writer.Timeout = TimeSpan.FromMinutes(10);
        int batchCompleted = 0;
        int batchSize = 10000;
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
                    Console.WriteLine($"Failed to write data in column '{c.Key}' for '{_definition.ModelType.Name}'. Trying to write null. " + ex.Message);
                    try
                    {
                        writer.WriteNull();
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to write null in column '{c.Key}' for '{_definition.ModelType.Name}'.");
                        throw;
                    }
                }
            }

            completed++;
            if (completed == batchSize)
            {
                batchCompleted++;
                completed = 0;
                Console.WriteLine($"Ingested {batchCompleted * batchSize + completed}");
            }
        }

        Console.WriteLine($"Ingested {batchCompleted * batchSize + completed}");
        writer.Complete();

        return batchCompleted * batchSize + completed;
        */
    }

    private string GetPostgresDefinition(bool includeAlias = true, bool useHistory = false, bool useTranslation = false)
    {
        var config = this.config.Value;
        string res;
        if (useHistory)
        {
            res = useTranslation
                ? $"{config.TranslationHistorySchema}.{_definition.ModelType.Name}"
                : $"{config.BaseHistorySchema}.{_definition.ModelType.Name}";
        }
        else
        {
            res = useTranslation
                ? $"{config.TranslationSchema}.{_definition.ModelType.Name}"
                : $"{config.BaseSchema}.{_definition.ModelType.Name}";
        }

        if (includeAlias)
        {
            res += $" AS {_definition.ModelType.Name}";
        }

        return res;
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
