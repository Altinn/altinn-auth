using System.Data;
using System.Security.Cryptography;
using System.Text;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Persistence.Core.QueryBuilders;

/// <inheritdoc/>
public class PostgresQueryBuilder : IDbQueryBuilder
{
    private readonly IOptions<AccessMgmtPersistenceOptions> _config;
    private readonly DbDefinition _definition;
    private readonly DbDefinitionRegistry _definitionRegistry;

    /// <summary>
    /// QueryBuilder for Postgres
    /// </summary>
    /// <param name="options"><see cref="IOptions{TOptions}"/></param>
    /// <param name="type">Type</param>
    /// <param name="definitionRegistry">DbDefinitionRegistry</param>
    public PostgresQueryBuilder(IOptions<AccessMgmtPersistenceOptions> options, Type type, DbDefinitionRegistry definitionRegistry)
    {
        _config = options;
        _definition = definitionRegistry.TryGetDefinition(type) ?? throw new Exception("Missing definition");
        _definitionRegistry = definitionRegistry;
    }

    #region Query

    /// <inheritdoc/>
    public string BuildBasicSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null)
    {
        var sb = new StringBuilder();

        if (options.AsOf.HasValue)
        {
            sb.AppendLine($"set local app.asof = '{options.AsOf.Value.ToUniversalTime()}';");
        }

        if (_definition.DefinitionType == DbDefinitionType.Query)
        {
            if (string.IsNullOrEmpty(_definition.Query))
            {
                throw new MissingMemberException($"Query not defined for '{_definition.ModelType.Name}'");
            }

            sb.AppendLine(_definition.Query);
        }
        else
        {
            sb.AppendLine("SELECT ");
            sb.AppendLine(GenerateColumns(options));
            sb.AppendLine("FROM " + GenerateSource(options));

            if (crossDef != null)
            {
                sb.AppendLine(GenerateCrossReferenceJoin(crossDef, options));
            }

            sb.AppendLine(GenerateFilterStatement(_definition.ModelType.Name, filters));
        }

        string query = AddPagingToQuery(sb.ToString(), options);

        return query;
    }

    /// <inheritdoc/>
    public string BuildExtendedSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null)
    {
        var sb = new StringBuilder();

        // Apply session settings (e.g., as-of)
        if (options.AsOf.HasValue)
        {
            sb.AppendLine($"set local app.asof = '{options.AsOf.Value.ToUniversalTime()}';");
        }

        if (_definition.DefinitionType == DbDefinitionType.Query)
        {
            if (string.IsNullOrEmpty(_definition.ExtendedQuery))
            {
                throw new MissingMemberException($"Extended query not defined for '{_definition.ModelType.Name}'");
            }

            sb.AppendLine(_definition.ExtendedQuery);
        }
        else
        {
            sb.AppendLine("SELECT ");
            sb.AppendLine(GenerateColumns(options));

            foreach (var relation in _definition.Relations)
            {
                sb.Append(',');
                sb.AppendLine(GenerateJoinPostgresColumns(relation, options));
            }

            sb.AppendLine("FROM " + GenerateSource(options));

            if (crossDef != null)
            {
                sb.AppendLine(GenerateCrossReferenceJoin(crossDef, options));
            }

            foreach (var j in _definition.Relations.Where(t => !t.IsList))
            {
                var joinStatement = GetJoinPostgresStatement(j, options);
                sb.AppendLine(joinStatement);
            }

            sb.AppendLine(GenerateFilterStatement(_definition.ModelType.Name, filters));
        }

        string query = AddPagingToQuery(sb.ToString(), options);

        return query;
    }

    private static string GetAuditTempTable(ChangeRequestOptions options)
    {
        var sb = new StringBuilder();

        // Opprett og fyll session_audit_context
        sb.AppendLine("CREATE TEMP TABLE IF NOT EXISTS session_audit_context (");
        sb.AppendLine("changed_by UUID,");
        sb.AppendLine("changed_by_system UUID,");
        sb.AppendLine("change_operation_id text");
        sb.AppendLine(") ON COMMIT DROP;");

        sb.AppendLine("TRUNCATE session_audit_context;");

        sb.AppendFormat(
            "INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)\n" +
            "VALUES ('{0}', '{1}', '{2}');\n",
            options.ChangedBy,
            options.ChangedBySystem,
            options.ChangeOperationId);

        return sb.ToString();
    }

    private static string GetAuditVariables(ChangeRequestOptions options)
    {
        return string.Format("SET LOCAL app.changed_by = '{0}'; SET LOCAL app.changed_by_system = '{1}'; SET LOCAL app.change_operation_id = '{2}';", options.ChangedBy, options.ChangedBySystem, options.ChangeOperationId);
    }

    /// <inheritdoc/>
    public string BuildInsertQuery(List<GenericParameter> parameters, ChangeRequestOptions options, bool forTranslation = false)
    {
        return $"{GetAuditVariables(options)} INSERT INTO {GetTableName(includeAlias: false, useTranslation: forTranslation)} ({InsertColumns(parameters)}) VALUES({InsertValues(parameters)})";
    }

    /// <inheritdoc/>
    public string BuildUpdateQuery(List<GenericParameter> parameters, ChangeRequestOptions options, bool forTranslation = false)
    {
        return $"{GetAuditVariables(options)} UPDATE {GetTableName(includeAlias: false, useTranslation: forTranslation)} SET {UpdateSetStatement(parameters)} WHERE id = @_id{(forTranslation ? " AND language = @_language" : string.Empty)}";
    }

    /// <inheritdoc/>
    public string BuildSingleNullUpdateQuery(GenericParameter parameter, ChangeRequestOptions options, bool forTranslation = false)
    {
        return $"{GetAuditVariables(options)} UPDATE {GetTableName(includeAlias: false, useTranslation: forTranslation)} SET {parameter.Key} = NULL WHERE id = @_id{(forTranslation ? " AND language = @_language" : string.Empty)}";
    }

    /// <inheritdoc/>
    public string BuildUpsertQuery(List<GenericParameter> insertProperties, IEnumerable<string> updateProperties, IEnumerable<string> compareFilter, ChangeRequestOptions options, bool forTranslation = false)
    {
        if (compareFilter == null || !compareFilter.Any())
        {
            compareFilter = new List<string>()
            {
                "Id"
            };
        }

        return BuildMergeQuery(insertProperties.Select(t => t.Key), updateProperties, compareFilter, options, forTranslation);
    }

    private string BuildMergeQuery(IEnumerable<string> insertProperties, IEnumerable<string> updateProperties, IEnumerable<string> compareFilter, ChangeRequestOptions options, bool forTranslation = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{GetAuditVariables(options)}");
        sb.AppendLine("WITH N AS (");
        sb.AppendLine("SELECT ");
        sb.AppendLine(string.Join(",", compareFilter.Select(t => $"@{t} as {t}")));
        if (forTranslation)
        {
            sb.AppendLine(", @language as language");
        }

        sb.AppendLine(")");
        sb.AppendLine($"MERGE INTO {GetTableName(includeAlias: false, useTranslation: forTranslation)} AS T USING N ON {string.Join(" AND ", compareFilter.Select(t => $"t.{t} = n.{t}"))}");
        if (forTranslation)
        {
            sb.AppendLine(" AND T.language = N.language");
        }

        sb.AppendLine($"WHEN MATCHED AND ({string.Join(" OR ", updateProperties.Select(t => $"T.{t} <> @{t}"))}) THEN");
        sb.AppendLine($"UPDATE SET {string.Join(",", updateProperties.Select(t => $"{t} = @{t}"))}");
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine($"INSERT ({InsertColumns(insertProperties)}) VALUES({InsertValues(insertProperties)});");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string BuildDeleteQuery(IEnumerable<GenericFilter> filters, ChangeRequestOptions options)
    {
        var filterStatement = GenerateFilterStatement(_definition.ModelType.Name, filters);
        return $"BEGIN; {GetAuditTempTable(options)} DELETE FROM {GetTableName(includeAlias: false)} {filterStatement}; COMMIT;";
    }

    /// <inheritdoc />
    public string GetTableName(bool includeAlias = true, bool useHistory = false, bool useTranslation = false, bool useHistoryView = false, bool includeSchema = true)
    {
        return GetTableName(_definition, includeAlias, useHistory, useTranslation, useHistoryView, includeSchema);
    }

    private string GetTableName(DbDefinition dbDefinition, bool includeAlias = true, bool useHistory = false, bool useTranslation = false, bool useHistoryView = false, bool includeSchema = true)
    {
        var config = _config.Value;

        var schema = includeSchema
            ? (useHistory
                ? (useTranslation ? config.TranslationHistorySchema : config.BaseHistorySchema)
                : (useTranslation ? config.TranslationSchema : config.BaseSchema))
            : string.Empty;

        var prefix = (useHistory && !useHistoryView) ? "_" : string.Empty;
        var tableName = string.IsNullOrEmpty(schema)
            ? $"{prefix}{dbDefinition.ModelType.Name}"
            : $"{schema}.{prefix}{dbDefinition.ModelType.Name}";

        return includeAlias
            ? $"{tableName} AS {dbDefinition.ModelType.Name}"
            : tableName;
    }

    private string GetSchemaName(bool useHistory = false, bool useTranslation = false)
    {
        var config = _config.Value;
        return useHistory ? (useTranslation ? config.TranslationHistorySchema : config.BaseHistorySchema) : (useTranslation ? config.TranslationSchema : config.BaseSchema);
    }

    private string GetTableAlias()
    {
        return GetTableAlias(_definition);
    }

    private string GetTableAlias(DbDefinition dbDefinition)
    {
        return dbDefinition.ModelType.Name;
    }

    /*Basic*/
    private string GenerateColumns(RequestOptions options)
    {
        bool useTranslation = !string.IsNullOrEmpty(options.Language) && _definition.EnableTranslation;
        var columns = new List<string>();

        foreach (var p in _definition.Properties)
        {
            if (_definition.ExtendedProperties.Count(t => t.Name == p.Name) > 0)
            {
                var ep = _definition.ExtendedProperties.First(t => t.Name == p.Name);
                columns.Add($"{ep.Function}({_definition.ModelType.Name}.{p.Name}) AS {ep.ExtendedProperty.Name}");
            }
            else
            {
                var property = p.Property;
                if (useTranslation && property.PropertyType == typeof(string))
                {
                    columns.Add($"coalesce(T_{_definition.ModelType.Name}.{property.Name}, {_definition.ModelType.Name}.{property.Name}) AS {property.Name}");
                }
                else
                {
                    columns.Add($"{_definition.ModelType.Name}.{property.Name} AS {property.Name}");
                }
            }
        }

        // Add paging row number if needed
        if (options.UsePaging)
        {
            string orderBy = !string.IsNullOrEmpty(options.OrderBy) && _definition.Properties.Exists(t => t.Name.Equals(options.OrderBy, StringComparison.CurrentCultureIgnoreCase)) ? options.OrderBy : "Id";
            columns.Add($"ROW_NUMBER() OVER (ORDER BY {_definition.ModelType.Name}.{orderBy}) AS _rownum");
        }

        return string.Join(',', columns);
    }

    private string GenerateSource(RequestOptions options)
    {
        bool useTranslation = !string.IsNullOrEmpty(options.Language) && _definition.EnableTranslation;
        bool useHistory = options.AsOf.HasValue;

        if (useTranslation)
        {
            // Example for translation JOIN
            return $"""
                {GetTableName(includeAlias: true, useHistory: useHistory)}
                LEFT JOIN LATERAL (SELECT * FROM {GetTableName(includeAlias: false, useTranslation: true, useHistory: useHistory)} AS T 
                WHERE T.Id = {_definition.ModelType.Name}.Id AND T.Language = @Language ) AS T_{_definition.ModelType.Name} ON 1=1
                """;
        }
        else
        {
            return GetTableName(includeAlias: true, useHistory: useHistory);
        }
    }

    /// <summary>
    /// Generates an INNER JOIN SQL filterStatement for a cross-reference relationship.
    /// </summary>
    /// <param name="crossRef">The cross-reference definition.</param>
    /// <param name="options">Request options that may affect SQL generation.</param>
    /// <returns>A formatted SQL INNER JOIN filterStatement.</returns>
    private string GenerateCrossReferenceJoin(DbCrossRelationDefinition crossRef, RequestOptions options)
    {
        bool useHistory = options.AsOf.HasValue;

        var crossRefDef = _definitionRegistry.TryGetDefinition(crossRef.CrossType) ?? throw new InvalidOperationException();

        string mainTable = GetTableAlias();
        string crossTable = GetTableName(crossRefDef, includeAlias: false, useHistory: useHistory);
        string mainIdentityProperty = crossRef.GetReferenceProperty(_definition.ModelType);
        string joinColumn = crossRef.GetIdentityProperty(_definition.ModelType);
        string filterColumn = crossRef.GetReverseIdentityProperty(_definition.ModelType);

        return $@"
        INNER JOIN {crossTable} AS X 
        ON {mainTable}.{mainIdentityProperty} = X.{joinColumn}
        AND X.{filterColumn} = @X_Id";
    }

    private string GenerateFilterStatement(string tableAlias, IEnumerable<GenericFilter> filters)
    {
        if (filters == null || !filters.Any())
        {
            return string.Empty;
        }

        var conditions = new List<string>();

        var multiple = filters.CountBy(t => t.PropertyName).Where(t => t.Value > 1).Select(t => t.Key);

        foreach (var filter in filters.Where(t => !multiple.Contains(t.PropertyName)))
        {
            string condition = filter.Comparer switch
            {
                FilterComparer.Equals => $"{tableAlias}.{filter.PropertyName} = @{filter.PropertyName}",
                FilterComparer.NotEqual => $"{tableAlias}.{filter.PropertyName} <> @{filter.PropertyName}",
                FilterComparer.GreaterThan => $"{tableAlias}.{filter.PropertyName} > @{filter.PropertyName}",
                FilterComparer.GreaterThanOrEqual => $"{tableAlias}.{filter.PropertyName} >= @{filter.PropertyName}",
                FilterComparer.LessThan => $"{tableAlias}.{filter.PropertyName} < @{filter.PropertyName}",
                FilterComparer.LessThanOrEqual => $"{tableAlias}.{filter.PropertyName} <= @{filter.PropertyName}",
                FilterComparer.StartsWith => $"{tableAlias}.{filter.PropertyName} ILIKE @{filter.PropertyName}",
                FilterComparer.EndsWith => $"{tableAlias}.{filter.PropertyName} ILIKE @{filter.PropertyName}",
                FilterComparer.Contains => $"{tableAlias}.{filter.PropertyName} ILIKE @{filter.PropertyName}",
                FilterComparer.Like => $"{tableAlias}.{filter.PropertyName} ILIKE @{filter.PropertyName}",
                _ => throw new NotSupportedException($"Comparer '{filter.Comparer}' is not supported.")
            };

            conditions.Add(condition);
        }

        foreach (var m in multiple)
        {
            var inList = new List<string>();
            var notInList = new List<string>();

            int a = 1;
            foreach (var filter in filters.Where(t => t.PropertyName == m))
            {
                if (filter.Comparer == FilterComparer.Equals)
                {
                    inList.Add($"@{m}_{a}");
                }
                else if (filter.Comparer == FilterComparer.NotEqual)
                {
                    notInList.Add($"@{m}_{a}");
                }
                else
                {
                    throw new Exception("Filter not supported");
                }

                a++;
            }

            if (inList.Any())
            {
                conditions.Add($"{tableAlias}.{m} IN ({string.Join(",", inList)})");
            }

            if (notInList.Any())
            {
                conditions.Add($"{tableAlias}.{m} NOT IN ({string.Join(",", notInList)})");
            }
        }

        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }

    private string AddPagingToQuery(string query, RequestOptions options)
    {
        if (!options.UsePaging)
        {
            return query;
        }

        var sb = new StringBuilder();
        sb.AppendLine("WITH pagedresult AS (");
        sb.AppendLine(query);
        sb.AppendLine(")");
        sb.AppendLine("SELECT *");
        sb.AppendLine($"FROM pagedresult, (SELECT MAX(pagedresult._rownum) AS _totalItemCount, {options.PageSize} as _pageSize, {options.PageNumber} as _pageNumber FROM pagedresult) AS _totalItemCount");
        sb.AppendLine($"ORDER BY _rownum OFFSET {options.PageSize * (options.PageNumber - 1)} ROWS FETCH NEXT {options.PageSize} ROWS ONLY");

        return sb.ToString();
    }

    /*Extended*/
    private string GetJoinPostgresStatement(DbRelationDefinition join, RequestOptions options)
    {
        var joinDef = _definitionRegistry.TryGetDefinition(join.Ref) ?? throw new InvalidOperationException();
        bool useHistory = options.AsOf.HasValue;

        var sb = new StringBuilder();

        sb.Append($"{(join.IsOptional ? "LEFT OUTER" : "INNER")} JOIN {GetTableName(joinDef, includeAlias: false, useHistory: useHistory)} AS _{join.ExtendedProperty} ON {join.Base.Name}.{join.BaseProperty} = _{join.ExtendedProperty}.{join.RefProperty} {GetJoinPostgresFilterString(join)}");

        bool useTranslation = !string.IsNullOrEmpty(options.Language);
        if (useTranslation && joinDef.EnableTranslation)
        {
            sb.AppendLine();
            sb.Append($"LEFT JOIN LATERAL (SELECT * FROM {GetTableName(joinDef, useTranslation: true, includeAlias: false, useHistory: useHistory)} AS T ");
            sb.Append($"WHERE T.Id = _{join.ExtendedProperty}.Id AND t.Language = @Language) AS T_{join.ExtendedProperty} ON 1=1");
        }

        return sb.ToString();
    }

    private string GenerateJoinPostgresColumns(DbRelationDefinition join, RequestOptions options)
    {
        var joinDef = _definitionRegistry.TryGetDefinition(join.Ref);
        if (joinDef == null)
        {
            return string.Empty;
        }

        if (!join.IsList)
        {
            bool useTranslation = !string.IsNullOrEmpty(options.Language);
            var columns = new List<string>();
            foreach (var p in joinDef.Properties.Select(t => t.Property))
            {
                if (joinDef.EnableTranslation && useTranslation && p.PropertyType == typeof(string))
                {
                    columns.Add($"coalesce(T_{join.ExtendedProperty}.{p.Name}, _{join.ExtendedProperty}.{p.Name}) AS {join.ExtendedProperty}_{p.Name}");
                }
                else
                {
                    columns.Add($"_{join.ExtendedProperty}.{p.Name} AS {join.ExtendedProperty}_{p.Name}");
                }
            }

            return string.Join(',', columns);
        }
        else
        {
            return $"COALESCE((SELECT JSON_AGG(ROW_TO_JSON({join.ExtendedProperty})) FROM {GetTableName(joinDef, includeAlias: false)} AS {join.ExtendedProperty} WHERE {join.ExtendedProperty}.{join.RefProperty} = {join.Base.Name}.{join.BaseProperty}), '[]') AS {join.ExtendedProperty}";
        }
    }

    private string GetJoinPostgresFilterString(DbRelationDefinition join)
    {
        if (join.Filters == null || join.Filters.Count == 0)
        {
            return string.Empty;
        }

        string result = string.Empty;

        foreach (var filter in join.Filters)
        {
            result += $" AND {join.Base.Name}.{filter.PropertyName} = _{join.ExtendedProperty}.{filter.Value}";
        }

        return result;
    }

    /*Write*/
    private string UpdateSetStatement(IEnumerable<GenericParameter> parameters)
    {
        return UpdateSetStatement(parameters.Select(t => t.Key).ToList());
    }

    private string UpdateSetStatement(List<string> values)
    {
        return string.Join(',', values.OrderBy(t => t).Select(t => $"{t} = @{t}").ToList());
    }

    private string InsertColumns(List<GenericParameter> values)
    {
        return InsertColumns(values.Select(t => t.Key));
    }

    private string InsertColumns(IEnumerable<string> values)
    {
        return string.Join(',', values.OrderBy(t => t).Select(t => $"{t}").ToList());
    }

    private string InsertValues(List<GenericParameter> values)
    {
        return InsertValues(values.Select(t => t.Key));
    }

    private string InsertValues(IEnumerable<string> values)
    {
        return string.Join(',', values.OrderBy(t => t).Select(t => $"@{t}").ToList());
    }

    private string MergeUpdateMatchStatement(List<GenericParameter> values)
    {
        return MergeUpdateMatchStatement(values.Select(t => t.Key));
    }

    private string MergeUpdateMatchStatement(IEnumerable<string> values)
    {
        return string.Join(" OR ", values.OrderBy(t => t).Select(t => $"T.{t} <> @{t}").ToList());
    }
    #endregion

    #region Schema

    /// <inheritdoc/>
    public DbMigrationScriptCollection GetMigrationScripts()
    {
        var scriptCollection = new DbMigrationScriptCollection(_definition.ModelType);
        scriptCollection.Version = _definition.Version;

        if (_definition.ManualPreMigrationScripts.Any())
        {
            foreach (var dep in _definition.ManualPreMigrationScripts)
            {
                scriptCollection.AddScripts($"PreMigrationScript({dep.Key})", dep.Value);
            }
        }

        //// Create view
        if (_definition.DefinitionType == DbDefinitionType.View)
        {
            scriptCollection.AddScripts(CreateView());
            foreach (var dep in _definition.ManualDependencies)
            {
                scriptCollection.AddDependency(dep);
            }

            return scriptCollection;
        }

        if (_definition.DefinitionType == DbDefinitionType.Query)
        {
            // TODO: PREPARE STATEMENT
            return scriptCollection;
        }

        //// Create tables
        scriptCollection.AddScripts(CreateTables());

        //// Create columns
        var pk = _definition.Constraints.FirstOrDefault(t => t.IsPrimaryKey);
        foreach (var column in _definition.Properties)
        {
            if (pk != null && pk.Properties.ContainsKey(column.Name))
            {
                continue;
            }

            scriptCollection.AddScripts(CreateColumn(column));
        }

        //// Create Foreign keys
        foreach (var fk in _definition.Relations)
        {
            scriptCollection.AddScripts(CreateForeignKeyConstraint(fk));
            scriptCollection.AddDependency(fk.Ref);
        }

        //// Create unique constraints
        foreach (var uc in _definition.Constraints)
        {
            if (uc.IsPrimaryKey)
            {
                continue;
            }

            scriptCollection.AddScripts(CreateUniqueConstraint(uc));
        }

        //// Create history views
        if (_definition.EnableAudit)
        {
            scriptCollection.AddScripts($"VIEW {GetTableName(includeAlias: false, useHistory: true, useHistoryView: true, useTranslation: false)}", CreateHistoryView(isTranslation: false));

            if (_definition.EnableTranslation)
            {
                scriptCollection.AddScripts($"VIEW {GetTableName(includeAlias: false, useHistory: true, useHistoryView: true, useTranslation: true)}", CreateHistoryView(isTranslation: true));
            }
        }

        //// Common meta function
        scriptCollection.AddScripts(CreateAuditMetadataFunction()); // Move to Pre?

        //// Create functions and triggers
        scriptCollection.AddScripts(CreateMetaDataTrigger(isTranslation: false));
        scriptCollection.AddScripts(CreateAuditUpdateFunction(isTranslation: false));
        scriptCollection.AddScripts(CreateAuditDeleteFunction(isTranslation: false));
        scriptCollection.AddScripts(CreateAuditUpdateTrigger(isTranslation: false));
        scriptCollection.AddScripts(CreateAuditDeleteTrigger(isTranslation: false));

        if (_definition.EnableTranslation)
        {
            scriptCollection.AddScripts(CreateMetaDataTrigger(isTranslation: true));
            scriptCollection.AddScripts(CreateAuditUpdateFunction(isTranslation: true));
            scriptCollection.AddScripts(CreateAuditDeleteFunction(isTranslation: true));
            scriptCollection.AddScripts(CreateAuditUpdateTrigger(isTranslation: true));
            scriptCollection.AddScripts(CreateAuditDeleteTrigger(isTranslation: true));
        }

        if (_definition.ManualPostMigrationScripts.Any())
        {
            foreach (var dep in _definition.ManualPostMigrationScripts)
            {
                scriptCollection.AddScripts($"PostMigrationScript({dep.Key})", dep.Value);
            }
        }

        return scriptCollection;
    }

    private OrderedDictionary<string, string> CreateView()
    {
        var scripts = new OrderedDictionary<string, string>();

        var query = $"""
        DROP VIEW IF EXISTS {GetTableName(includeAlias: false)};
        CREATE VIEW {GetTableName(includeAlias: false)} AS
        {_definition.Query}
        """;

        scripts.Add($"CREATE VIEW {GetTableName(includeAlias: false)} v{_definition.Version}", query);

        return scripts;
    }

    private OrderedDictionary<string, string> CreateTables()
    {
        var scripts = new OrderedDictionary<string, string>();

        var pk = _definition.Constraints.FirstOrDefault(t => t.IsPrimaryKey);
        if (pk == null)
        {
            throw new Exception($"PK: {_definition.ModelType.Name} does not contain a primary key");
        }

        var properties = _definition.ModelType.GetProperties().ToList();
        foreach (var property in pk.Properties)
        {
            if (!properties.Exists(t => t.Name == property.Key))
            {
                throw new Exception($"PK: {_definition.ModelType.Name} does not contain the property '{property.Key}'");
            }
        }

        string primaryKeyDefinition = string.Join(',', pk.Properties.Select(t => $"{t.Key} {DbHelperMethods.GetPostgresType(t.Value)} NOT NULL "));

        var dboName = GetTableName(includeAlias: false, useTranslation: false, useHistory: false);
        var translationName = GetTableName(includeAlias: false, useTranslation: true, useHistory: false);
        var dboHistoryName = GetTableName(includeAlias: false, useTranslation: false, useHistory: true);
        var translationHistoryName = GetTableName(includeAlias: false, useTranslation: true, useHistory: true);

        var dboScript = CreateTableScript(dboName, primaryKeyDefinition, _definition.EnableAudit, isHistory: false, isTranslation: false);
        var translationScript = CreateTableScript(translationName, primaryKeyDefinition, _definition.EnableAudit, isHistory: false, isTranslation: true);
        var basicHistoryScript = CreateTableScript(dboHistoryName, primaryKeyDefinition, _definition.EnableAudit, isHistory: true, isTranslation: false);
        var translationHistoryScript = CreateTableScript(translationHistoryName, primaryKeyDefinition, _definition.EnableAudit, isHistory: true, isTranslation: true);

        scripts.Add($"CREATE TABLE {dboName}", dboScript);

        if (_definition.EnableTranslation)
        {
            string fkName = $"FK_Translation_{_definition.ModelType.Name}_id";
            scripts.Add($"CREATE TABLE {translationName}", translationScript);
            var translationForeignKey = $"ALTER TABLE {GetSchemaName(useTranslation: true)}.{GetTableName(includeAlias: false, includeSchema: false)} DROP CONSTRAINT IF EXISTS {fkName}; ALTER TABLE {GetSchemaName(useTranslation: true)}.{GetTableName(includeAlias: false, includeSchema: false)} ADD CONSTRAINT {fkName} FOREIGN KEY (id) REFERENCES {GetSchemaName()}.{GetTableName(includeAlias: false, includeSchema: false)} (id) ON DELETE CASCADE;";
            scripts.Add($"ADD CONSTRAINT FK_Translation_{_definition.ModelType.Name}_id", translationForeignKey);
        }

        if (_definition.EnableAudit)
        {
            scripts.Add($"CREATE TABLE {dboHistoryName}", basicHistoryScript);
            if (_definition.EnableTranslation)
            {
                scripts.Add($"CREATE TABLE {translationHistoryName}", translationHistoryScript);
            }
        }

        return scripts;
    }

    private string CreateTableScript(string name, string primaryKeyDefinition, bool useAudit, bool isHistory, bool isTranslation)
    {
        var script = new StringBuilder();
        script.AppendLine($"CREATE TABLE IF NOT EXISTS {name}(");
        script.AppendLine(primaryKeyDefinition);

        if (isTranslation)
        {
            script.AppendLine(", language text not null");
        }

        if (useAudit)
        {
            if (!isHistory)
            {
                script.AppendLine(", audit_validfrom timestamptz not null default now()");
            }
            else
            {
                script.AppendLine(", audit_validfrom timestamptz not null");
                script.AppendLine(", audit_validto timestamptz not null");
            }

            script.AppendLine(", audit_changedby uuid not null");
            script.AppendLine(", audit_changedbysystem uuid not null");
            script.AppendLine(", audit_changeoperation text not null");

            if (isHistory)
            {
                script.AppendLine(", audit_deletedby uuid null"); // History only
                script.AppendLine(", audit_deletedbysystem uuid null"); // History only
                script.AppendLine(", audit_deleteoperation text null"); // History only
            }
        }

        if (!isHistory)
        {
            if (isTranslation)
            {
                string fkName = $"FK_{_definition.ModelType.Name}_id";
                script.AppendLine($", CONSTRAINT PK_{_definition.ModelType.Name} PRIMARY KEY ({string.Join(',', _definition.Constraints.First(t => t.IsPrimaryKey).Properties.Select(t => $"{t.Key}"))}, language)");
                var query = $"ALTER TABLE {GetSchemaName(useTranslation: true)}.{GetTableName(includeAlias: false, includeSchema: false)} DROP CONSTRAINT IF EXISTS {fkName}; ALTER TABLE {GetSchemaName(useTranslation: true)}.{GetTableName(includeAlias: false, includeSchema: false)} ADD CONSTRAINT {fkName} FOREIGN KEY (id) REFERENCES {GetSchemaName()}.{GetTableName(includeAlias: false, includeSchema: false)} (id) ON DELETE CASCADE;";
            }
            else
            {
                script.AppendLine($", CONSTRAINT PK_{_definition.ModelType.Name} PRIMARY KEY ({string.Join(',', _definition.Constraints.First(t => t.IsPrimaryKey).Properties.Select(t => $"{t.Key}"))})");
            }
        }

        script.AppendLine(");");

        return script.ToString();
    }

    private OrderedDictionary<string, string> CreateColumn(DbPropertyDefinition column)
    {
        var scripts = new OrderedDictionary<string, string>();

        var basic = CreateColumn(GetTableName(includeAlias: false), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), column.IsNullable, column.DefaultValue);
        scripts.Add(basic.Key, basic.Query);

        if (_definition.EnableTranslation)
        {
            var translation = CreateColumn(GetTableName(includeAlias: false, useTranslation: true), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), true, column.DefaultValue);
            scripts.Add(translation.Key, translation.Query);
        }

        if (_definition.EnableAudit)
        {
            var history = CreateColumn(GetTableName(includeAlias: false, useHistory: true), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), true, column.DefaultValue);
            scripts.Add(history.Key, history.Query);

            if (_definition.EnableTranslation)
            {
                var translationHistory = CreateColumn(GetTableName(includeAlias: false, useTranslation: true, useHistory: true), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), true, column.DefaultValue);
                scripts.Add(translationHistory.Key, translationHistory.Query);
            }
        }

        return scripts;
    }

    private (string Key, string Query) CreateColumn(string tableName, string columnName, string columnDataType, bool isNullable, string defaultValue = null)
    {
        var key = $"ADD COLUMN {tableName}.{columnName}";
        var query = $"ALTER TABLE {tableName} ADD IF NOT EXISTS {columnName} {columnDataType} {(isNullable ? "NULL" : "NOT NULL")}{(string.IsNullOrEmpty(defaultValue) ? string.Empty : $" DEFAULT {defaultValue}")};";

        return (key, query);
    }

    private OrderedDictionary<string, string> CreateUniqueConstraint(DbConstraintDefinition constraint)
    {
        var res = new OrderedDictionary<string, string>();

        string name = string.IsNullOrEmpty(constraint.Name) ? _definition.ModelType.Name : constraint.Name;

        var allPropsOnModel = _definition.ModelType.GetProperties().Select(p => p.Name).ToHashSet();
        foreach (var prop in constraint.Properties.Keys)
        {
            if (!allPropsOnModel.Contains(prop))
            {
                throw new Exception($"Property {prop} not found on {_definition.ModelType.Name}");
            }
        }

        foreach (var prop in constraint.NullableProperties.Keys)
        {
            if (!allPropsOnModel.Contains(prop))
            {
                throw new Exception($"Property {prop} not found on {_definition.ModelType.Name}");
            }
        }

        var nullableProps = constraint.NullableProperties.Keys.ToList();
        var allProps = constraint.Properties.Keys.ToList();
        var nonNullableProps = allProps.Where(p => !nullableProps.Contains(p)).ToList();

        string tableName = GetTableName(includeAlias: false);

        string dropConstraintKey = $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {name}";
        string dropConstraintQuery = $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {name};";
        res.Add(dropConstraintKey, dropConstraintQuery);

        if (!constraint.NullableProperties.Any())
        {
            string colsCsv = string.Join(", ", allProps);

            string addConstraintKey = $"ALTER TABLE {tableName} ADD CONSTRAINT {name}";
            string addConstraintQuery = $"ALTER TABLE {tableName} ADD CONSTRAINT {name} UNIQUE ({colsCsv});";
            res.Add(addConstraintKey, addConstraintQuery);

            string idxName = $"uc_{_definition.ModelType.Name}_{string.Join("_", allProps)}_idx".ToLowerInvariant();
            if (idxName.Length > 63)
            {
                idxName = GenerateUniqueConstraintName(_definition.ModelType.Name, allProps, 0);
            }

            string idxKey = $"CREATE INDEX {idxName}";
            string idxQuery =
                $"CREATE UNIQUE INDEX IF NOT EXISTS {idxName} " +
                $"ON {tableName} ({colsCsv}) " +
                $"{(constraint.IncludedProperties.Any()
                    ? $"INCLUDE ({string.Join(", ", constraint.IncludedProperties.Keys)}) "
                    : string.Empty)};";
            res.Add(idxKey, idxQuery);

            return res;
        }

        int nullablePropertyCount = nullableProps.Count;
        int totalCombinations = 1 << nullablePropertyCount;

        for (int mask = 0; mask < totalCombinations; mask++)
        {
            var whereConditions = new List<string>();
            var indexColumns = new List<string>(nonNullableProps);
            for (int bit = 0; bit < nullablePropertyCount; bit++)
            {
                string nullableCol = nullableProps[bit];
                bool isNotNull = ((mask >> bit) & 1) == 1;
                if (isNotNull)
                {
                    indexColumns.Add(nullableCol);
                    whereConditions.Add($"{nullableCol} IS NOT NULL");
                }
                else
                {
                    whereConditions.Add($"{nullableCol} IS NULL");
                }
            }

            string idxName = GenerateUniqueConstraintName(_definition.ModelType.Name, allProps, mask);

            string columnsList = string.Join(", ", indexColumns);
            string whereClause = string.Join(" AND ", whereConditions);
            string idxKey = $"CREATE INDEX {idxName}";
            string idxQuery =
                $"CREATE UNIQUE INDEX IF NOT EXISTS {idxName}" +
                $" ON {tableName} ({columnsList})" +
                $"{(constraint.IncludedProperties.Any() 
                    ? $" INCLUDE ({string.Join(',', constraint.IncludedProperties.Keys)})" 
                    : string.Empty)}" +
                $" WHERE {whereClause};";

            res.Add(idxKey, idxQuery);
        }

        return res;
    }

    private string GenerateUniqueConstraintName(string modelName, IList<string> indexColumns, int mask)
    {
        const int MaxPgNameLength = 63;

        string columnsPart = string.Join("_", indexColumns).ToLowerInvariant();
        string baseName = $"uc_{modelName}_{columnsPart}_m{mask}".ToLowerInvariant();

        if (baseName.Length <= MaxPgNameLength)
        {
            return baseName;
        }

        byte[] hashBytes;
        using (var sha1 = SHA1.Create())
        {
            hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(baseName));
        }

        string fullHashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        string shortHash = fullHashHex.Substring(0, 8);

        int maskLength = mask.ToString().Length;
        int reservedLen = /* "uc_" */ 3
                         + modelName.Length
                         + /* "_" between modelName and columns */ 1
                         + /* "_m" + mask */ 2 + maskLength
                         + /* "_" before hash */ 1
                         + shortHash.Length;

        int availForColumns = MaxPgNameLength - reservedLen;
        if (availForColumns < 1)
        {
            int reservedMinimal = 3 + modelName.Length + 1 + shortHash.Length;
            if (reservedMinimal <= MaxPgNameLength)
            {
                return $"uc_{modelName}_{shortHash}";
            }
            else
            {
                int availForModel = MaxPgNameLength - (3 /*"uc_"*/ + 1 /*"_"*/ + shortHash.Length);
                string truncatedModel = modelName.Substring(0, Math.Max(0, availForModel));
                return $"uc_{truncatedModel}_{shortHash}";
            }
        }

        string truncatedColumns;
        if (columnsPart.Length <= availForColumns)
        {
            truncatedColumns = columnsPart;
        }
        else
        {
            truncatedColumns = columnsPart.Substring(0, availForColumns);
        }

        return $"uc_{modelName}_{truncatedColumns}_m{mask}_{shortHash}";
    }

    private OrderedDictionary<string, string> CreateForeignKeyConstraint(DbRelationDefinition foreignKey)
    {
        var res = new OrderedDictionary<string, string>();

        if (!_definition.ModelType.GetProperties().ToList().Exists(t => t.Name.Equals(foreignKey.BaseProperty, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"{_definition.ModelType.Name} does not contain the property '{foreignKey.BaseProperty}'");
        }

        if (!foreignKey.Ref.GetProperties().ToList().Exists(t => t.Name.Equals(foreignKey.RefProperty, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"{foreignKey.Ref.Name} does not contain the property '{foreignKey.RefProperty}'");
        }

        var targetDef = _definitionRegistry.TryGetDefinition(foreignKey.Ref);
        if (targetDef == null)
        {
            throw new Exception("Type not found");
        }

        string name = string.IsNullOrEmpty(foreignKey.Name) ? $"{_definition.ModelType.Name}_{foreignKey.BaseProperty}" : foreignKey.Name;

        var key = $"ADD CONSTRAINT {GetTableName(includeAlias: false)}.{name}";
        var query = $"ALTER TABLE {GetTableName(includeAlias: false)} DROP CONSTRAINT IF EXISTS {name}; ALTER TABLE {GetTableName(includeAlias: false)} ADD CONSTRAINT {name} FOREIGN KEY ({foreignKey.BaseProperty}) REFERENCES {GetTableName(targetDef, includeAlias: false)} ({foreignKey.RefProperty}) {(foreignKey.UseCascadeDelete ? "ON DELETE CASCADE" : "ON DELETE SET NULL")};";

        res.Add(key, query);

        var idxName = $"fk_{_definition.ModelType.Name}_{foreignKey.BaseProperty}_{targetDef.ModelType.Name}_idx".ToLower();
        var idxKey = $"CREATE INDEX {idxName}";
        var idxQuery = $"CREATE INDEX IF NOT EXISTS {idxName} ON {GetTableName(includeAlias: false)} ({foreignKey.BaseProperty});";

        res.Add(idxKey, idxQuery);

        return res;
    }

    private string CreateHistoryView(bool isTranslation)
    {
        string tableName = GetTableName(includeAlias: true, useHistory: false, useTranslation: isTranslation);
        string columnDefinitions = string.Join(',', _definition.Properties.Select(t => t.Name));

        if (isTranslation)
        {
            columnDefinitions += ", language";
        }

        string historyColumns = $"{columnDefinitions}, audit_validfrom, audit_validto, audit_changedby, audit_changedbysystem, audit_changeoperation, audit_deletedby, audit_deletedbysystem, audit_deleteoperation";
        string baseColumns = $"{columnDefinitions}, audit_validfrom, now() as audit_validto, audit_changedby, audit_changedbysystem, audit_changeoperation, null::uuid AS audit_deletedby, null::uuid AS audit_deletedbysystem, null::text AS audit_deleteoperation";

        string viewQuery = $"""
        DROP VIEW IF EXISTS {GetTableName(includeAlias: false, useHistory: true, useHistoryView: true, useTranslation: isTranslation)};
        CREATE VIEW {GetTableName(includeAlias: false, useHistory: true, useHistoryView: true, useTranslation: isTranslation)} AS
        SELECT {historyColumns}
        FROM  {GetTableName(useHistory: true, useTranslation: isTranslation)}
        WHERE audit_validfrom <= coalesce(current_setting('app.asof', true)::timestamptz, now())
        AND audit_validto > coalesce(current_setting('app.asof', true)::timestamptz, now())
        UNION ALL
        SELECT {baseColumns}
        FROM {tableName}
        WHERE audit_validfrom <= coalesce(current_setting('app.asof', true)::timestamptz, now());
        """;

        return viewQuery;
    }

    private OrderedDictionary<string, string> CreateAuditMetadataFunction()
    {
        var scripts = new OrderedDictionary<string, string>();

        var sb = new StringBuilder();
        sb.AppendLine("CREATE OR REPLACE FUNCTION dbo.set_audit_metadata_fn()");
        sb.AppendLine("RETURNS TRIGGER AS $$");
        sb.AppendLine("DECLARE");
        sb.AppendLine("changed_by UUID;");
        sb.AppendLine("changed_by_system UUID;");
        sb.AppendLine("change_operation_id text;");
        sb.AppendLine("BEGIN");
        sb.AppendLine("SELECT current_setting('app.changed_by', false) INTO changed_by;");
        sb.AppendLine("SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;");
        sb.AppendLine("SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;");
        sb.AppendLine("NEW.audit_changedby := changed_by;");
        sb.AppendLine("NEW.audit_changedbysystem := changed_by_system;");
        sb.AppendLine("NEW.audit_changeoperation := change_operation_id;");
        sb.AppendLine("NEW.audit_validfrom := now();");
        sb.AppendLine("RETURN NEW;");
        sb.AppendLine("END;");
        sb.AppendLine("$$ LANGUAGE plpgsql;");

        scripts.Add($"CREATE FUNCTION dbo.set_audit_metadata_fn", sb.ToString());

        return scripts;
    }

    private OrderedDictionary<string, string> CreateMetaDataTrigger(bool isTranslation)
    {
        var scripts = new OrderedDictionary<string, string>();

        string modelName = _definition.ModelType.Name;
        string schema = GetSchemaName(useHistory: false, useTranslation: isTranslation);
        string functionName = $"set_audit_metadata_fn";
        string triggerName = $"{modelName}_Meta";
        string tableName = GetTableName(includeAlias: false, useTranslation: isTranslation);

        var sb = new StringBuilder();
        sb.AppendLine($"DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE '{triggerName}' AND t.tgrelid = '{tableName}'::regclass) THEN");
        sb.AppendLine($"CREATE OR REPLACE TRIGGER {triggerName} BEFORE INSERT OR UPDATE ON {tableName}");
        sb.AppendLine($"FOR EACH ROW EXECUTE FUNCTION dbo.{functionName}();");
        sb.AppendLine($"END IF; END $$;");

        scripts.Add($"CREATE TRIGGER {schema}.{triggerName}", sb.ToString());

        return scripts;
    }

    private OrderedDictionary<string, string> CreateAuditUpdateFunction(bool isTranslation)
    {
        var scripts = new OrderedDictionary<string, string>();

        string tableName = GetTableName(includeAlias: false, useTranslation: isTranslation);
        string historyTableName = GetTableName(includeAlias: false, useHistory: true, useTranslation: isTranslation);
        string modelName = _definition.ModelType.Name;
        string schema = GetSchemaName(useHistory: false, useTranslation: isTranslation);

        string columns = string.Join(',', _definition.Properties.Select(t => t.Name));
        string oldColumns = string.Join(',', _definition.Properties.Select(t => $"OLD.{t.Name}"));
        if (isTranslation)
        {
            columns += ", language";
            oldColumns += ", OLD.language";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE OR REPLACE FUNCTION {schema}.audit_{modelName}_update_fn()");
        sb.AppendLine("RETURNS TRIGGER AS $$");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"INSERT INTO {historyTableName} (");
        sb.AppendLine($"{columns},");
        sb.AppendLine("audit_validfrom, audit_validto,");
        sb.AppendLine("audit_changedby, audit_changedbysystem, audit_changeoperation");
        sb.AppendLine(") VALUES (");
        sb.AppendLine($"{oldColumns},");
        sb.AppendLine("OLD.audit_validfrom, now(),");
        sb.AppendLine("OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation");
        sb.AppendLine(");");
        sb.AppendLine("RETURN NEW;");
        sb.AppendLine("END;");
        sb.AppendLine("$$ LANGUAGE plpgsql;");

        scripts.Add($"CREATE FUNCTION {schema}.audit_{modelName}_update_fn", sb.ToString());

        return scripts;
    }

    private OrderedDictionary<string, string> CreateAuditDeleteFunction(bool isTranslation)
    {
        var scripts = new OrderedDictionary<string, string>();

        string tableName = GetTableName(includeAlias: false, useTranslation: isTranslation);
        string historyTableName = GetTableName(includeAlias: false, useHistory: true, useTranslation: isTranslation);
        string modelName = _definition.ModelType.Name;
        string schema = GetSchemaName(useHistory: false, useTranslation: isTranslation);

        string columns = string.Join(',', _definition.Properties.Select(t => t.Name));
        string oldColumns = string.Join(',', _definition.Properties.Select(t => $"OLD.{t.Name}"));
        if (isTranslation)
        {
            columns += ", language";
            oldColumns += ", OLD.language";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE OR REPLACE FUNCTION {schema}.audit_{modelName}_delete_fn()");
        sb.AppendLine("RETURNS TRIGGER AS $$");
        sb.AppendLine("DECLARE ctx RECORD;");
        sb.AppendLine("BEGIN");
        sb.AppendLine("SELECT * INTO ctx FROM session_audit_context LIMIT 1;");
        sb.AppendLine($"INSERT INTO {historyTableName} (");
        sb.AppendLine($"{columns},");
        sb.AppendLine("audit_validfrom, audit_validto,");
        sb.AppendLine("audit_changedby, audit_changedbysystem, audit_changeoperation,");
        sb.AppendLine("audit_deletedby, audit_deletedbysystem, audit_deleteoperation");
        sb.AppendLine(") VALUES (");
        sb.AppendLine($"{oldColumns},");
        sb.AppendLine("OLD.audit_validfrom, now(),");
        sb.AppendLine("OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,");
        sb.AppendLine("ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id");
        sb.AppendLine(");");
        sb.AppendLine("RETURN OLD;");
        sb.AppendLine("END;");
        sb.AppendLine("$$ LANGUAGE plpgsql;");

        scripts.Add($"CREATE FUNCTION {schema}.audit_{modelName}_delete_fn", sb.ToString());

        return scripts;
    }

    private OrderedDictionary<string, string> CreateAuditUpdateTrigger(bool isTranslation)
    {
        var scripts = new OrderedDictionary<string, string>();

        string modelName = _definition.ModelType.Name;
        string schema = GetSchemaName(useHistory: false, useTranslation: isTranslation);
        string functionName = $"audit_{modelName}_update_fn";
        string triggerName = $"{modelName}_Audit_Update";
        string tableName = GetTableName(includeAlias: false, useTranslation: isTranslation);

        var sb = new StringBuilder();
        sb.AppendLine($"DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE '{triggerName}' AND t.tgrelid = '{tableName}'::regclass) THEN");
        sb.AppendLine($"CREATE OR REPLACE TRIGGER {triggerName} AFTER UPDATE ON {tableName}");
        sb.AppendLine($"FOR EACH ROW EXECUTE FUNCTION {schema}.{functionName}();");
        sb.AppendLine($"END IF; END $$;");

        scripts.Add($"CREATE TRIGGER {schema}.{triggerName}", sb.ToString());

        return scripts;
    }

    private OrderedDictionary<string, string> CreateAuditDeleteTrigger(bool isTranslation)
    {
        var scripts = new OrderedDictionary<string, string>();

        string modelName = _definition.ModelType.Name;
        string schema = GetSchemaName(useHistory: false, useTranslation: isTranslation);
        string functionName = $"audit_{modelName}_delete_fn";
        string triggerName = $"{modelName}_Audit_Delete";
        string tableName = GetTableName(includeAlias: false, useTranslation: isTranslation);

        var sb = new StringBuilder();
        sb.AppendLine($"DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE '{triggerName}' AND t.tgrelid = '{tableName}'::regclass) THEN");
        sb.AppendLine($"CREATE OR REPLACE TRIGGER {triggerName} AFTER DELETE ON {tableName}");
        sb.AppendLine($"FOR EACH ROW EXECUTE FUNCTION {schema}.{functionName}();");
        sb.AppendLine($"END IF; END $$;");

        scripts.Add($"CREATE TRIGGER {schema}.{triggerName}", sb.ToString());

        return scripts;
    }
    #endregion
}
