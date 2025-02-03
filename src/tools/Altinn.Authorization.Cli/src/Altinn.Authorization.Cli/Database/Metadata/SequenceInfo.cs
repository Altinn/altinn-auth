using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents information about a sequence.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("Sequence {Schema}.{Name}")]
public class SequenceInfo
    : SequenceRef
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceInfo"/> class.
    /// </summary>
    /// <param name="schema">The schema name.</param>
    /// <param name="name">The sequence name.</param>
    /// <param name="value">The current sequence value.</param>
    /// <param name="isCalled">The <c>is_called</c> flag on the sequence.</param>
    public SequenceInfo(string schema, string name, long value, bool isCalled)
        : base(schema, name)
    {
        Value = value;
        IsCalled = isCalled;
    }

    /// <summary>
    /// Gets the current sequence value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Gets the <c>is_called</c> flag on the sequence.
    /// </summary>
    public bool IsCalled { get; }
}
