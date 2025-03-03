using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Executors;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <inheritdoc/>
public abstract class BasicRepository<T> : IDbBasicRepository<T>
    where T : class, new()
{
    /// <summary>
    /// DbAccessConfig
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed")]
    //protected readonly DbAccessConfig config;

    /// <summary>
    /// DbDefinitionRegistry
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed")]
    protected readonly DbDefinitionRegistry definitionRegistry;

    /// <summary>
    /// NpgsqlDataSource
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed")]
    protected IDbExecutor executor;

    /// <summary>
    /// BasicRepository
    /// </summary>
    /// <param name="options">DbAccessConfig</param>
    /// <param name="dbDefinitionRegistry">DbDefinitionRegistry</param>
    /// <param name="executor">NpgsqlDataSource</param>
    public BasicRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor)
    {
        //config = options.Value;
        this.definitionRegistry = dbDefinitionRegistry;
        this.executor = executor;
    }

    /// <summary>
    /// GetOrAddDefinition
    /// </summary>
    public DbDefinition Definition { get { return definitionRegistry.GetOrAddDefinition<T>(); } }

    /// <inheritdoc/>
    public GenericFilterBuilder<T> CreateFilterBuilder() { return new GenericFilterBuilder<T>(); }

    #region Read

    /// <inheritdoc/>
    public async Task<T?> Get(Guid id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        var res = await Get(new List<GenericFilter>() { new GenericFilter("id", id) }, options, cancellationToken: cancellationToken);
        return res.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> Get<TProperty>(Expression<Func<T, TProperty>> property, TProperty value, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        string propertyName = ExtractPropertyInfo(property).Name;
        var filters = new List<GenericFilter>
        {
            new GenericFilter(propertyName, value!, FilterComparer.Equals)
        };
        return await Get(filters, options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> Get(RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Get(filters: new List<GenericFilter>(), options: options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> Get(GenericFilterBuilder<T> filterBuilder, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Get(filters: filterBuilder, options: options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> Get(IEnumerable<GenericFilter> filters, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new RequestOptions();
        filters ??= new List<GenericFilter>();

        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        var query = queryBuilder.BuildBasicSelectQuery(options, filters);
        var param = BuildFilterParameters(filters, options);

        return await executor.ExecuteQuery<T>(query, param, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Extracts property info
    /// </summary>
    /// <typeparam name="TLocal"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    protected PropertyInfo ExtractPropertyInfo<TLocal, TProperty>(Expression<Func<TLocal, TProperty>> expression)
    {
        MemberExpression? memberExpression = expression.Body switch
        {
            MemberExpression member => member,
            UnaryExpression { Operand: MemberExpression member } => member,
            _ => null
        };

        return memberExpression?.Member as PropertyInfo ?? throw new ArgumentException($"Expression '{expression}' does not refer to a valid property.");
    }
    #endregion

    #region Write

    /// <summary>
    /// BuildParameters
    /// </summary>
    /// <param name="obj">Object to extract parameters</param>
    /// <returns></returns>
    protected List<GenericParameter> BuildParameters(object obj)
    {
        var parameters = new List<GenericParameter>();
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            parameters.Add(new GenericParameter(property.Name, property.GetValue(obj) ?? DBNull.Value));
        }

        return parameters;
    }

    /// <summary>
    /// BuildTranslationParameters
    /// </summary>
    /// <param name="obj">Object to extract parameters</param>
    /// <returns></returns>
    protected List<GenericParameter> BuildTranslationParameters(object obj)
    {
        var parameters = new List<GenericParameter>();
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(string) || property.Name == "Id")
            {
                parameters.Add(new GenericParameter(property.Name, property.GetValue(obj) ?? DBNull.Value));
            }
        }

        return parameters;
    }

    /// <summary>
    /// BuildFilterParameters
    /// </summary>
    /// <param name="filters">GenericFilter</param>
    /// <param name="options">RequestOptions</param>
    /// <returns></returns>
    protected List<GenericParameter> BuildFilterParameters(IEnumerable<GenericFilter> filters, RequestOptions options)
    {
        var parameters = new List<GenericParameter>();

        foreach (var filter in filters)
        {
            object value = filter.Comparer switch
            {
                FilterComparer.StartsWith => $"{filter.Value}%",
                FilterComparer.EndsWith => $"%{filter.Value}",
                FilterComparer.Contains => $"%{filter.Value}%",
                _ => filter.Value
            };

            parameters.Add(new GenericParameter(filter.PropertyName, value));
        }

        if (options.Language != null)
        {
            parameters.Add(new GenericParameter("Language", options.Language));
        }

        if (options.AsOf.HasValue)
        {
            parameters.Add(new GenericParameter("_AsOf", options.AsOf.Value));
        }

        return parameters;
    }

    /// <inheritdoc/>
    public async Task<int> Ingest(List<T> data, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        //// var importer = new BulkImporter<T>(connection, GetOrAddDefinition);
        //// return await importer.Ingest(data, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Create(T entity, CancellationToken cancellationToken = default)
    {
        var param = BuildParameters(entity);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildInsertQuery(param);

        return await executor.ExecuteCommand(query, param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Upsert(T entity, CancellationToken cancellationToken = default)
    {
        var param = BuildParameters(entity);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpsertQuery(param);

        return await executor.ExecuteCommand(query, param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Update(Guid id, T entity, CancellationToken cancellationToken = default)
    {
        var param = BuildParameters(entity);
        return await Update(id: id, parameters: param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Update(Guid id, List<GenericParameter> parameters, CancellationToken cancellationToken = default)
    {
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpdateQuery(parameters);
        parameters.Add(new GenericParameter("_id", id));
        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildDeleteQuery();
        return await executor.ExecuteCommand(query, [new GenericParameter("_id", id)], cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CreateTranslation(T obj, string language, CancellationToken cancellationToken = default)
    {
        if (!Definition.HasTranslation)
        {
            return 0;
        }

        var parameters = BuildTranslationParameters(obj);
        parameters.Add(new GenericParameter("Language", language));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildInsertQuery(parameters, forTranslation: true);

        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> UpdateTranslation(Guid id, T obj, string language, CancellationToken cancellationToken = default)
    {
        if (!Definition.HasTranslation)
        {
            return 0;
        }

        var parameters = BuildTranslationParameters(obj);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpdateQuery(parameters, forTranslation: true);

        parameters.Add(new GenericParameter("_language", language));
        parameters.Add(new GenericParameter("_id", id));

        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> UpsertTranslation(Guid id, T obj, string language, CancellationToken cancellationToken = default)
    {
        if (!Definition.HasTranslation)
        {
            return 0;
        }

        var parameters = BuildTranslationParameters(obj);
        parameters.Add(new GenericParameter("Language", language));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpsertQuery(parameters, forTranslation: true);

        parameters.Add(new GenericParameter("_language", language));
        parameters.Add(new GenericParameter("_id", id));

        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    #endregion

}
