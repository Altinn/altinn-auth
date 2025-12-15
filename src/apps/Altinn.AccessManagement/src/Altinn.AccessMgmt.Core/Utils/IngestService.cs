using System.Data;
using System.Reflection;
using System.Text;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Altinn.AccessMgmt.PersistenceEF.Utils;

public class IngestService : IIngestService
{
    public AppDbContext DbContext { get; set; }
    
    public IngestService(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<int> IngestData<T>(List<T> data, CancellationToken cancellationToken = default)
    {
        var model = DbContext.Model ?? throw new ArgumentNullException(nameof(T));
        var tableName = GetTableName<T>(model);
        var ingestColumns = GetColumns<T>(model);

        return await WriteToIngest(data, ingestColumns, tableName.TableName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> IngestTempData<T>(List<T> data, Guid ingestId, CancellationToken cancellationToken = default)
    {
        if (ingestId.Equals(Guid.Empty))
        {
            throw new Exception(string.Format("Ingest id '{0}' not valid", ingestId.ToString()));
        }

        var table = GetTableName<T>(DbContext.Model);
        var ingestColumns = GetColumns<T>(DbContext.Model);

        string columnStatement = string.Join(',', ingestColumns.Select(t => t.Name));

        var ingestName = ingestId.ToString().Replace("-", string.Empty);
        string ingestTableName = "ingest." + table.TableName + "_" + ingestName;

        var createIngestTable = $"CREATE UNLOGGED TABLE IF NOT EXISTS {ingestTableName} AS SELECT {columnStatement} FROM {table.SchemaName}.{table.TableName} WITH NO DATA;";

        await DbContext.Database.ExecuteSqlRawAsync(createIngestTable, cancellationToken);

        var completed = await WriteToIngest(data, ingestColumns, ingestTableName, cancellationToken);

        return completed;
    }

    /// <inheritdoc />
    public async Task<int> MergeTempData<T>(Guid ingestId, AuditValues auditValues, IEnumerable<string> matchColumns = null, IEnumerable<string> ignoreColumns = null, CancellationToken cancellationToken = default)
    {
        if (matchColumns == null || matchColumns.Count() == 0)
        {
            matchColumns = ["id"];
        }

        var table = GetTableName<T>(DbContext.Model);
        var ingestColumns = GetColumns<T>(DbContext.Model);

        if (ignoreColumns != null && ignoreColumns.Count() > 0)
        {
            ingestColumns.RemoveAll(t => ignoreColumns.Contains(t.Name));
        }

        string columnStatement = string.Join(',', ingestColumns.Select(t => t.Name));

        var ingestName = ingestId.ToString().Replace("-", string.Empty);
        string ingestTableName = "ingest." + table.TableName + "_" + ingestName;

        var mergeMatchStatement = string.Join(" AND ", matchColumns.Select(t => $"(target.{t} IS NULL AND source.{t} IS NULL OR target.{t} = source.{t})"));
        var mergeUpdateUnMatchStatement = string.Join(
            " OR ",
            ingestColumns
                .Where(t => matchColumns.Count(y => y.Equals(t.Name, StringComparison.OrdinalIgnoreCase)) == 0)
                .Select(t =>
                    $"(" +
                    $"target.{t.Name} <> source.{t.Name} " +
                    $"OR (target.{t.Name} IS NULL AND source.{t.Name} IS NOT NULL) " +
                    $"OR (target.{t.Name} IS NOT NULL AND source.{t.Name} IS NULL)" +
                    $")"
                )
        );

        /*
        Info: '... ingestColumns.Where(t => !t.IsPK && ...' Disables updates on PK. Inserts are not affected.
        Checkout IsPK and IsFK for new features.
        */

        string mergeUpdateStatement = string.Join(", ", ingestColumns.Where(t => !t.IsPK && !matchColumns.Any(y => y.Equals(t.Name, StringComparison.OrdinalIgnoreCase))).Select(t => $"{t.Name} = source.{t.Name}"));
        if (!string.IsNullOrEmpty(mergeUpdateStatement))
        {
            mergeUpdateStatement += ", ";
        }

        mergeUpdateStatement += $"audit_changedby = '{auditValues.ChangedBy}', audit_changedbysystem = '{auditValues.ChangedBySystem}', audit_changeoperation = '{auditValues.OperationId}'";

        var insertColumns = string.Join(", ", ingestColumns.Select(t => $"{t.Name}"));
        var insertValues = string.Join(", ", ingestColumns.Select(t => $"source.{t.Name}"));

        var sb = new StringBuilder();

        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine(GetAuditVariables(auditValues));
        sb.AppendLine($"MERGE INTO {table.SchemaName}.{table.TableName} AS target USING {ingestTableName} AS source ON {mergeMatchStatement}");
        sb.AppendLine($"WHEN MATCHED AND ({mergeUpdateUnMatchStatement}) THEN ");
        sb.AppendLine($"UPDATE SET {mergeUpdateStatement}");
        sb.AppendLine($"WHEN NOT MATCHED THEN ");
        //// sb.AppendLine($"INSERT ({insertColumns}) VALUES ({insertValues});");
        sb.AppendLine($"INSERT ({insertColumns},audit_changedby,audit_changedbysystem,audit_changeoperation) VALUES ({insertValues},'{auditValues.ChangedBy}','{auditValues.ChangedBySystem}','{auditValues.OperationId}');");
        sb.AppendLine("COMMIT TRANSACTION;");

        string mergeStatement = sb.ToString();

        Console.WriteLine("Starting MERGE");

        var res = await ExecuteMigrationCommand(mergeStatement, cancellationToken: cancellationToken);

        Console.WriteLine("Cleanup");
        var dropIngestTable = $"DROP TABLE IF EXISTS {ingestTableName};";
        await ExecuteMigrationCommand(dropIngestTable, cancellationToken: cancellationToken);

        Console.WriteLine($"Merged {res}");

        return res;
    }

    /// <inheritdoc />
    public async Task<int> IngestAndMergeData<T>(List<T> data, AuditValues auditValues, IEnumerable<string> matchColumns = null, CancellationToken cancellationToken = default)
    {
        var ingestId = Guid.CreateVersion7();
        await IngestTempData(data, ingestId, cancellationToken);
        var res = await MergeTempData<T>(ingestId, auditValues, matchColumns, null, cancellationToken);

        return res;
    }

    private async Task<int> WriteToIngest<T>(List<T> data, List<IngestColumnDefinition> ingestColumns, string tableName, CancellationToken cancellationToken = default)
    {
        var conn = (Npgsql.NpgsqlConnection)DbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
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
                    writer.Write(c.Property.GetValue(d), c.DbTypeName);
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
                    }
                }
            }

            completed++;
        }

