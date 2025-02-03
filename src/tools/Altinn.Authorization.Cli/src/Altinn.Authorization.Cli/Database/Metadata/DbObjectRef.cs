using CommunityToolkit.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents a reference to a database object.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("{Schema}.{Name}")]
public abstract class DbObjectRef
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbObjectRef"/> class.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <param name="name">The object name.</param>
    protected DbObjectRef(
        string schema,
        string name)
    {
        Guard.IsNotNullOrWhiteSpace(schema);
        Guard.IsNotNullOrWhiteSpace(name);

        Schema = schema;
        Name = name;
    }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public string Schema { get; }

    /// <summary>
    /// Gets the object name.
    /// </summary>
    public string Name { get; }
}
