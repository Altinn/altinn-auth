using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Cli.Database.Metadata;
using Altinn.Authorization.Cli.Database.Prompt;
using Altinn.Authorization.Cli.Utils;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Database;

/// <summary>
/// Exports all tables in a database schema.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExportDatabaseCommand(CancellationToken cancellationToken)
    : BaseCommand<ExportDatabaseCommand.Settings>(cancellationToken)
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var source = await DbHelper.Create(settings.SourceConnectionString!, cancellationToken)
            .LogOnFailure("[bold red]Failed to connect to the source database[/]");

        var schemaInfo = await source.GetSchemaInfo(settings.SchemaName!, cancellationToken)
            .LogOnFailure($"[bold red]Failed to get table graph for schema \"{settings.SchemaName}\"[/]");

        var selected = await new SchemaItemPrompt(schemaInfo)
            .Title("Select what to export")
            .ShowAsync(AnsiConsole.Console, cancellationToken);

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
                var index = 0;
                ProgressTask? sequenceTask = null;
                ProgressTask? clearTask = null;

                List<ExportTask> tasks = new(selected.Tables.Length);
                await source.BeginTransaction(cancellationToken);
                
                if (!settings.NoClear)
                {
                    clearTask = ctx.AddTask("clear output directory", autoStart: false, maxValue: 1);
                }

                foreach (var table in selected.Tables)
                {
                    if (table.EstimatedTableRows < 0)
                    {
                        continue;
                    }

                    var task = CreateExportTask(table, index++, ctx);
                    tasks.Add(task);
                }

                if (selected.Sequences.Length > 0)
                {
                    sequenceTask = ctx.AddTask("export sequences", autoStart: false, maxValue: 1);
                }

                if (!Directory.Exists(settings.OutputDirectory!.FullName))
                {
                    Directory.CreateDirectory(settings.OutputDirectory.FullName);
                }

                if (clearTask is not null)
                {
                    clearTask.StartTask();
                    
                    foreach (var subdir in settings.OutputDirectory.EnumerateDirectories())
                    {
                        subdir.Delete(recursive: true);
                    }

                    foreach (var file in settings.OutputDirectory.EnumerateFiles())
                    {
                        file.Delete();
                    }

                    clearTask.Value = clearTask.MaxValue;
                    clearTask.StopTask();
                }

                var copier = TextCopier.Create(mibibyteCount: 10);
                foreach (var task in tasks)
                {
                    var table = task.Table;
                    var colsString = string.Join(',', table.Columns.Select(c => $"\"{c.Name}\""));
                    var copyFrom = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) FROM STDIN (FORMAT csv)""";
                    var copyTo = /*strpsql*/$"""COPY "{table.Schema}"."{table.Name}" ({colsString}) TO STDOUT (FORMAT csv, FORCE_QUOTE *)""";

                    task.Progress.StartTask();
                    using var reader = await source.BeginTextExport(copyTo, cancellationToken);

                    await using var writer = File.CreateText(Path.Combine(settings.OutputDirectory!.FullName, $"{task.Index:D2}-{task.Table.Name}.asdn-v1"));
                    await writer.WriteLineAsync(copyFrom);
                    await copier.CopyLinesAsync(reader, writer, task.Progress.AsLineProgress(), cancellationToken);

                    task.Progress.Value = task.Progress.MaxValue;
                    task.Progress.StopTask();
                    ctx.Refresh();
                }

                if (selected.Sequences.Length > 0)
                {
                    sequenceTask!.StartTask();
                    await using var writer = File.CreateText(Path.Combine(settings.OutputDirectory!.FullName, $"{index++:D2}-sequences.sql"));

                    foreach (var seq in schemaInfo.Sequences)
                    {
                        writer.WriteLine(/*strpsql*/$"""SELECT setval('{seq.Schema}.{seq.Name}', {seq.Value}, {(seq.IsCalled ? "true" : "false")});""");
                    }

                    sequenceTask.Value = sequenceTask.MaxValue;
                    sequenceTask.StopTask();
                }

                ctx.Refresh();
            });

        await Task.Yield();
        return 0;
    }

    private ExportTask CreateExportTask(TableInfo table, int index, ProgressContext ctx)
    {
        var progressTask = ctx.AddTask($"export {table.Name}", autoStart: false, maxValue: table.EstimatedTableRows!.Value);

        return new ExportTask(progressTask, table, index);
    }

    private sealed record ExportTask(ProgressTask Progress, TableInfo Table, int Index);

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
        /// Gets the schema name to copy.
        /// </summary>
        [Description("The schema name to copy.")]
        [CommandArgument(1, "<SCHEMA_NAME>")]
        public string? SchemaName { get; init; }

        /// <summary>
        /// Gets the output directory to write the exported tables to.
        /// </summary>
        [CommandArgument(2, "<OUTPUT_DIRECTORY>")]
        public DirectoryInfo? OutputDirectory { get; init; }

        /// <summary>
        /// Gets a value indicating whether to clear the output directory before exporting.
        /// </summary>
        [Description("Don't clear the output directory before exporting.")]
        [CommandOption("-n|--no-clear")]
        public bool NoClear { get; init; }
    }
}
