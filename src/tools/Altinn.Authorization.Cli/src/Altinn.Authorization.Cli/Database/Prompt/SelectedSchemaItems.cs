using Altinn.Authorization.Cli.Database.Metadata;
using CommunityToolkit.Diagnostics;
using Spectre.Console;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Altinn.Authorization.Cli.Database.Prompt;

/// <summary>
/// A subset of schema items selected by the user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SelectedSchemaItems
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectedSchemaItems"/> class.
    /// </summary>
    /// <param name="schemaInfo">The <see cref="SchemaInfo"/>.</param>
    /// <param name="selectedTables">A set of selected tables.</param>
    /// <param name="selectedSequences">A set of selected sequences.</param>
    public SelectedSchemaItems(
        SchemaInfo schemaInfo,
        ImmutableArray<TableGraph.Node> selectedTables,
        ImmutableArray<SequenceInfo> selectedSequences)
    {
        Schema = schemaInfo;
        Tables = selectedTables;
        Sequences = selectedSequences;
    }

    /// <summary>
    /// Gets the schema.
    /// </summary>
    public SchemaInfo Schema { get; }

    /// <summary>
    /// Gets the selected tables.
    /// </summary>
    /// <remarks>
    /// The order is preserved from the <see cref="Schema"/>.
    /// </remarks>
    public ImmutableArray<TableGraph.Node> Tables { get; }

    /// <summary>
    /// Gets the selected sequences.
    /// </summary>
    /// <remarks>
    /// The order is preserved from the <see cref="Schema"/>.
    /// </remarks>
    public ImmutableArray<SequenceInfo> Sequences { get; }
}
