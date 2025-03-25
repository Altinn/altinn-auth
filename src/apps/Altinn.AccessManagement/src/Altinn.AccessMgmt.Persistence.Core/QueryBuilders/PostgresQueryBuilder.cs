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
            sb.AppendLine($"set local x.asof = '{options.AsOf.Value.ToUniversalTime()}';");
        }

        sb.AppendLine("SELECT ");
        sb.AppendLine(GenerateColumns(options));
        sb.AppendLine("FROM " + GenerateSource(options));

        if (crossDef != null)
        {
            sb.AppendLine(GenerateCrossReferenceJoin(crossDef, options));
        }

        sb.AppendLine(GenerateFilterStatement(_definition.ModelType.Name, filters));

        string query = sb.ToString();
        return AddPagingToQuery(query, options);
    }

    /// <inheritdoc/>
    public string BuildExtendedSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null)
    {
        var sb = new StringBuilder();

        // Apply session settings (e.g., as-of)
        if (options.AsOf.HasValue)
        {
            sb.AppendLine($"set local x.asof = '{options.AsOf.Value.ToUniversalTime()}';");
        }

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

        string query = AddPagingToQuery(sb.ToString(), options);
        Console.WriteLine(query);

        return query;
    }

    /// <inheritdoc/>
    public string BuildInsertQuery(List<GenericParameter> parameters, bool forTranslation = false)
    {
        return $"INSERT INTO {GetTableName(includeAlias: false, useTranslation: forTranslation)} ({InsertColumns(parameters)}) VALUES({InsertValues(parameters)})";
    }

    /// <inheritdoc/>
    public string BuildUpdateQuery(List<GenericParameter> parameters, bool forTranslation = false)
    {
        return $"UPDATE {GetTableName(includeAlias: false, useTranslation: forTranslation)} SET {UpdateSetStatement(parameters)} WHERE id = @_id{(forTranslation ? " AND language = @_language" : string.Empty)}";
    }

    /// <inheritdoc/>
    public string BuildUpsertQuery(List<GenericParameter> parameters, bool forTranslation = false)
    {
        return BuildMergeQuery(parameters, [new GenericFilter("id", "id")], forTranslation);

        /*
        var sb = new StringBuilder();
        sb.AppendLine($"INSERT INTO {GetTableName(includeAlias: false, useTranslation: forTranslation)} ({InsertColumns(parameters)}) VALUES({InsertValues(parameters)})");
        sb.AppendLine(" ON CONFLICT (id) DO ");
        sb.AppendLine($"UPDATE SET {UpdateSetStatement(parameters)}");
        return sb.ToString();
        */
    }

    /// <inheritdoc/>
    public string BuildUpsertQuery(List<GenericParameter> parameters, List<GenericFilter> mergeFilter, bool forTranslation = false)
    {
        return BuildMergeQuery(parameters, mergeFilter, forTranslation);
    }

    private string BuildMergeQuery(List<GenericParameter> parameters, List<GenericFilter> mergeFilter, bool forTranslation = false)
    {
        if (mergeFilter == null || !mergeFilter.Any())
        {
            throw new ArgumentException("Missing mergefilter");
        }

        var sb = new StringBuilder();
        sb.AppendLine("WITH N AS ( SELECT ");
        sb.AppendLine("@id as id");
        if (forTranslation)
        {
            sb.AppendLine(", @language as language");
        }

        sb.AppendLine(")");

        var mergeStatementFilter = string.Join(" AND ", mergeFilter.Select(t => $"T.{t.PropertyName} = N.{t.PropertyName}"));
        sb.AppendLine($"MERGE INTO {GetTableName(includeAlias: false, useTranslation: forTranslation)} AS T USING N ON {mergeStatementFilter}");
        if (forTranslation)
        {
            sb.AppendLine(" AND T.language = N.language");
        }

        sb.AppendLine("WHEN MATCHED");

        sb.AppendLine($"AND ({MergeUpdateMatchStatement(parameters)})");

        sb.AppendLine("THEN");
        sb.AppendLine($"UPDATE SET {UpdateSetStatement(parameters)}");
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine($"INSERT ({InsertColumns(parameters)}) VALUES({InsertValues(parameters)});");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string BuildDeleteQuery(IEnumerable<GenericFilter> filters)
    {
        var filterStatement = GenerateFilterStatement(_definition.ModelType.Name, filters);
        return $"DELETE FROM {GetTableName(includeAlias: false)} {filterStatement}";
    }

    /// <inheritdoc />
    public string GetTableName(bool includeAlias = true, bool useHistory = false, bool useTranslation = false, bool useHistoryView = false)
    {
        return GetTableName(_definition, includeAlias, useHistory, useTranslation, useHistoryView);
    }

    private string GetTableName(DbDefinition dbDefinition, bool includeAlias = true, bool useHistory = false, bool useTranslation = false, bool useHistoryView = false)
    {
        var config = this._config.Value;

        string res = string.Empty;
        if (useHistory)
        {
            string historyTablePrefix = useHistoryView ? string.Empty : "_";
            if (useTranslation)
            {
                res = $"{config.TranslationHistorySchema}.{historyTablePrefix}{dbDefinition.ModelType.Name}";
            }
            else
            {
                res = $"{config.BaseHistorySchema}.{historyTablePrefix}{dbDefinition.ModelType.Name}";
            }
        }
        else
        {
            if (useTranslation)
            {
                res = $"{config.TranslationSchema}.{dbDefinition.ModelType.Name}";
            }
            else
            {
                res = $"{config.BaseSchema}.{dbDefinition.ModelType.Name}";
            }
        }

        if (includeAlias)
        {
            res += $" AS {dbDefinition.ModelType.Name}";
        }

        return res;
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
        bool useTranslation = !string.IsNullOrEmpty(options.Language) && _definition.HasTranslation;
        var columns = new List<string>();

        foreach (var p in _definition.Properties.Select(t => t.Property))
        {
            if (useTranslation && p.PropertyType == typeof(string))
            {
                columns.Add($"coalesce(T_{_definition.ModelType.Name}.{p.Name}, {_definition.ModelType.Name}.{p.Name}) AS {p.Name}");
            }
            else
            {
                columns.Add($"{_definition.ModelType.Name}.{p.Name} AS {p.Name}");
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
        bool useTranslation = !string.IsNullOrEmpty(options.Language) && _definition.HasTranslation;
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
                conditions.Add($"{tableAlias}.{m} IN ({string.Join(",", notInList)})");
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
        sb.AppendLine("FROM pagedresult, (SELECT MAX(pagedresult._rownum) AS totalitems FROM pagedresult) AS pageinfo");
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
        if (useTranslation && joinDef.HasTranslation)
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
                if (joinDef.HasTranslation && useTranslation && p.PropertyType == typeof(string))
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

        if (_definition.IsView)
        {
            scriptCollection.AddScripts(CreateView(_definition.ViewVersion));
            foreach (var dep in _definition.ViewDependencies)
            {
                scriptCollection.AddDependency(dep);
            }

            return scriptCollection;
        }

        scriptCollection.AddScripts(CreateTable());

        var pk = _definition.Constraints.FirstOrDefault(t => t.IsPrimaryKey);
        foreach (var column in _definition.Properties)
        {
            if (pk != null && pk.Properties.ContainsKey(column.Name))
            {
                continue;
            }

            scriptCollection.AddScripts(CreateColumn(column));
        }

        foreach (var fk in _definition.Relations)
        {
            scriptCollection.AddScripts(CreateForeignKeyConstraint(fk));
            scriptCollection.AddDependency(fk.Ref);
        }

        foreach (var uc in _definition.Constraints)
        {
            if (uc.IsPrimaryKey)
            {
                continue;
            }

            scriptCollection.AddScripts(CreateUniqueConstraint(uc));
        }

        if (_definition.HasHistory)
        {
            scriptCollection.AddScripts(CreateSharedHistoryFunction());
            scriptCollection.AddScripts(CreateHistoryTriggersAndView(false));

            if (_definition.HasTranslation)
            {
                scriptCollection.AddScripts(CreateHistoryTriggersAndView(true));
            }
        }

        return scriptCollection;
    }

    private OrderedDictionary<string, string> CreateView(int version = 1)
    {
        var scripts = new OrderedDictionary<string, string>();

        var query = $"""
        CREATE OR REPLACE VIEW {GetTableName(includeAlias: false)} AS
        {_definition.ViewQuery}
        """;

        scripts.Add($"CREATE VIEW {GetTableName(includeAlias: false)} {version}", query);

        return scripts;
    }

    private OrderedDictionary<string, string> CreateTable()
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

        string basicKey = $"CREATE TABLE {GetTableName(includeAlias: false)}";
        var basicTable = new StringBuilder();
        basicTable.AppendLine($"CREATE TABLE {GetTableName(includeAlias: false)} (");
        basicTable.AppendLine(primaryKeyDefinition);
        if (_definition.HasHistory)
        {
            basicTable.AppendLine(", validfrom timestamptz default now()");
        }

        basicTable.AppendLine($", CONSTRAINT PK_{_definition.ModelType.Name} PRIMARY KEY ({string.Join(',', pk.Properties.Select(t => $"{t.Key}"))})");
        basicTable.AppendLine(");");

        scripts.Add(basicKey, basicTable.ToString());

        if (_definition.HasTranslation)
        {
            string translationKey = $"CREATE TABLE {GetTableName(includeAlias: false, useTranslation: true)}";
            var translationTable = new StringBuilder();
            translationTable.AppendLine($"CREATE TABLE {GetTableName(includeAlias: false, useTranslation: true)} (");
            translationTable.AppendLine(primaryKeyDefinition);
            if (_definition.HasHistory)
            {
                translationTable.AppendLine(", validfrom timestamptz default now()");
            }

            translationTable.AppendLine($", Language text NOT NULL");
            translationTable.AppendLine($", CONSTRAINT PK_{_definition.ModelType.Name} PRIMARY KEY ({string.Join(',', _definition.Constraints.First(t => t.IsPrimaryKey).Properties.Select(t => $"{t.Key}"))}, Language)");
            translationTable.AppendLine(");");

            scripts.Add(translationKey, translationTable.ToString());
        }

        if (_definition.HasHistory)
        {
            string historyKey = $"CREATE TABLE {GetTableName(includeAlias: false, useHistory: true)}";
            var historyTable = new StringBuilder();
            historyTable.AppendLine($"CREATE TABLE {GetTableName(includeAlias: false, useHistory: true)}(");
            historyTable.AppendLine(primaryKeyDefinition);
            historyTable.AppendLine(", validfrom timestamptz default now()");
            historyTable.AppendLine(", validto timestamptz default now()");
            historyTable.AppendLine(");");

            scripts.Add(historyKey, historyTable.ToString());

            if (_definition.HasTranslation)
            {
                string historyTranslationKey = $"CREATE TABLE {GetTableName(includeAlias: false, useTranslation: true, useHistory: true)}";
                var historyTranslationTable = new StringBuilder();
                historyTranslationTable.AppendLine($"CREATE TABLE {GetTableName(includeAlias: false, useTranslation: true, useHistory: true)}(");
                historyTranslationTable.AppendLine(primaryKeyDefinition);
                historyTranslationTable.AppendLine(", validfrom timestamptz default now()");
                historyTranslationTable.AppendLine(", validto timestamptz default now()");
                historyTranslationTable.AppendLine(", Language text NOT NULL");
                historyTranslationTable.AppendLine(");");

                scripts.Add(historyTranslationKey, historyTranslationTable.ToString());
            }
        }

        return scripts;
    }

    private OrderedDictionary<string, string> CreateColumn(DbPropertyDefinition column)
    {
        var scripts = new OrderedDictionary<string, string>();

        var basic = CreateColumn(GetTableName(includeAlias: false), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), column.IsNullable, column.DefaultValue);
        scripts.Add(basic.Key, basic.Query);

        if (_definition.HasTranslation)
        {
            var translation = CreateColumn(GetTableName(includeAlias: false, useTranslation: true), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), true, column.DefaultValue);
            scripts.Add(translation.Key, translation.Query);
        }

        if (_definition.HasHistory)
        {
            var history = CreateColumn(GetTableName(includeAlias: false, useHistory: true), column.Name, DbHelperMethods.GetPostgresType(column.Property).ToString(), true, column.DefaultValue);
            scripts.Add(history.Key, history.Query);

            if (_definition.HasTranslation)
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
        var query = $"ALTER TABLE {tableName} ADD {columnName} {columnDataType} {(isNullable ? "NULL" : "NOT NULL")}{(string.IsNullOrEmpty(defaultValue) ? string.Empty : $" DEFAULT {defaultValue}")};";

        return (key, query);
    }

    private (string Key, string Query) CreateUniqueConstraint(DbConstraintDefinition constraint)
    {
        string name = string.IsNullOrEmpty(constraint.Name) ? _definition.ModelType.Name : constraint.Name;

        var props = _definition.ModelType.GetProperties();

        foreach (var property in constraint.Properties)
        {
            if (_definition.ModelType.GetProperties().Count(t => t.Name.Equals(property.Key)) == 0)
            {
                throw new Exception($"Property {property.Key} not found on {_definition.ModelType.Name}");
            }
        }

        string key = $"ADD CONSTRAINT {GetTableName(includeAlias: false)}.{name}";
        string query = $"ALTER TABLE {GetTableName(includeAlias: false)} ADD CONSTRAINT {name} UNIQUE ({string.Join(',', constraint.Properties.Keys)});";

        return (key, query);
    }

    private (string Key, string Query) CreateForeignKeyConstraint(DbRelationDefinition foreignKey)
    {
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
        var query = $"ALTER TABLE {GetTableName(includeAlias: false)} ADD CONSTRAINT {name} FOREIGN KEY ({foreignKey.BaseProperty}) REFERENCES {GetTableName(targetDef, includeAlias: false)} ({foreignKey.RefProperty}) {(foreignKey.UseCascadeDelete ? "ON DELETE CASCADE" : "ON DELETE SET NULL")};";

        return (key, query);
    }

    private OrderedDictionary<string, string> CreateSharedHistoryFunction()
    {
        var res = new OrderedDictionary<string, string>();

        string key = "FUNCTION update_validfrom";
        string query = """
        CREATE OR REPLACE FUNCTION dbo.update_validfrom() RETURNS trigger AS
        $$
        BEGIN
            NEW.validfrom := NOW();
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
        """;

        res.Add(key, query);
        return res;
    }

    private OrderedDictionary<string, string> CreateHistoryTriggersAndView(bool forTranslationTable)
    {
        var scripts = new OrderedDictionary<string, string>();

        /*
        var shared = CreateSharedHistoryFunction();
        scripts.Add(shared.key, shared.query);
        */

        string tableName = GetTableName(includeAlias: false, useTranslation: forTranslationTable);

        string columnDefinitions = string.Join(',', _definition.Properties.Select(t => t.Name));
        string columnOldDefinitions = string.Join(',', _definition.Properties.Select(t => $"OLD.{t.Name}"));

        string keyTriggerUpdateValidFrom = $"TRIGGER {_definition.ModelType.Name}_update_validfrom ON {tableName}";
        string queryTriggerUpdateValidFrom = $"""
        CREATE OR REPLACE TRIGGER {_definition.ModelType.Name}_update_validfrom BEFORE UPDATE ON {tableName}
        FOR EACH ROW EXECUTE FUNCTION dbo.update_validfrom();
        """;

        scripts.Add(keyTriggerUpdateValidFrom, queryTriggerUpdateValidFrom);

        string copyToHistoryFuncName = $"{tableName}_copy_to_history()";
        string keyCopyToHistory = $"FUNCTION {copyToHistoryFuncName}";
        string queryCopyToHistory = $"""
        CREATE OR REPLACE FUNCTION {copyToHistoryFuncName} RETURNS TRIGGER AS $$ BEGIN
        INSERT INTO {GetTableName(includeAlias: false, useHistory: true, useTranslation: forTranslationTable)} ({columnDefinitions}, {(forTranslationTable ? "language, " : string.Empty)}validfrom, validto) VALUES({columnOldDefinitions}, {(forTranslationTable ? "OLD.language, " : string.Empty)}OLD.validfrom, now());
        RETURN NEW;
        END; $$ LANGUAGE plpgsql;
        CREATE OR REPLACE TRIGGER {_definition.ModelType.Name}_History AFTER UPDATE ON {tableName}
        FOR EACH ROW EXECUTE FUNCTION {copyToHistoryFuncName};
        """;
        scripts.Add(keyCopyToHistory, queryCopyToHistory);

        string viewKey = $"VIEW {GetTableName(includeAlias: false, useHistory: true, useHistoryView: true, useTranslation: forTranslationTable)}";
        string viewQuery = $"""
        CREATE OR REPLACE VIEW {GetTableName(includeAlias: false, useHistory: true, useHistoryView: true, useTranslation: forTranslationTable)} AS
        SELECT {columnDefinitions}, {(forTranslationTable ? "language, " : string.Empty)} validfrom, validto
        FROM  {GetTableName(useHistory: true, useTranslation: forTranslationTable)}
        WHERE validfrom <= coalesce(current_setting('x.asof', true)::timestamptz, now())
        AND validto > coalesce(current_setting('x.asof', true)::timestamptz, now())
        UNION ALL
        SELECT  {columnDefinitions}, {(forTranslationTable ? "language, " : string.Empty)}validfrom, now() AS validto
        FROM {tableName}
        where validfrom <= coalesce(current_setting('x.asof', true)::timestamptz, now());
        """;
        scripts.Add(viewKey, viewQuery);

        return scripts;
    }
    #endregion
}
