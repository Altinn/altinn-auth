using System.Collections.Concurrent;
using System.Reflection;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Registry for database definitions.
/// </summary>
public class DbDefinitionRegistry(IOptions<AccessMgmtPersistenceOptions> configuration)
{
    /// <summary>
    /// The internal store for database definitions, keyed by type.
    /// </summary>
    private readonly ConcurrentDictionary<Type, DbDefinition> store = new();
    private readonly ConcurrentDictionary<Type, Lazy<IDbQueryBuilder>> _queryBuilders = new();
    private readonly IOptions<AccessMgmtPersistenceOptions> configuration = configuration;

    /// <summary>
    /// Get QueryBuilder
    /// </summary>
    /// <returns></returns>
    public IDbQueryBuilder GetQueryBuilder<T>()
    {
        return GetQueryBuilder(typeof(T));
    }

    /// <summary>
    /// Get QueryBuilder
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns></returns>
    public IDbQueryBuilder GetQueryBuilder(Type type)
    {
        return _queryBuilders.GetOrAdd(type, t =>
            new Lazy<IDbQueryBuilder>(() =>
            {
                var definition = TryGetDefinition(type);
                return configuration.Value.DbType switch
                {
                    MgmtDbType.Postgres => new PostgresQueryBuilder(configuration, type, this),
                    MgmtDbType.MSSQL => new MssqlQueryBuilder(type, this),
                    _ => throw new NotSupportedException($"Database type '{configuration.Value.DbType.ToString()}' is not supported.")
                };
            })
        ).Value;
    }

    /// <summary>
    /// Configures and registers a database definition for type <typeparamref name="T"/> using a configuration action.
    /// </summary>
    /// <typeparam name="T">The entity type for which the definition is to be registered.</typeparam>
    /// <param name="configure">An action that configures a <see cref="DbDefinitionBuilder{T}"/>.</param>
    public void Define<T>(Action<DbDefinitionBuilder<T>> configure)
    {
        var builder = new DbDefinitionBuilder<T>();
        configure(builder);
        store.AddOrUpdate(
            typeof(T),
            _ => builder.Build(), // Add
            (_, __) => builder.Build() // Update
        );
    }

    /// <summary>
    /// Registers a database definition for type <typeparamref name="T"/> using an existing <see cref="DbDefinitionBuilder{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type for which the definition is to be registered.</typeparam>
    /// <param name="builder">The builder containing the definition configuration.</param>
    public void Define<T>(DbDefinitionBuilder<T> builder)
    {
        store.AddOrUpdate(
            typeof(T),
            _ => builder.Build(), // Add
            (_, __) => builder.Build() // Update
        );
    }

    /// <summary>
    /// Registers a database definition for type <typeparamref name="T"/> using an existing <see cref="DbDefinition"/>.
    /// </summary>
    /// <typeparam name="T">The entity type for which the definition is to be registered.</typeparam>
    /// <param name="dbDefinition">The database definition to register.</param>
    public void Define<T>(DbDefinition dbDefinition)
    {
        store.AddOrUpdate(
            typeof(T),
            _ => dbDefinition, // Add
            (_, __) => dbDefinition // Update
        );
    }

    /// <summary>
    /// Retrieves the registered database definition for type <typeparamref name="T"/>.
    /// If no definition is registered, a new one is built using the default configuration.
    /// </summary>
    /// <typeparam name="T">The entity type whose definition is requested.</typeparam>
    /// <returns>The corresponding <see cref="DbDefinition"/>.</returns>
    public DbDefinition GetOrAddDefinition<T>()
    {
        return store.GetOrAdd(typeof(T), _ => new DbDefinitionBuilder<T>().Build());
    }

    /// <summary>
    /// Retrieves all registered database definitions
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DbDefinition> GetAllDefinitions()
    {
        return store.Values;
    }

    /// <summary>
    /// Attempts to retrieve the base database definition for the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public DbDefinition TryGetBaseDefinition<T>()
    {
        return store.Where(t => t.Value.ModelType == typeof(T)).FirstOrDefault().Value;
    }

    /// <summary>
    /// Attempts to retrieve the registered database definition for the specified type.
    /// </summary>
    /// <param name="type">The entity type for which the definition is requested.</param>
    /// <returns>
    /// The corresponding <see cref="DbDefinition"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public DbDefinition TryGetDefinition(Type type)
    {
        return store.ContainsKey(type) ? store[type] : null;
    }

    /// <summary>
    /// Attempts to retrieve the registered database definition for the specified type.
    /// </summary>
    /// <typeparam name="T">The entity type whose definition is requested.</typeparam>
    /// <returns>
    /// The corresponding <see cref="DbDefinition"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public DbDefinition TryGetDefinition<T>()
    {
        return store.ContainsKey(typeof(T)) ? store[typeof(T)] : null;
    }

    /// <summary>
    /// Scans assemblies for types implementing <see cref="IDbDefinition"/> and registers their definitions.
    /// If a <paramref name="definitionNamespace"/> is provided, only definitions from the specified assembly are loaded.
    /// Otherwise, it scans all assemblies whose name starts with the executing assembly's name.
    /// </summary>
    /// <param name="definitionNamespace">
    /// The assembly name to load definitions from. If empty, the current executing assembly's related assemblies are scanned.
    /// </param>
    public void RegisterAllDefinitions(string definitionNamespace = "")
    {
        List<IDbDefinition> definitions;
        if (string.IsNullOrEmpty(definitionNamespace))
        {
            var executingAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;

            definitions = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name!.StartsWith(executingAssemblyName))
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IDbDefinition).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract
                //// Uncomment and adjust the following line if namespace filtering is needed:
                //// && t.Namespace?.StartsWith("MyProject.Definitions") == true
                )
                .Select(t => (IDbDefinition)Activator.CreateInstance(t)!)
                .ToList();
        }
        else
        {
            var targetAssembly = Assembly.Load(definitionNamespace);
            definitions = targetAssembly.GetTypes()
                .Where(t => typeof(IDbDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (IDbDefinition)Activator.CreateInstance(t)!)
                .ToList();
        }

        foreach (var definition in definitions)
        {
            definition.Define();
        }
    }
}
