using Altinn.Authorization.DeployApi.Tasks;
using Npgsql;
using Spectre.Console;

namespace Altinn.Authorization.DeployApi.BootstrapDatabase;

internal sealed class CreateDatabaseSchemaTask
    : StepTask
{
    private readonly NpgsqlConnection _conn;
    private readonly string _ownerRoleName;
    private readonly string _schemaName;
    private readonly BootstrapDatabasePipeline.SchemaBootstrapModel _schemaCfg;

    public CreateDatabaseSchemaTask(NpgsqlConnection conn, string ownerRoleName, string schemaName, BootstrapDatabasePipeline.SchemaBootstrapModel schemaCfg)
    {
        _conn = conn;
        _ownerRoleName = ownerRoleName;
        _schemaName = schemaName;
        _schemaCfg = schemaCfg;
    }

    public override string Name => $"Creating schema '[cyan]{_schemaName}[/]'";

    public override async Task ExecuteAsync(ProgressTask task, CancellationToken cancellationToken)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            /*strpsql*/$"""
            CREATE SCHEMA "{_schemaName}"
            AUTHORIZATION "{_ownerRoleName}"
            """;

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateSchema)
        {
            // Schema already exists
        }
    }
}
