using System.Linq.Expressions;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Executors;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <inheritdoc/>
public abstract class ExtendedRepository<T, TExtended> : BasicRepository<T>, IDbExtendedRepository<T, TExtended>
    where T : class, new()
    where TExtended : class, new()
{
    /// <inheritdoc/>
    protected ExtendedRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    {
    }

    /// <inheritdoc/>
    public async Task<TExtended> GetExtended(Guid id, RequestOptions options = null, CancellationToken cancellationToken = default)
    {
        var res = await GetExtended([new GenericFilter("Id", id)], options, cancellationToken);
        return res.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TExtended>> GetExtended(RequestOptions options = null, CancellationToken cancellationToken = default)
    {
        return await GetExtended(filters: [], options, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TExtended>> GetExtended<TProperty>(Expression<Func<TExtended, TProperty>> property, TProperty value, RequestOptions options = null, CancellationToken cancellationToken = default)
    {
        string propertyName = ExtractPropertyInfo(property).Name;
        var filters = new List<GenericFilter>
        {
            new GenericFilter(propertyName, value!, FilterComparer.Equals)
        };
        return await GetExtended(filters, options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TExtended>> GetExtended(GenericFilterBuilder<TExtended> filter, RequestOptions options = null, CancellationToken cancellationToken = default)
    {
        return await GetExtended(filters: filter, options, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TExtended>> GetExtended(IEnumerable<GenericFilter> filters, RequestOptions options = null, CancellationToken cancellationToken = default)
    {
        options ??= new RequestOptions();
        filters ??= new List<GenericFilter>();

        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        var query = queryBuilder.BuildExtendedSelectQuery(options, filters);
        var param = BuildFilterParameters(filters, options);

        return await executor.ExecuteQuery<TExtended>(query, param, cancellationToken);
    }
}
