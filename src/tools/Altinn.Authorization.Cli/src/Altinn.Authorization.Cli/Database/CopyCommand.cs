using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Cli.Database.Metadata;
using Altinn.Authorization.Cli.Database.Prompt;
using Altinn.Authorization.Cli.Utils;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Database;

/// <summary>
/// Command for copying a schema from one database to another.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CopyCommand(CancellationToken cancellationToken)
    : BaseCommand<CopyCommand.Settings>(cancellationToken)
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var source = await DbHelper.Create(settings.SourceConnectionString!, cancellationToken)
            .LogOnFailure("[bold red]Failed to connect to the source database[/]");

        await using var target = await DbHelper.Create(settings.TargetConnectionString!, cancellationToken)
            .LogOnFailure("[bold red]Failed to connect to the target database[/]");
        
        var schemaInfo = await source.GetSchemaInfo(settings.SchemaName!, cancellationToken)
            .LogOnFailure($"[bold red]Failed to get table graph for schema \"{settings.SchemaName}\"[/]");

        var selected = await new SchemaItemPrompt(schemaInfo)
            .Title("Select what to copy")
            .ShowAsync(AnsiConsole.Console, cancellationToken);

        // we need to keep the ordering from the graph
        var tables = selected.Tables;
        var sequences = selected.Sequences;

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
            ])
            .StartAsync(async (ctx) =>
            {
                await target.BeginTransaction(cancellationToken);
                if (!settings.NoTruncate)
                {
                    var truncTask = ctx.AddTask("Truncating target tables", autoStart: true, maxValue: tables.Length);

                    foreach (var table in tables.AsEnumerable().Reverse())
                    {
                        await TruncateTable(target, table, cancellationToken)
                            .LogOnFailure($"[bold red]Failed to truncate table \"{table.Schema}\".\"{table.Name}\"[/]");
                        truncTask.Increment(1);
                        ctx.Refresh();
                    }

                    truncTask.Value = truncTask.MaxValue;
                    truncTask.StopTask();
                    ctx.Refresh();
                }

                var copier = TextCopier.Create(mibibyteCount: 10);
                foreach (var table in tables)
                {
                    if (table.EstimatedTableRows < 0)
                    {
                        // skip tables that are unallocated on the source
                        continue;
                    }

                    var copyTask = ctx.AddTask($"""Copy table "[yellow]{table.Name}[/]" """.TrimEnd(), autoStart: true, maxValue: table.EstimatedTableRows);
                    var colsString = string.Join(',', table.Columns.Select(c => $"\"{c.Name}\""));
                    var copyFrom = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) FROM STDIN""";
                    var copyTo = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) TO STDOUT""";

                    using var reader = await source.BeginTextExport(copyTo, cancellationToken);
                    await using var writer = await target.BeginTextImport(copyFrom, cancellationToken);
                    await copier.CopyLinesAsync(reader, writer, copyTask.AsLineProgress(), cancellationToken);

                    copyTask.Value = copyTask.MaxValue;
                    copyTask.StopTask();
                    ctx.Refresh();
                }

                if (sequences.Length > 0)
                {
                    var seqTask = ctx.AddTask("Updating target sequences", autoStart: true, maxValue: sequences.Length);
                    foreach (var seq in sequences)
                    {
                        await UpdateSequence(target, seq, cancellationToken)
                            .LogOnFailure($"[bold red]Failed to update sequence \"{seq.Schema}\".\"{seq.Name}\"[/]");
                        seqTask.Increment(1);
                        ctx.Refresh();
                    }

                    seqTask.Value = seqTask.MaxValue;
                    seqTask.StopTask();
                    ctx.Refresh();
                }

                await target.Commit(cancellationToken);
            });

        return 0;
    }

    private async Task TruncateTable(DbHelper db, TableRef table, CancellationToken cancellationToken)
    {
        await using var cmd = db.CreateCommand(/*strpsql*/$"""TRUNCATE "{table.Schema}"."{table.Name}" RESTART IDENTITY CASCADE""");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateSequence(DbHelper db, SequenceInfo seq, CancellationToken cancellationToken)
    {
        await using var cmd = db.CreateCommand(/*strpsql*/$"""SELECT setval('{seq.Schema}.{seq.Name}', {seq.Value}, {(seq.IsCalled ? "true" : "false")})""");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Settings for the export database command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        /// <summary>
        /// Gets the connection string to the source database.
        /// </summary>
        [Description("The connection string to the source database.")]
        [CommandArgument(0, "<SOURCE_CONNECTION_STRING>")]
        [ExpandEnvironmentVariables]
        public string? SourceConnectionString { get; init; }

        /// <summary>
        /// Gets the connection string to the target database.
        /// </summary>
        [Description("The connection string to the target database.")]
        [CommandArgument(1, "<TARGET_CONNECTION_STRING>")]
        [ExpandEnvironmentVariables]
        public string? TargetConnectionString { get; init; }

        /// <summary>
        /// Gets the schema name to copy.
        /// </summary>
        [Description("The schema name to copy.")]
        [CommandArgument(2, "<SCHEMA_NAME>")]
        public string? SchemaName { get; init; }

        /// <summary>
        /// Gets a value indicating whether to truncate the target tables before copying.
        /// </summary>
        [Description("Don't truncate the target tables before copying.")]
        [CommandOption("-n|--no-truncate")]
        public bool NoTruncate { get; init; }
    }
}
