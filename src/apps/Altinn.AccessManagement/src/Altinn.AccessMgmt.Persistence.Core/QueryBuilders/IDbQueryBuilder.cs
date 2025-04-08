using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.QueryBuilders;

/// <summary>
/// Interface for building database queries
/// </summary>
public interface IDbQueryBuilder
{
    /// <summary>
    /// Builds a SELECT query for basic types
    /// </summary>
    string BuildBasicSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null);

    /// <summary>
    /// Builds a SELECT query for extended types
    /// </summary>
    string BuildExtendedSelectQuery(RequestOptions options, IEnumerable<GenericFilter> filters, DbCrossRelationDefinition crossDef = null);

    /// <summary>
    /// Gets table name
    /// </summary>
    /// <returns></returns>
    string GetTableName(bool includeAlias = true, bool useHistory = false, bool useTranslation = false, bool useHistoryView = false, bool includeSchema = true);

    /// <summary>
    /// Builds a INSERT query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="options">Options for the request</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildInsertQuery(List<GenericParameter> parameters, ChangeRequestOptions options, bool forTranslation = false);

    /// <summary>
    /// Builds a DELETE query
    /// </summary>
    /// <returns></returns>
    /// <param name="filters">Filters</param>
    /// <param name="options">Options for the request</param>
    /// <returns></returns>
    string BuildDeleteQuery(IEnumerable<GenericFilter> filters, ChangeRequestOptions options);

    /// <summary>
    /// Builds a UPDATE query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="options">Options for the request</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildUpdateQuery(List<GenericParameter> parameters, ChangeRequestOptions options, bool forTranslation = false);

    /// <summary>
    /// Builds a UPDATE query
    /// </summary>
    /// <param name="parameter">Parameter</param>
    /// <param name="options">Options for the request</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildSingleNullUpdateQuery(GenericParameter parameter, ChangeRequestOptions options, bool forTranslation = false);

    /// <summary>
    /// Builds a UPSERT query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildUpsertQuery(List<GenericParameter> parameters, ChangeRequestOptions options, bool forTranslation = false);

    /// <summary>
    /// Builds a UPSERT query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="mergeFilter">Parameters for merge statement</param>
    /// <param name="options">Options for the request</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildUpsertQuery(List<GenericParameter> parameters, List<GenericFilter> mergeFilter, ChangeRequestOptions options, bool forTranslation = false);

    /// <summary>
    /// Generates mirgration scripts for the definition
    /// </summary>
    /// <returns></returns>
    DbMigrationScriptCollection GetMigrationScripts();
}
