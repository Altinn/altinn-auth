using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <inheritdoc/>
public abstract class BasicRepository<T> : IDbBasicRepository<T>
    where T : class, new()
{
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
    /// <param name="dbDefinitionRegistry">DbDefinitionRegistry</param>
    /// <param name="executor">NpgsqlDataSource</param>
    public BasicRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor)
    {
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
    public async Task<T> Get(Guid id, RequestOptions options = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        var res = await Get(new List<GenericFilter>() { new GenericFilter("id", id) }, options, callerName, cancellationToken: cancellationToken);
        return res.Data.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<QueryResponse<T>> Get<TProperty>(Expression<Func<T, TProperty>> property, TProperty value, RequestOptions options = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        string propertyName = ExtractPropertyInfo(property).Name;
        var filters = new List<GenericFilter>
        {
            new GenericFilter(propertyName, value!, FilterComparer.Equals)
        };
        return await Get(filters, options, callerName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QueryResponse<T>> Get(RequestOptions options = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        return await Get(filters: new List<GenericFilter>(), options: options, callerName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QueryResponse<T>> Get(GenericFilterBuilder<T> filterBuilder, RequestOptions options = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        return await Get(filters: filterBuilder, options: options, callerName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QueryResponse<T>> Get(IEnumerable<GenericFilter> filters, RequestOptions options = null, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
    {
        options ??= new RequestOptions();
        filters ??= new List<GenericFilter>();

        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        var query = queryBuilder.BuildBasicSelectQuery(options, filters);
        var param = BuildFilterParameters(filters, options);

        return await executor.ExecuteQuery<T>(query, param, callerName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<TypedDbAudit<T>>> GetFromHistory(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<TypedDbAudit<T>> GetFromHistory(Guid id, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Extracts property info
    /// </summary>
    /// <typeparam name="TLocal"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    protected PropertyInfo ExtractPropertyInfo<TLocal, TProperty>(Expression<Func<TLocal, TProperty>> expression)
    {
        MemberExpression memberExpression = expression.Body switch
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

        var multiple = filters.CountBy(t => t.PropertyName).Where(t => t.Value > 1).Select(t => t.Key);

        foreach (var filter in filters.Where(t => !multiple.Contains(t.PropertyName)))
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

        if (multiple != null && multiple.Any())
        {
            foreach (var m in multiple)
            {
                int a = 1;
                foreach (var filter in filters.Where(t => t.PropertyName == m))
                {
                    parameters.Add(new GenericParameter($"@{m}_{a}", filter.Value));
                    a++;
                }
            }
        }

        if (options != null)
        {
            if (options.Language != null)
            {
                parameters.Add(new GenericParameter("Language", options.Language));
            }

            if (options.AsOf.HasValue)
            {
                parameters.Add(new GenericParameter("_AsOf", options.AsOf.Value));
            }
        }

        return parameters;
    }

    /// <inheritdoc/>
    public async Task<int> Create(T entity, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        var param = BuildParameters(entity);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildInsertQuery(param, options: options);

        return await executor.ExecuteCommand(query, param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Upsert(T entity, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        var param = BuildParameters(entity);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpsertQuery(param, options: options);

        return await executor.ExecuteCommand(query, param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Upsert(T entity, List<GenericFilter> mergeFilter, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        var param = BuildParameters(entity);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpsertQuery(param, mergeFilter, options: options);

        return await executor.ExecuteCommand(query, param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Update(Guid id, T entity, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        var param = BuildParameters(entity);
        return await Update(id: id, parameters: param, cancellationToken: cancellationToken, options: options);
    }

    /// <inheritdoc/>
    public async Task<int> Update<TProperty>(Expression<Func<T, TProperty>> property, TProperty value, Guid id, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        if (value == null)
        {
            var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
            string query = queryBuilder.BuildSingleNullUpdateQuery(new GenericParameter(ExtractPropertyInfo(property).Name, value), options: options);
            return await executor.ExecuteCommand(query, [new GenericParameter("_id", id)], cancellationToken: cancellationToken);
        }

        return await Update(id, [new GenericParameter(ExtractPropertyInfo(property).Name, value)], cancellationToken: cancellationToken, options: options);
    }

    /// <inheritdoc/>
    public async Task<int> Update<TProperty>(Expression<Func<T, TProperty>> property, Guid id, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildSingleNullUpdateQuery(new GenericParameter(ExtractPropertyInfo(property).Name, null), options: options);
        return await executor.ExecuteCommand(query, [new GenericParameter("_id", id)], cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Update(Guid id, List<GenericParameter> parameters, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpdateQuery(parameters, options: options);
        parameters.Add(new GenericParameter("_id", id));
        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> Delete(Guid id, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        return await Delete([new GenericFilter("id", id)], cancellationToken: cancellationToken, options: options);
    }

    /// <inheritdoc/>
    public async Task<int> Delete(IEnumerable<GenericFilter> filters, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        // TODO: Implement DeletedBy ... Somehow
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        var param = BuildFilterParameters(filters, null);
        string query = queryBuilder.BuildDeleteQuery(filters, options: options);
        return await executor.ExecuteCommand(query, param, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CreateTranslation(T obj, string language, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        if (!Definition.EnableTranslation)
        {
            return 0;
        }

        var parameters = BuildTranslationParameters(obj);
        parameters.Add(new GenericParameter("Language", language));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildInsertQuery(parameters, forTranslation: true, options: options);

        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> UpdateTranslation(Guid id, T obj, string language, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        if (!Definition.EnableTranslation)
        {
            return 0;
        }

        var parameters = BuildTranslationParameters(obj);
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpdateQuery(parameters, forTranslation: true, options: options);

        parameters.Add(new GenericParameter("_language", language));
        parameters.Add(new GenericParameter("_id", id));

        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> UpsertTranslation(Guid id, T obj, string language, ChangeRequestOptions options = null, CancellationToken cancellationToken = default)
    {
        if (Definition.DefinitionType != DbDefinitionType.Table)
        {
            throw new Exception($"'{Definition.ModelType.Name}' is not defined as a table");
        }

        if (!Definition.EnableTranslation)
        {
            return 0;
        }

        var parameters = BuildTranslationParameters(obj);
        parameters.Add(new GenericParameter("Language", language));
        var queryBuilder = definitionRegistry.GetQueryBuilder<T>();
        string query = queryBuilder.BuildUpsertQuery(parameters, forTranslation: true, options: options);

        parameters.Add(new GenericParameter("_language", language));
        parameters.Add(new GenericParameter("_id", id));

        return await executor.ExecuteCommand(query, parameters, cancellationToken: cancellationToken);
    }
    #endregion

}
