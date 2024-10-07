using Altinn.Authorization.DeployApi.Tasks;
using Npgsql;
using Spectre.Console;

namespace Altinn.Authorization.DeployApi.BootstrapDatabase;

internal sealed class GrantDatabasePrivilegesTask
    : StepTask
{
    private readonly NpgsqlConnection _conn;
    private readonly string _databaseName;
    private readonly string _roleName;
    private readonly string _privileges;

    public GrantDatabasePrivilegesTask(NpgsqlConnection conn, string databaseName, string roleName, string privileges)
    {
        _conn = conn;
        _databaseName = databaseName;
        _roleName = roleName;
        _privileges = privileges;
    }

    public override string Name => $"Granting db privileges [green]{_privileges}[/] to role '[cyan]{_roleName}[/]'";

    public override async Task ExecuteAsync(ProgressTask task, CancellationToken cancellationToken)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            /*strpsql*/$"""
            GRANT {_privileges} ON DATABASE "{_databaseName}" TO "{_roleName}"
            """;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
