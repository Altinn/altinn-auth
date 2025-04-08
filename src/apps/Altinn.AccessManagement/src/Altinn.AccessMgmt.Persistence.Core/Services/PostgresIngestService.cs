using System.Data;
using System.Reflection;
using System.Text;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Altinn.Authorization.Host.Database;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <inheritdoc />
public class PostgresIngestService(IAltinnDatabase databaseFactory, IDbExecutor dbExecutor, DbDefinitionRegistry definitionRegistry) : IIngestService
{
    private readonly IAltinnDatabase _databaseFactory = databaseFactory;
    private readonly IDbExecutor dbExecutor = dbExecutor;
    private readonly DbDefinitionRegistry definitionRegistry = definitionRegistry;

    /// <inheritdoc />
    public async Task<int> IngestData<T>(List<T> data, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var definition = definitionRegistry.TryGetDefinition<T>() ?? throw new Exception(string.Format("Definition not found for '{0}'", type.Name));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();

        string tableName = queryBuilder.GetTableName(includeAlias: false);
        var ingestColumns = GetColumns(definition, queryBuilder);

        return await WriteToIngest(data, ingestColumns, tableName, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> IngestTempData<T>(List<T> data, Guid ingestId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        if (ingestId.Equals(Guid.Empty))
        {
            throw new Exception(string.Format("Ingest id '{0}' not valid", ingestId.ToString()));
        }

        var type = typeof(T);
        var definition = definitionRegistry.TryGetDefinition<T>() ?? throw new Exception(string.Format("Definition not found for '{0}'", type.Name));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();

        var ingestColumns = GetColumns(definition, queryBuilder);
        string columnStatement = string.Join(',', ingestColumns.Select(t => t.Name));

        string tableName = queryBuilder.GetTableName(includeAlias: false);
        var ingestName = ingestId.ToString().Replace("-", string.Empty);
        string ingestTableName = "ingest." + queryBuilder.GetTableName(includeAlias: false, includeSchema: false) + "_" + ingestName;

        var createIngestTable = $"CREATE UNLOGGED TABLE IF NOT EXISTS {ingestTableName} AS SELECT {columnStatement} FROM {tableName} WITH NO DATA;";
        await dbExecutor.ExecuteMigrationCommand(createIngestTable, null, cancellationToken);
        
        var completed = await WriteToIngest(data, ingestColumns, ingestTableName, options, cancellationToken);

        return completed;
    }

    /// <inheritdoc />
    public async Task<int> MergeTempData<T>(Guid ingestId, ChangeRequestOptions options, IEnumerable<GenericParameter> matchColumns = null, CancellationToken cancellationToken = default)
    {
        if (matchColumns == null || matchColumns.Count() == 0)
        {
            matchColumns = [new GenericParameter("id", "id")];
        }

        var type = typeof(T);
        var definition = definitionRegistry.TryGetDefinition<T>() ?? throw new Exception(string.Format("Definition not found for '{0}'", type.Name));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();

        var ingestColumns = GetColumns(definition, queryBuilder);
        string columnStatement = string.Join(',', ingestColumns.Select(t => t.Name));

        string tableName = queryBuilder.GetTableName(includeAlias: false);
        var ingestName = ingestId.ToString().Replace("-", string.Empty);
        string ingestTableName = "ingest." + queryBuilder.GetTableName(includeAlias: false, includeSchema: false) + "_" + ingestName;

        var mergeMatchStatement = string.Join(" AND ", matchColumns.Select(t => $"target.{t.Key} = source.{t.Key}"));
        var mergeUpdateUnMatchStatement = string.Join(" OR ", ingestColumns.Where(t => matchColumns.Count(y => y.Key.Equals(t.Name, StringComparison.OrdinalIgnoreCase)) == 0).Select(t => $"target.{t.Name} <> source.{t.Name}"));
        var mergeUpdateStatement = string.Join(" , ", ingestColumns.Where(t => matchColumns.Count(y => y.Key.Equals(t.Name, StringComparison.OrdinalIgnoreCase)) == 0).Select(t => $"{t.Name} = source.{t.Name}"));
        var insertColumns = string.Join(" , ", ingestColumns.Select(t => $"{t.Name}"));
        var insertValues = string.Join(" , ", ingestColumns.Select(t => $"source.{t.Name}"));

        var sb = new StringBuilder();

        sb.AppendLine(GetAuditVariables(options));
        sb.AppendLine($"MERGE INTO {tableName} AS target USING {ingestTableName} AS source ON {mergeMatchStatement}");
        
        if (type.Name != "Assignment")
        {
            sb.AppendLine($"WHEN MATCHED AND ({mergeUpdateUnMatchStatement}) THEN ");
            sb.AppendLine($"UPDATE SET {mergeUpdateStatement}");
        }
            
        sb.AppendLine($"WHEN NOT MATCHED THEN ");
        sb.AppendLine($"INSERT ({insertColumns}) VALUES ({insertValues});");

        string mergeStatement = sb.ToString();

        Console.WriteLine("Starting MERGE");

        var res = await dbExecutor.ExecuteMigrationCommand(mergeStatement, null, cancellationToken);

        Console.WriteLine("Cleanup");
        var dropIngestTable = $"DROP TABLE IF EXISTS {ingestTableName};";
        await dbExecutor.ExecuteMigrationCommand(dropIngestTable, null, cancellationToken);

        Console.WriteLine($"Merged {res}");

        return res;
    }

    /// <inheritdoc />
    public async Task<int> IngestAndMergeData<T>(List<T> data, ChangeRequestOptions options, IEnumerable<GenericParameter> matchColumns = null, CancellationToken cancellationToken = default)
    {
        var ingestId = Guid.CreateVersion7();
        await IngestTempData(data, ingestId, options, cancellationToken);
        var res = await MergeTempData<T>(ingestId, options, matchColumns, cancellationToken);

        return res;
    }

    private async Task<int> WriteToIngest<T>(List<T> data, List<IngestColumnDefinition> ingestColumns, string tableName, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        string columnStatement = string.Join(',', ingestColumns.Select(t => t.Name));
        using var writer = await conn.BeginBinaryImportAsync($"COPY {tableName} ({columnStatement}) FROM STDIN (FORMAT BINARY)", cancellationToken: cancellationToken);
        writer.Timeout = TimeSpan.FromMinutes(10);
        int completed = 0;
        foreach (var d in data)
        {
            writer.StartRow();
            foreach (var c in ingestColumns)
            {
                try
                {
                    writer.Write(c.Property.GetValue(d), c.Type);
                }
                catch (Exception ex)
                {
                    // Replace with proper logging.
                    Console.WriteLine($"Failed to write data in column '{c.Name}' for '{tableName}'. Trying to write null. " + ex.Message);
                    try
                    {
                        writer.WriteNull();
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to write null in column '{c.Name}' for '{tableName}'.");
                        throw;
                    }
                }
            }

            completed++;
        }

        writer.Complete();

        Console.WriteLine($"Ingested {completed}");
        return completed;
    }

    private Dictionary<Type, List<IngestColumnDefinition>> TypedIngestColumnDefinitions { get; set; } = new Dictionary<Type, List<IngestColumnDefinition>>();

    private List<IngestColumnDefinition> GetColumns(DbDefinition definition, IDbQueryBuilder queryBuilder)
    {
        if (TypedIngestColumnDefinitions.ContainsKey(definition.ModelType))
        {
            return TypedIngestColumnDefinitions[definition.ModelType];
        }

        using var conn = _databaseFactory.CreatePgsqlConnection(SourceType.Migration);
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        var tableName = queryBuilder.GetTableName();
        var dt = new DataTable();
        var dataAdapter = new NpgsqlDataAdapter($"SELECT * FROM {tableName} LIMIT 10", conn);
        dataAdapter.Fill(dt);
        dt.Clear();

        var columns = new List<IngestColumnDefinition>();

        foreach (DataColumn c in dt.Columns)
        {
            if (!definition.Properties.Exists(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }

            columns.Add(new IngestColumnDefinition()
            {
                Name = c.ColumnName,
                Property = definition.Properties.First(t => t.Name.Equals(c.ColumnName, StringComparison.CurrentCultureIgnoreCase)).Property,
                Type = GetPostgresType(c.DataType)                
            });
        }

        TypedIngestColumnDefinitions.Add(definition.ModelType, columns);
        return columns;
    }

    private static string GetAuditVariables(ChangeRequestOptions options)
    {
        return string.Format("SET LOCAL app.changed_by = '{0}'; SET LOCAL app.changed_by_system = '{1}'; SET LOCAL app.change_operation_id = '{2}';", options.ChangedBy, options.ChangedBySystem, options.ChangeOperationId);
    }

    /// <summary>
    /// Definition of column to ingest
    /// </summary>
    internal class IngestColumnDefinition
        {
            /// <summary>
            /// Column name
            /// </summary>
            internal string Name { get; set; }

            /// <summary>
            /// Db data type
            /// </summary>
            internal NpgsqlDbType Type { get; set; }

            /// <summary>
            /// PropertyInfo
            /// </summary>
            internal PropertyInfo Property { get; set; }
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
