using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents a reference to a table.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("Table {Schema}.{Name}")]
public class TableRef(string schema, string name)
    : DbObjectRef(schema, name)
{
}
