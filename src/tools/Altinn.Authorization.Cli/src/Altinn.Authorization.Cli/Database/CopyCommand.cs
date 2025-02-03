using System.Collections.Immutable;
using System.ComponentModel;
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
                await using var tmpDir = TempDir.Create(delete: false);
                var tableJobs = new List<TableJobs>();

                ProgressTask? truncTask = null;
                ProgressTask? seqTask = null;

                if (!settings.NoTruncate)
                {
                    truncTask = ctx.AddTask("Truncating target tables", autoStart: false, maxValue: tables.Length);
                }

                foreach (var table in tables)
                {
                    if (table.EstimatedTableRows < 0)
                    {
                        // skip tables that are unallocated on the source
                        continue;
                    }

                    var colsString = string.Join(',', table.Columns.Select(c => $"\"{c.Name}\""));
                    var copyFrom = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) FROM STDIN""";
                    var copyTo = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) TO STDOUT""";

                    var file = tmpDir.File($"{table.Name}.pgtxt");

                    tableJobs.Add(new()
                    {
                        Table = table,
                        ExportSql = copyTo,
                        ImportSql = copyFrom,
                        File = file,
                        ExportJob = ctx.AddTask($"""Export table "[yellow]{table.Name}[/]" """.TrimEnd(), autoStart: false, maxValue: table.EstimatedTableRows),
                        ImportJob = ctx.AddTask($"""Import table "[yellow]{table.Name}[/]" """.TrimEnd(), autoStart: false, maxValue: table.EstimatedTableRows),
                    });
                }

                if (sequences.Length > 0)
                {
                    seqTask = ctx.AddTask("Updating target sequences", autoStart: false, maxValue: sequences.Length);
                }

                var channelOptions = new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true,
                };

                var channel = Channel.CreateUnbounded<TableJobs>(channelOptions);
                var exportAllTask = ExportTables(source, tableJobs, channel.Writer, cancellationToken);

                await target.BeginTransaction(cancellationToken);
                if (truncTask is not null)
                {
                    await TruncateTables(target, tables, truncTask, cancellationToken);
                }

                // import all tables (as they become available)
                await ImportTables(target, channel.Reader, cancellationToken);
                await exportAllTask.WaitAsync(cancellationToken);
                ctx.Refresh();

                if (seqTask is not null)
                {
                    seqTask.StartTask();
                    foreach (var seq in sequences)
                    {
                        await UpdateSequence(target, seq, cancellationToken)
                            .LogOnFailure($"[bold red]Failed to update sequence \"{seq.Schema}\".\"{seq.Name}\"[/]");
                        seqTask.Increment(1);
                    }

                    seqTask.Value = seqTask.MaxValue;
                    seqTask.StopTask();
                }

                await target.Commit(cancellationToken);
                ctx.Refresh();
            });

        return 0;
    }

    private async Task TruncateTables(DbHelper db, ImmutableArray<TableGraph.Node> tables, ProgressTask ctx, CancellationToken cancellationToken)
    {
        ctx.StartTask();

        foreach (var table in tables.AsEnumerable().Reverse())
        {
            await TruncateTable(db, table, cancellationToken)
                .LogOnFailure($"[bold red]Failed to truncate table \"{table.Schema}\".\"{table.Name}\"[/]");
            
            ctx.Increment(1);
        }

        ctx.Value = ctx.MaxValue;
        ctx.StopTask();
    }

    private async Task ExportTables(
        DbHelper db,
        List<TableJobs> tableJobs,
        ChannelWriter<TableJobs> writer,
        CancellationToken cancellationToken)
    {
        var copier = TextCopier.Create(mibibyteCount: 10);

        foreach (var job in tableJobs)
        {
            job.ExportJob.StartTask();

            try
            {
                using var reader = await db.BeginTextExport(job.ExportSql, cancellationToken);
                await using var streamWriter = job.File.CreateText();

                var progress = job.ExportJob.AsLineProgress();
                await copier.CopyLinesAsync(reader, streamWriter, progress, cancellationToken);

                // we now know the actual line-count
                job.ImportJob.MaxValue = progress.RealCount;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to export table \"{job.Table.Schema}\".\"{job.Table.Name}\"", e);
            }

            job.ExportJob.Value = job.ExportJob.MaxValue;
            job.ExportJob.StopTask();

            await writer.WriteAsync(job, cancellationToken);
        }

        writer.Complete();
    }

    private async Task ImportTables(
        DbHelper db,
        ChannelReader<TableJobs> reader,
        CancellationToken cancellationToken)
    {
        var copier = TextCopier.Create(mibibyteCount: 10);

        await foreach (var job in reader.ReadAllAsync(cancellationToken))
        {
            job.ImportJob.StartTask();

            try
            {
                using var fileReader = job.File.OpenText();
                await using var writer = await db.BeginTextImport(job.ImportSql, cancellationToken);

                await copier.CopyLinesAsync(fileReader, writer, job.ImportJob.AsLineProgress(), cancellationToken);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to import table \"{job.Table.Schema}\".\"{job.Table.Name}\"", e);
            }

            job.ImportJob.Value = job.ImportJob.MaxValue;
            job.ImportJob.StopTask();
        }
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

    private record TableJobs
    {
        public required TableGraph.Node Table { get; init; }
        
        public required string ImportSql { get; init; }

        public required string ExportSql { get; init; }

        public required FileInfo File { get; init; }

        public required ProgressTask ExportJob { get; init; }

        public required ProgressTask ImportJob { get; init; }
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