        writer.Complete();

        Console.WriteLine($"Ingested {completed}");
        return completed;
    }

    private async Task<int> ExecuteMigrationCommand(string query, CancellationToken cancellationToken = default)
    {
        return await DbContext.Database.ExecuteSqlRawAsync(query, cancellationToken);
    }

    private Dictionary<Type, List<IngestColumnDefinition>> TypedIngestColumnDefinitions { get; set; } = new Dictionary<Type, List<IngestColumnDefinition>>();

    private List<IngestColumnDefinition> GetColumns<T>(IModel entityModel)
    {
        var table = GetTableName<T>(entityModel);

        var entityTypes = entityModel.GetEntityTypes();
        if (entityTypes is null || !entityTypes.Any()) 
        { 
            return null; 
        }

        var et = entityTypes.FirstOrDefault(x => x.GetTableName() == table.TableName && x.GetSchema() == BaseConfiguration.BaseSchema);
        var storeObject = StoreObjectIdentifier.Table(table.TableName, BaseConfiguration.BaseSchema);

        if (et is null) 
        { 
            return null; 
        }

        return et.GetProperties()
            .Where(n => (!n.Name.StartsWith("audit_", StringComparison.OrdinalIgnoreCase)) || n.Name.Equals("audit_validfrom", StringComparison.OrdinalIgnoreCase))
            .Select(p => new IngestColumnDefinition() 
            { 
                Name = p.GetColumnName(storeObject), 
                Property = p.PropertyInfo, 
                DbTypeName = p.GetColumnType(),
                IsFK = p.IsForeignKey(),
                IsPK = p.IsPrimaryKey()
            })
            .Distinct()
            .ToList()!;
    }

    private (string TableName, string SchemaName) GetTableName<T>(IModel entityModel)
    {
        return (typeof(T).Name.ToLower(), BaseConfiguration.BaseSchema);
    }
    
    private static string GetAuditVariables(AuditValues auditValues)
    {
        return string.Format("SET LOCAL app.changed_by = '{0}'; SET LOCAL app.changed_by_system = '{1}'; SET LOCAL app.change_operation_id = '{2}';", auditValues.ChangedBy, auditValues.ChangedBySystem, auditValues.OperationId);
    }
}

/// <summary>
/// Ingest data service
/// </summary>
public interface IIngestService
{
    /// <summary>
    /// Ingest data
    /// </summary>
    Task<int> IngestData<T>(List<T> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest data to temp table, using original table as template
    /// </summary>
    Task<int> IngestTempData<T>(List<T> data, Guid ingestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge data from temp table to original
    /// </summary>
    Task<int> MergeTempData<T>(Guid ingestId, AuditValues auditValues, IEnumerable<string> matchColumns = null, IEnumerable<string> ignoreColumns = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest data to temp table, using original table as template
    /// </summary>
    Task<int> IngestAndMergeData<T>(List<T> data, AuditValues auditValues, IEnumerable<string> matchColumns = null, CancellationToken cancellationToken = default);
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
    internal string DbTypeName { get; set; }

    /// <summary>
    /// PropertyInfo
    /// </summary>
    internal PropertyInfo Property { get; set; }

    /// <summary>
    /// Is column part of the primary key
    /// </summary>
    public bool IsPK { get; set; }

    /// <summary>
    /// Is column refrenced to from other tables
    /// </summary>
    public bool IsFK { get; set; }
}
