using Altinn.Authorization.DeployApi.Tasks;
using Npgsql;
using Spectre.Console;

namespace Altinn.Authorization.DeployApi.BootstrapDatabase;

internal sealed class CreateDatabaseTask
    : StepTask
{
    private readonly NpgsqlConnection _conn;
    private readonly string _databaseName;

    public CreateDatabaseTask(NpgsqlConnection conn, string databaseName)
    {
        _conn = conn;
        _databaseName = databaseName;
    }

    public override string Name => $"Creating database '[cyan]{_databaseName}[/]'";

    public override async Task ExecuteAsync(ProgressTask task, CancellationToken cancellationToken)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            /*strpsql*/$"""
            CREATE DATABASE "{_databaseName}"
            """;
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateDatabase)
        {
            // Database already exists
        }
    }
}
