using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Altinn.Authorization.Cli.Database;
using Altinn.Authorization.Cli.Database.Utils;
using Altinn.Authorization.Cli.Utils;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace Altinn.Authorization.Cli.Register;

/// <summary>
/// Command for getting external role information from register-db.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExternalRolesCommand(CancellationToken ct)
    : BaseCommand<ExternalRolesCommand.Settings>(ct)
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var db = await DbHelper.Create(settings.ConnectionString!, cancellationToken);
        await using var cmd = db.CreateCommand(
            /*strpsql*/"""
            SELECT 
                erd.source,
                erd.identifier,
                erd."name",
                erd.description,
                erd.code
            FROM
                register.external_role_definition erd
            """);

        IOutputBuilder builder = settings.Format switch {
            OutputFormat.TABLE => new TableOutputBuilder(),
            OutputFormat.SQL => new SqlBuilder(),
            OutputFormat.HTML => new HtmlBuilder(),
            OutputFormat.MD => new MarkdownBuilder(),
            _ => throw new InvalidOperationException("Unknown output format.")
        };

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var sourceOrdinal = reader.GetOrdinal("source");
        var identifierOrdinal = reader.GetOrdinal("identifier");
        var nameOrdinal = reader.GetOrdinal("name");
        var descriptionOrdinal = reader.GetOrdinal("description");
        var codeOrdinal = reader.GetOrdinal("code");

        while (await reader.ReadAsync(cancellationToken))
        {
            var source = await reader.GetFieldValueAsync<string>(sourceOrdinal, cancellationToken);
            var identifier = await reader.GetFieldValueAsync<string>(identifierOrdinal, cancellationToken);
            var name = await reader.GetFieldValueAsync<Dictionary<string, string>>(nameOrdinal, cancellationToken);
            var description = await reader.GetFieldValueAsync<Dictionary<string, string>>(descriptionOrdinal, cancellationToken);
            var code = await reader.GetNullableFieldValueAsync<string>(codeOrdinal, cancellationToken);

            builder.AddRole(
                source,
                identifier,
                name["nb"],
                description["nb"],
                code ?? string.Empty);
        }

        builder.Render();
        return 0;
    }

    private interface IOutputBuilder
    {
        void AddRole(string source, string identifier, string name, string description, string? code);

        void Render();
    }

    private class TableOutputBuilder
        : IOutputBuilder
    {
        private readonly Table _table;

        public TableOutputBuilder()
        {
            _table = new Table();
            _table.AddColumn(new TableColumn("Source").RightAligned());
            _table.AddColumn("Identifier");
            _table.AddColumn("Name");
            _table.AddColumn("Description");
            _table.AddColumn("Code");
        }

        public void AddRole(string source, string identifier, string name, string description, string? code)
        {
            _table.AddRow(
                source,
                identifier,
                name,
                description,
                code ?? string.Empty);
        }

        public void Render()
        {
            AnsiConsole.Write(_table);
        }
    }

    private class SqlBuilder
        : IOutputBuilder
    {
        private readonly StringBuilder _builder = new();

        public void AddRole(string source, string identifier, string name, string description, string? code)
        {
            if (_builder.Length > 0)
            {
                _builder.AppendLine();
            }

            _builder.AppendLine(
                /*strpsql*/$"""
                -- {name}
                UPDATE register.external_role_definition erd
                SET
                WHERE erd.source = '{source}' AND erd.identifier = '{identifier}';
                """);
        }

        public void Render()
        {
            Console.WriteLine(_builder.ToString());
        }
    }

    private class HtmlBuilder
        : IOutputBuilder
    {
        private readonly StringBuilder _builder;

        public HtmlBuilder()
        {
            _builder = new();
            _builder.AppendLine("<table>");
            _builder.AppendLine("  <thead>");
            _builder.AppendLine("    <tr>");
            _builder.AppendLine("      <td>Source</td>");
            _builder.AppendLine("      <td>Identifier</td>");
            _builder.AppendLine("      <td>Name</td>");
            _builder.AppendLine("      <td>Description</td>");
            _builder.AppendLine("      <td>Code</td>");
            _builder.AppendLine("      <td>Urn</td>");
            _builder.AppendLine("    </tr>");
            _builder.AppendLine("  </thead>");
            _builder.AppendLine("  <tbody>");
        }

        public void AddRole(string source, string identifier, string name, string description, string? code)
        {
            _builder.AppendLine("    <tr>");
            _builder.AppendLine($"      <td style=\"text-align: right;\">{source}</td>");
            _builder.AppendLine($"      <td>{identifier}</td>");
            _builder.AppendLine($"      <td>{name}</td>");
            _builder.AppendLine($"      <td>{description}</td>");
            _builder.AppendLine($"      <td>{code ?? string.Empty}</td>");
            _builder.AppendLine($"      <td><code style=\"white-space: nowrap;\">urn:altinn:external-role:{source}:{identifier}</code></td>");
            _builder.AppendLine("    </tr>");
        }

        public void Render()
        {
            _builder.AppendLine("  </tbody>");
            _builder.AppendLine("</table>");
            Console.WriteLine(_builder.ToString());
        }
    }

    private class MarkdownBuilder
        : IOutputBuilder
    {
        private readonly StringBuilder _builder;

        public MarkdownBuilder()
        {
            _builder = new();
            _builder.AppendLine(
                """
                | Source | Identifier | Name | Description | Code | Urn |
                | -----: | :--------- | ---- | ----------- | ---- | --- |
                """);
        }

        public void AddRole(string source, string identifier, string name, string description, string? code)
        {
            _builder.AppendLine(
                $"""
                | {source} | {identifier} | {name} | {description} | {code ?? string.Empty} | `urn:altinn:external-role:{source}:{identifier}` |
                """);
        }

        public void Render()
        {
            Console.WriteLine(_builder.ToString());
        }
    }

    /// <summary>
    /// Roles output format.
    /// </summary>
    public enum OutputFormat
    {
        /// <summary>
        /// Output as a table.
        /// </summary>
        TABLE = default,

        /// <summary>
        /// Output as SQL update statements.
        /// </summary>
        SQL,

        /// <summary>
        /// Output as HTML table.
        /// </summary>
        HTML,

        /// <summary>
        /// Output as GitHub flavored markdown.
        /// </summary>
        MD,
    }

    /// <summary>
    /// Settings for the retry command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        /// <summary>
        /// Gets the connection string to the source database.
        /// </summary>
        [Description("The connection string to the database.")]
        [CommandArgument(0, "<CONNECTION_STRING>")]
        [ExpandEnvironmentVariables]
        public string? ConnectionString { get; init; }

        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        [Description("The output format.")]
        [CommandOption("-o|--output <TABLE|SQL|HTML|MD>")]
        [DefaultValue(OutputFormat.TABLE)]
        public OutputFormat Format { get; init; }
    }
}
