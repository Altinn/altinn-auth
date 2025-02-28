using System.Diagnostics.CodeAnalysis;

namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Base class for database definitions.
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseDbDefinition<T>
{
    /// <summary>
    /// DbDefinitionRegistry
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed")]
    protected readonly DbDefinitionRegistry definitionRegistry;

    /// <summary>
    /// Gets the current definition.
    /// </summary>
    public DbDefinition DbDefinition { get { return definitionRegistry.GetOrAddDefinition<T>(); } }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDbDefinition{T}"/> class.
    /// </summary>
    /// <param name="definitionRegistry">DbDefinitionRegistry</param>
    public BaseDbDefinition(DbDefinitionRegistry definitionRegistry)
    {
        this.definitionRegistry = definitionRegistry;
    }
}
