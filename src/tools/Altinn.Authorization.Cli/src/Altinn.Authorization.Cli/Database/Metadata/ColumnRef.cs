using CommunityToolkit.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents a reference to a column.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("{Name} (in {Table.Schema}.{Table.Name})")]
public class ColumnRef
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnRef"/> class.
    /// </summary>
    /// <param name="table">The table reference.</param>
    /// <param name="name">The column name.</param>
    public ColumnRef(
        TableRef table,
        string name)
    {
        Guard.IsNotNull(table);
        Guard.IsNotNullOrWhiteSpace(name);

        Table = table;
        Name = name;
    }

    /// <summary>
    /// Gets the table reference.
    /// </summary>
    public TableRef Table { get; }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name { get; }
}
