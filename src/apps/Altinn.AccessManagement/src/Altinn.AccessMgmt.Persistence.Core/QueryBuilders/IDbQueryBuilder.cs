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
    /// Builds a INSERT query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildInsertQuery(List<GenericParameter> parameters, bool forTranslation = false);

    /// <summary>
    /// Builds a DELETE query
    /// </summary>
    /// <returns></returns>
    string BuildDeleteQuery();

    /// <summary>
    /// Builds a UPDATE query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildUpdateQuery(List<GenericParameter> parameters, bool forTranslation = false);

    /// <summary>
    /// Builds a UPSERT query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildUpsertQuery(List<GenericParameter> parameters, bool forTranslation = false);

    /// <summary>
    /// Builds a UPSERT query
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="mergeFilter">Parameters for merge statement</param>
    /// <param name="forTranslation">Is this for a translation table</param>
    /// <returns></returns>
    string BuildUpsertQuery(List<GenericParameter> parameters, List<GenericFilter> mergeFilter, bool forTranslation = false);

    /// <summary>
    /// Generates mirgration scripts for the definition
    /// </summary>
    /// <returns></returns>
    DbMigrationScriptCollection GetMigrationScripts();
}
