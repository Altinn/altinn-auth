using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Executors;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <inheritdoc/>
public abstract class CrossRepository<T, TExtended, TA, TB> : ExtendedRepository<T, TExtended>, IDbCrossRepository<T, TExtended, TA, TB>
    where T : class, new()
    where TExtended : class, new()
    where TA : class, new()
    where TB : class, new()
{

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossRepository{T, TExtended, TA, TB}"/> class.
    /// </summary>
    /// <param name="dbDefinitionRegistry">DbDefinitionRegistry</param>
    /// <param name="executor">IDbExecutor</param>
    protected CrossRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    { }

    /// <inheritdoc/>
    public async Task<int> CreateCross(Guid AIdentity, Guid BIdentity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<int> DeleteCross(Guid AIdentity, Guid BIdentity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TA>> GetA(Guid id, RequestOptions options, List<GenericFilter>? filters = null, CancellationToken cancellationToken = default)
        => await GetCrossReferencedEntities<TA>(id, options, filters, cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<TB>> GetB(Guid id, RequestOptions options, List<GenericFilter>? filters = null, CancellationToken cancellationToken = default)
        => await GetCrossReferencedEntities<TB>(id, options, filters, cancellationToken);

    /// <summary>
    /// Retrieves entities from a cross-reference table based on the given type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to retrieve.</typeparam>
    /// <param name="id">The cross-reference identifier.</param>
    /// <param name="options">Query request options.</param>
    /// <param name="filters">Optional filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of entities of type <typeparamref name="TEntity"/>.</returns>
    private async Task<IEnumerable<TEntity>> GetCrossReferencedEntities<TEntity>(
        Guid id, RequestOptions options, List<GenericFilter>? filters = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        // Get the definition for the entity type
        var def = definitionRegistry.TryGetDefinition<TEntity>() ?? definitionRegistry.TryGetBaseDefinition<TEntity>();

        if (def == null)
        {
            throw new Exception($"GetOrAddDefinition not found for {typeof(TEntity).Name}");
        }

        bool isExtended = def != definitionRegistry.TryGetDefinition<TEntity>();

        options ??= new RequestOptions();
        filters ??= new List<GenericFilter>();

        var queryBuilder = definitionRegistry.GetQueryBuilder<TEntity>();
        string query = query = isExtended
            ? queryBuilder.BuildExtendedSelectQuery(options, filters, Definition.CrossRelation)
            : queryBuilder.BuildBasicSelectQuery(options, filters, Definition.CrossRelation);

        if (string.IsNullOrEmpty(query))
        {
            throw new Exception($"Query not found for {typeof(TEntity).Name}");
        }

        var param = BuildFilterParameters(filters, options);
        param.Add(new GenericParameter("X_Id", id));

        return await executor.ExecuteQuery<TEntity>(query, param, cancellationToken);
    }
}
