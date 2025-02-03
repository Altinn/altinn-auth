using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents a reference to a sequence.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("Sequence {Schema}.{Name}")]
public class SequenceRef(string schema, string name)
    : DbObjectRef(schema, name)
{
}
