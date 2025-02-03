using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents information about a table.
/// </summary>
[ExcludeFromCodeCoverage]
public class TableInfo
    : TableRef
{

    /// <summary>
    /// Initializes a new instance of the <see cref="TableInfo"/> class.
    /// </summary>
    /// <param name="schema">The schema name.</param>
    /// <param name="name">The table name.</param>
    /// <param name="columnNames">The column names.</param>
    /// <param name="estimatedRowCount">The estimated number of rows in the table.</param>
    public TableInfo(string schema, string name, IEnumerable<string> columnNames, long? estimatedRowCount = null)
        : base(schema, name)
    {
        Columns = columnNames.Select(columnName => new ColumnRef(this, columnName))
            .ToImmutableArray();

        EstimatedTableRows = estimatedRowCount;
    }

    /// <summary>
    /// Gets the columns.
    /// </summary>
    public ImmutableArray<ColumnRef> Columns { get; }

    /// <summary>
    /// Gets the estimated number of rows in the table (if fetched).
    /// </summary>
    public long? EstimatedTableRows { get; }
}
