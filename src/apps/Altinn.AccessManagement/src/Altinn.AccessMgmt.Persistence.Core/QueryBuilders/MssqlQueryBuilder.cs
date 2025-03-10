using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.QueryBuilders;

/// <summary>
/// QueryBuilder for MSSQL
/// </summary>
public class MssqlQueryBuilder : IDbQueryBuilder
{
    private readonly DbDefinition _definition;
    private readonly DbDefinitionRegistry _definitionRegistry;

    /// <summary>
    /// QueryBuilder for MSSQL
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="definitionRegistry">DbDefinitionRegistry</param>
    public MssqlQueryBuilder(Type type, DbDefinitionRegistry definitionRegistry)
    {
        _definition = definitionRegistry.TryGetDefinition(type) ?? throw new Exception("Missing definition");
        _definitionRegistry = definitionRegistry;
    }

    /// <inheritdoc/>
    public string BuildBasicSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string BuildDeleteQuery()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string BuildExtendedSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string BuildInsertQuery(List<GenericParameter> parameters, bool forTranslation = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string BuildUpdateQuery(List<GenericParameter> parameters, bool forTranslation = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string BuildUpsertQuery(List<GenericParameter> parameters, bool forTranslation = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string BuildUpsertQuery(List<GenericParameter> parameters, List<GenericFilter> mergeFilter, bool forTranslation = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public DbMigrationScriptCollection GetMigrationScripts()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string GetTableName(bool includeAlias = true, bool useHistory = false, bool useTranslation = false, bool useHistoryView = false)
    {
        throw new NotImplementedException();
    }
}
