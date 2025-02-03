using Altinn.Authorization.Cli.Database.Metadata;
using CommunityToolkit.Diagnostics;
using Spectre.Console;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Altinn.Authorization.Cli.Database.Prompt;

/// <summary>
/// A prompt for selecting schema items.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SchemaItemPrompt
    : IPrompt<SelectedSchemaItems>
{
    private readonly static Func<TableGraph.Node, string> _defaultFormatTable
        = table =>
        {
            var sb = new StringBuilder(table.Name);
            sb.Append(" [[");

            var first = true;
            foreach (var col in table.Columns)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append("[yellow]").Append(col.Name).Append("[/]");
            }

            sb.Append("]]");

            return sb.ToString();
        };

    private readonly static Func<SequenceInfo, string> _defaultFormatSequence
        = seq => seq.Name;

    private readonly static Func<GroupType, string> _defaultFormatGroupType
        = type => type switch
        {
            GroupType.Tables => "Tables",
            GroupType.Sequences => "Sequences",
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(type), $"Invalid group type: {type}"),
        };

    private readonly SchemaInfo _schema;
    private readonly MultiSelectionPrompt<SelectionItem> _inner = new MultiSelectionPrompt<SelectionItem>()
        .NotRequired()
        .PageSize(20)
        .MoreChoicesText("[grey](Move up and down to reveal more items)[/]")
        .InstructionsText("[grey](Press [blue]<space>[/] to toggle an item, [green]<enter>[/] to accept)[/]");

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaItemPrompt"/> class.
    /// </summary>
    /// <param name="schema">The <see cref="SchemaInfo"/> to select items from.</param>
    public SchemaItemPrompt(SchemaInfo schema)
    {
        _schema = schema;
    }

    /// <inheritdoc cref="MultiSelectionPrompt{T}.Title"/>
    public string? Title {
        get => _inner.Title;
        set => _inner.Title = value;
    }

    /// <inheritdoc cref="MultiSelectionPrompt{T}.PageSize"/>
    public string? MoreChoicesText 
    {
        get => _inner.MoreChoicesText;
        set => _inner.MoreChoicesText = value;
    }

    /// <inheritdoc cref="MultiSelectionPrompt{T}.InstructionsText"/>
    public string? InstructionsText
    {
        get => _inner.InstructionsText;
        set => _inner.InstructionsText = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show sequences.
    /// </summary>
    public bool ShowTables { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show sequences.
    /// </summary>
    public bool ShowSequences { get; set; } = true;

    /// <summary>
    /// Gets or sets the function used to format tables.
    /// </summary>
    public Func<TableGraph.Node, string> TableConverter { get; set; } = _defaultFormatTable;

    /// <summary>
    /// Gets or sets the function used to format sequences.
    /// </summary>
    public Func<SequenceInfo, string> SequenceConverter { get; set; } = _defaultFormatSequence;

    /// <summary>
    /// Gets or sets the function used to format group names.
    /// </summary>
    public Func<GroupType, string> GroupTypeConverter { get; set; } = _defaultFormatGroupType;

    /// <inheritdoc/>
    public SelectedSchemaItems Show(IAnsiConsole console)
    {
        PrepareSelect();
        var selected = _inner.Show(console);
        return PostSelect(selected);
    }

    /// <inheritdoc/>
    public async Task<SelectedSchemaItems> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        PrepareSelect();
        var selected = await _inner.ShowAsync(console, cancellationToken);
        return PostSelect(selected);
    }

    private void PrepareSelect()
    {
        _inner.Converter = item => item switch
        {
            GroupItem group => GroupTypeConverter(group.Type),
            TableItem table => TableConverter(table.Table),
            SequenceItem sequence => SequenceConverter(sequence.Sequence),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(item), $"Invalid item type: {item}"),
        };

        if (ShowTables)
        {
            _inner.AddChoices(new GroupItem(GroupType.Tables), group =>
            {
                group.Select();

                foreach (var table in _schema.Tables)
                {
                    var item = new TableItem(table);
                    group.AddChild(item);
                    _inner.Select(item);
                }
            });
        }

        if (ShowSequences)
        {
            _inner.AddChoices(new GroupItem(GroupType.Sequences), group =>
            {
                group.Select();
                foreach (var sequence in _schema.Sequences)
                {
                    var item = new SequenceItem(sequence);
                    group.AddChild(item);
                    _inner.Select(item);
                }
            });
        }
    }

    private SelectedSchemaItems PostSelect(List<SelectionItem> selected)
    {
        // Note: we need to retain the order in the SchemaInfo, which may not be preserved by the user's selection.
        var tables = _schema.Tables
            .Where(table => selected.OfType<TableItem>().Any(item => item.Table == table))
            .ToImmutableArray();

        var sequences = _schema.Sequences
            .Where(sequence => selected.OfType<SequenceItem>().Any(item => item.Sequence == sequence))
            .ToImmutableArray();

        return new SelectedSchemaItems(_schema, tables, sequences);
    }

    /// <summary>
    /// The type of group.
    /// </summary>
    public enum GroupType
    {
        /// <summary>
        /// Tables.
        /// </summary>
        Tables,

        /// <summary>
        /// Sequences.
        /// </summary>
        Sequences,
    }

    private abstract class SelectionItem()
    {
    }

    private sealed class GroupItem(GroupType type)
        : SelectionItem()
    {
        public GroupType Type => type;
    }

    private sealed class TableItem(TableGraph.Node table)
        : SelectionItem()
    {
        public TableGraph.Node Table => table;
    }

    private sealed class SequenceItem(SequenceInfo sequence)
        : SelectionItem()
    {
        public SequenceInfo Sequence => sequence;
    }
}
