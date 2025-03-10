using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
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

        using var dir = TempDir.Create(deleteOnDispose: !Debugger.IsAttached);
        AnsiConsole.MarkupLineInterpolated($"Temporary directory: [yellow]{dir.DirPath}[/]");

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
                var plan = CopyPlan.Plan(ctx, tables, sequences, settings);

                await target.BeginTransaction(cancellationToken);
                if (plan.Truncate is { } truncateTask)
                {
                    await TruncateTables(target, truncateTask, tables, cancellationToken);
                    ctx.Refresh();
                }

                await CopyTables(source, target, plan.Tables, dir, cancellationToken);
                ctx.Refresh();

                if (plan.Sequences is { } sequencesTask)
                {
                    await UpdateSequences(target, sequencesTask, sequences, cancellationToken);
                    ctx.Refresh();
                }

                await target.Commit(cancellationToken);
            });

        dir.Delete();
        return 0;
    }

    private async Task CopyTables(DbHelper source, DbHelper target, ImmutableArray<TableTask> tables, TempDir dir, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TableTask>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true,
        });

        var exporterTask = Task.Run(() => ExportTables(source, tables, channel.Writer, dir, cancellationToken), cancellationToken);
        var importerTask = Task.Run(() => ImportTables(target, channel.Reader, dir, cancellationToken), cancellationToken);

        await Task.WhenAll(exporterTask, importerTask);

        static async Task ExportTables(DbHelper db, ImmutableArray<TableTask> tables, ChannelWriter<TableTask> writer, TempDir dir, CancellationToken cancellationToken)
        {
            var copier = TextCopier.Create(10);

            try
            {
                foreach (var table in tables)
                {
                    await ExportTable(db, table, copier, dir, cancellationToken);
                    await writer.WriteAsync(table, cancellationToken);
                }
            }
            catch (Exception e)
            {
                writer.TryComplete(e);
                throw;
            }
            finally
            {
                writer.TryComplete();
            }
        }

        static async Task ExportTable(DbHelper db, TableTask table, TextCopier copier, TempDir dir, CancellationToken cancellationToken)
        {
            using var exportTask = table.ExportTask.Run();
            using var reader = await db.BeginTextExport(table.ExportSql, cancellationToken);
            await using var writer = dir.CreateText(table.FileName);

            await copier.CopyLinesAsync(reader, writer, table, cancellationToken);
            table.ExportTask.MaxValue = table.ExportedRows;
            table.ExportTask.Value = table.ExportedRows;
        }

        static async Task ImportTables(DbHelper db, ChannelReader<TableTask> reader, TempDir dir, CancellationToken cancellationToken)
        {
            var copier = TextCopier.Create(10);

            await foreach (var task in reader.ReadAllAsync(cancellationToken))
            {
                await ImportTable(db, task, copier, dir, cancellationToken);
            }
        }

        static async Task ImportTable(DbHelper db, TableTask table, TextCopier copier, TempDir dir, CancellationToken cancellationToken)
        {
            table.ImportTask.MaxValue = table.ExportedRows;

            using var importTask = table.ImportTask.Run();
            using var reader = dir.OpenText(table.FileName);
            await using var writer = await db.BeginTextImport(table.ImportSql, cancellationToken);

            await copier.CopyLinesAsync(reader, writer, table.ImportTask.AsLineProgress(), cancellationToken);
        }
    }

    private async Task TruncateTables(DbHelper db, ProgressTask ctx, ImmutableArray<TableGraph.Node> tables, CancellationToken cancellationToken)
    {
        using var updateTask = ctx.Run();

        foreach (var table in tables.AsEnumerable().Reverse())
        {
            await TruncateTable(db, table, cancellationToken)
                .LogOnFailure($"[bold red]Failed to truncate table \"{table.Schema}\".\"{table.Name}\"[/]");
            ctx.Increment(1);
        }

        ctx.Value = ctx.MaxValue;
        ctx.StopTask();
    }

    private async Task TruncateTable(DbHelper db, TableRef table, CancellationToken cancellationToken)
    {
        await using var cmd = db.CreateCommand(/*strpsql*/$"""TRUNCATE "{table.Schema}"."{table.Name}" RESTART IDENTITY CASCADE""");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateSequences(DbHelper db, ProgressTask ctx, ImmutableArray<SequenceInfo> sequences, CancellationToken cancellationToken)
    {
        using var updateTask = ctx.Run();

        foreach (var seq in sequences)
        {
            await UpdateSequence(db, seq, cancellationToken)
                .LogOnFailure($"[bold red]Failed to update sequence \"{seq.Schema}\".\"{seq.Name}\"[/]");
            ctx.Increment(1);
        }

        ctx.Value = ctx.MaxValue;
    }

    private async Task UpdateSequence(DbHelper db, SequenceInfo seq, CancellationToken cancellationToken)
    {
        await using var cmd = db.CreateCommand(/*strpsql*/$"""SELECT setval('{seq.Schema}.{seq.Name}', {seq.Value}, {(seq.IsCalled ? "true" : "false")})""");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class CopyPlan
    {
        public static CopyPlan Plan(ProgressContext ctx, ImmutableArray<TableGraph.Node> tables, ImmutableArray<SequenceInfo> sequences, Settings settings)
        {
            ProgressTask? truncateTask = null;
            List<TableTask> tablePlans = new(tables.Length);
            ProgressTask? sequencesTask = null;

            if (!settings.NoTruncate)
            {
                truncateTask = ctx.AddTask("Truncating target tables", autoStart: false, maxValue: tables.Length);
            }

            var exportTasks = new List<(ProgressTask, TableGraph.Node)>(tables.Length);
            foreach (var table in tables)
            {
                if (table.EstimatedTableRows < 0)
                {
                    // skip tables that are unallocated on the source
                    continue;
                }

                var exportTask = ctx.AddTask($"""Export table "[yellow]{table.Name}[/]" """.TrimEnd(), autoStart: false, maxValue: table.EstimatedTableRows);
                exportTasks.Add((exportTask, table));
            }

            foreach (var (exportTask, table) in exportTasks)
            {
                var importTask = ctx.AddTask($"""Import table "[yellow]{table.Name}[/]" """.TrimEnd(), autoStart: false, maxValue: table.EstimatedTableRows);

                var colsString = string.Join(',', table.Columns.Select(c => $"\"{c.Name}\""));
                var copyFrom = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) FROM STDIN""";
                var copyTo = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) TO STDOUT""";
                var fileName = $"{table.Schema}.{table.Name}.txt";

                tablePlans.Add(new TableTask
                {
                    ExportTask = exportTask,
                    ExportSql = copyTo,
                    ImportTask = importTask,
                    ImportSql = copyFrom,
                    FileName = fileName,
                });
            }

            if (sequences.Length > 0)
            {
                sequencesTask = ctx.AddTask("Updating target sequences", autoStart: false, maxValue: sequences.Length);
            }

            return new CopyPlan
            {
                Truncate = truncateTask,
                Tables = tablePlans.ToImmutableArray(),
                Sequences = sequencesTask,
            };
        }

        public required ProgressTask? Truncate { get; init; }

        public required ImmutableArray<TableTask> Tables { get; init; }

        public required ProgressTask? Sequences { get; init; }
    }

    private sealed class TableTask
        : IProgress<int>
    {
        private int _exportedRows;

        public required ProgressTask ExportTask { get; init; }

        public required ProgressTask ImportTask { get; init; }

        public required string ExportSql { get; init; }

        public required string ImportSql { get; init; }

        public required string FileName { get; init; }

        public int ExportedRows => Volatile.Read(ref _exportedRows);

        void IProgress<int>.Report(int value)
        {
            Interlocked.Add(ref _exportedRows, value);
            ExportTask.Increment(value);
        }
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
