using System.Security.Cryptography;
using Altinn.Authorization.DeployApi.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Npgsql;
using Spectre.Console;

namespace Altinn.Authorization.DeployApi.BootstrapDatabase;

internal sealed class CreateDatabaseRoleTask
    : StepTask<CreateDatabaseRoleTask.Result>
{
    private readonly SecretClient _secrets;
    private readonly NpgsqlConnection _conn;
    private readonly string _roleName;
    private readonly string _adminRole;

    public CreateDatabaseRoleTask(SecretClient secrets, NpgsqlConnection conn, string roleName, string adminRole)
    {
        _secrets = secrets;
        _conn = conn;
        _roleName = roleName;
        _adminRole = adminRole;
    }

    public override string Name => $"Creating role '[cyan]{_roleName}[/]'";

    public override async Task<Result> ExecuteAsync(ProgressTask task, CancellationToken cancellationToken)
    {
        var pw = await GetOrCreatePassword(task, cancellationToken);
        var secretName = $"-db-{_roleName.Replace('_', '-')}-pw";
        var secret = await _secrets.GetSecretAsync(secretName, cancellationToken: cancellationToken);

        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            /*strpsql*/$"""
            CREATE ROLE "{_roleName}"
            WITH LOGIN
            PASSWORD '{pw.Password}'
            """;

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateObject)
        {
            if (pw.Updated)
            {
                cmd.CommandText =
                    /*strpsql*/$"""
                    ALTER ROLE "{_roleName}"
                    WITH LOGIN
                    PASSWORD '{pw.Password}'
                    """;

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        cmd.CommandText =
            /*strpsql*/$"""
            GRANT "{_roleName}" TO "{_adminRole}"
            """;

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return new Result(_roleName, pw.Password);
    }

    private async Task<PasswordResult> GetOrCreatePassword(ProgressTask task, CancellationToken cancellationToken)
    {
        var secretName = $"-db-{_roleName.Replace('_', '-')}-pw";
        Response<KeyVaultSecret> secret;
        bool updated = false;
        try
        {
            secret = await _secrets.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            var pw = GenerateRandomPw();
            secret = await _secrets.SetSecretAsync(secretName, pw, cancellationToken: cancellationToken);
            updated = true;
        }

        return new PasswordResult(secret.Value.Value, updated);
    }

    private static string GenerateRandomPw()
    {
        const string VALID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+";
        return RandomNumberGenerator.GetString(VALID_CHARS.AsSpan(), 64);
    }

    private record PasswordResult(string Password, bool Updated);

    internal record Result(string RoleName, string Password);
}
