using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.Authorization.Cli.Config;
using Altinn.Authorization.Cli.Utils;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.PostgreSql.FlexibleServers;
using Azure.Security.KeyVault.Secrets;
using Npgsql;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Database;

/// <summary>
/// Command for bootstrapping a Postgres Flex server.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BootstapCommand(CancellationToken cancellationToken)
    : BaseCommand<BootstapCommand.Settings>(cancellationToken)
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Status().AutoRefresh(true).Spinner(Spinner.Known.Default).StartAsync("[yellow]Bootstrapping Postgres Flex server[/]", async ctx =>
        {
            var tokenOpts = new DefaultAzureCredentialOptions();
            if (!string.IsNullOrEmpty(settings.TenantId))
            {
                tokenOpts.TenantId = settings.TenantId;
            }

            var token = new DefaultAzureCredential(tokenOpts);
            var postgresArm = new ArmClient(token, settings.ServerSubscriptionId);
            var keyVaultArm = new ArmClient(token, settings.KeyVaultSubscriptionId);

            var content = await File.ReadAllTextAsync(settings.ConfigFile.FullName);

            // TODO: validate
            var config = JsonSerializer.Deserialize<AppsConfig>(content)!;

            var postgresResource = await GetPostgresFlexibleServerResource(postgresArm, settings, cancellationToken);
            var keyVaultResource = await GetArmKeyVaultResource(keyVaultArm, settings, cancellationToken);
            var secretClient = new SecretClient(keyVaultResource.Data.Properties.VaultUri, token);
            await CreateDatabase(settings, token, config, postgresResource, cancellationToken);

            var connectionString = await CreateAdminConnectionString(token, postgresResource, settings, config.Database.Name, cancellationToken);
            await using var conn = new NpgsqlConnection(connectionString.ToString());
            await conn.OpenAsync(cancellationToken);
            WriteOperationSucceeded($"Connected to database '{config.Database.Name}'");

            var migratorUser = await CreateDatabaseRole(conn, secretClient, $"{config.Database.Prefix}_migrator", connectionString.Username!, settings, cancellationToken);
            await GrantAzurePgAdmin(conn, migratorUser.RoleName, cancellationToken);
            var appUser = await CreateDatabaseRole(conn, secretClient, $"{config.Database.Prefix}_app", connectionString.Username!, settings, cancellationToken);
            await GrantDatabasePrivileges(conn, config.Database.Name, migratorUser.RoleName, "CREATE, CONNECT", cancellationToken);
            await GrantDatabasePrivileges(conn, config.Database.Name, appUser.RoleName, "CONNECT", cancellationToken);

            foreach (var (schemaName, schemaCfg) in config.Database.Schemas)
            {
                await CreateDatabaseSchema(conn, migratorUser, schemaName, schemaCfg, cancellationToken);
                await GrantUsageOnSchema(conn, appUser, schemaName, cancellationToken);
            }

            await StoreConnectionString(config, migratorUser, postgresResource.Data.FullyQualifiedDomainName, secretClient, settings, cancellationToken);
            await StoreConnectionString(config, appUser, postgresResource.Data.FullyQualifiedDomainName, secretClient, settings, cancellationToken);
        });

        WriteOperationSucceeded("[bold green]Bootstrap completed successfully![/]");

        return 0;
    }

    private async Task CreateDatabase(Settings settings, DefaultAzureCredential token, AppsConfig config, PostgreSqlFlexibleServerResource postgresResource, CancellationToken cancellationToken)
    {
        var connectionString = await CreateAdminConnectionString(token, postgresResource, settings, "postgres", cancellationToken);
        await using var conn = new NpgsqlConnection(connectionString.ToString());
        await conn.OpenAsync(cancellationToken);
        await CreateDatabase(conn, config.Database.Name, cancellationToken);
    }

    private async Task<PostgreSqlFlexibleServerResource> GetPostgresFlexibleServerResource(ArmClient arm, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var serverSubscription = await arm.GetDefaultSubscriptionAsync(cancellationToken);
            var serverResourceGroup = await serverSubscription.GetResourceGroupAsync(settings.ServerResourceGroup, cancellationToken);
            var pg = await serverResourceGroup.Value.GetPostgreSqlFlexibleServerAsync(settings.ServerName, cancellationToken);
            WriteOperationSucceeded("Fetched azure postgreSQL flexible server.");
            return pg;
        }
        catch
        {
            WriteOperationFailed("Fetched azure postgreSQL flexible server.");
            throw;
        }
    }

    private async Task<KeyVaultResource> GetArmKeyVaultResource(ArmClient arm, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var serverSubscription = await arm.GetDefaultSubscriptionAsync(cancellationToken);
            var serverResourceGroup = await serverSubscription.GetResourceGroupAsync(settings.KeyVaultResourceGroup, cancellationToken);
            var keyVault = await serverResourceGroup.Value.GetKeyVaultAsync(settings.KeyVaultName, cancellationToken);
            WriteOperationSucceeded("Fetched azure key vault.");
            return keyVault;
        }
        catch
        {
            WriteOperationFailed("Fetched azure key vault.");
            throw;
        }
    }

    private async Task<NpgsqlConnectionStringBuilder> CreateAdminConnectionString(TokenCredential token, PostgreSqlFlexibleServerResource postgres, Settings settings, string database, CancellationToken cancellationToken)
    {
        try
        {
            var cred = await token.GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), cancellationToken);
            var username = settings.PrincipalName;
            if (string.IsNullOrEmpty(username))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(cred.Token);
                username = jwtToken.Claims.First(claim => claim.Type == "unique_name" || claim.Type == "azp_name").Value;
            }

            var connectionString = new NpgsqlConnectionStringBuilder()
            {
                Host = postgres.Data.FullyQualifiedDomainName,
                Database = database,
                Username = username,
                Password = cred.Token,
                Port = 5432,
                SslMode = SslMode.Require,
                Pooling = false,
            };

            WriteOperationSucceeded("Create admin connection string for postgres server.");
            return connectionString;
        }
        catch
        {
            WriteOperationFailed("Create admin connection string for postgres server.");
            throw;
        }
    }

    private async Task<Result> CreateDatabaseRole(NpgsqlConnection conn, SecretClient secretClient, string roleName, string user, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var pw = await GetOrCreatePassword(secretClient, roleName, settings, cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = /*strpsql*/
            $"""
            CREATE ROLE "{roleName}"
            WITH LOGIN
            PASSWORD '{pw.Password}'
            """;

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                WriteOperationSucceeded($"""Created role "{roleName}".""");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateObject)
            {
                if (pw.Updated)
                {
                    cmd.CommandText = /*strpsql*/
                    $"""
                    ALTER ROLE "{roleName}"
                    WITH LOGIN
                    PASSWORD '{pw.Password}'
                    """;

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    WriteOperationSucceeded($"""Updated role "{roleName}".""");
                }
            }

            cmd.CommandText = /*strpsql*/
            $"""
            GRANT "{roleName}" TO "{user}"
            """;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            WriteOperationSucceeded($"""Role creation "{roleName}" and assigment to "{user}" upserted successfully.""");
            var result = new Result(roleName, pw.Password);

            return result;
        }
        catch
        {
            WriteOperationFailed($"""Role Creation "{roleName}" and assigment to "{user}" failed.""");
            throw;
        }
    }

    private async Task GrantAzurePgAdmin(NpgsqlConnection conn, string roleName, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = /*strpsql*/
            $""" 
            GRANT "azure_pg_admin" TO "{roleName}"
            """;

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            WriteOperationSucceeded($"Granted 'azure_pg_admin' to role '{roleName}'.");
        }
        catch
        {
            WriteOperationFailed($"Grant 'azure_pg_admin' to role '{roleName}'.");
            throw;
        }
    }

    private async Task CreateDatabase(NpgsqlConnection conn, string database, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = /*strpsql*/
        $"""
        CREATE DATABASE "{database}"
        """;

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            WriteOperationSucceeded($"Database '{database}' created.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateDatabase)
        {
            WriteOperationSucceeded($"Database '{database}' exists. Skipping creation.");
        }
        catch
        {
            WriteOperationFailed($"Database '{database}' creation failed.");
            throw;
        }
    }

    private async Task GrantDatabasePrivileges(NpgsqlConnection conn, string database, string role, string privileges, CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                /*strpsql*/$"""
                GRANT {privileges} ON DATABASE "{database}" TO "{role}"
                """;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            WriteOperationSucceeded($"Grant privileges '{privileges}' for role '{role}' in database.");
        }
        catch
        {
            WriteOperationFailed($"Grant privileges '{privileges}' for role '{role}' in database.");
            throw;
        }
    }

    private async Task CreateDatabaseSchema(NpgsqlConnection conn, Result migratorUser, string schemaName, JsonElement schemaCfg, CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                /*strpsql*/$"""
                CREATE SCHEMA "{schemaName}"
                AUTHORIZATION "{migratorUser.RoleName}"
                """;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            WriteOperationSucceeded($"Create schema '{schemaName}' in database.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateSchema)
        {
            WriteOperationSucceeded($"Schema '{schemaName}' already exists, skipping operation.");
        }
        catch
        {
            WriteOperationFailed($"Upsert schema '{schemaName}' in database.'{conn.Database}");
            throw;
        }
    }

    private async Task GrantUsageOnSchema(NpgsqlConnection conn, Result appUser, string schemaName, CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                /*strpsql*/$"""
                GRANT USAGE ON SCHEMA {schemaName} TO {appUser.RoleName};
                """;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            WriteOperationSucceeded($"Grant privileges on '{schemaName}' in database for {appUser.RoleName}.");
        }
        catch
        {
            WriteOperationFailed($"Grant privileges on '{schemaName}' in database for {appUser.RoleName}.");
            throw;
        }
    }

    private async Task StoreConnectionString(AppsConfig config, Result user, string serverUrl, SecretClient secretClient, Settings settings, CancellationToken cancellationToken)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder()
        {
            Host = serverUrl,
            Username = user.RoleName,
            Password = user.Password,
            Database = config.Database.Name,
            SslMode = SslMode.Require,
            Port = 5432,
            Pooling = true,
        };

        if (settings.UsePgbouncer)
        {
            connectionStringBuilder.NoResetOnClose = true;
            connectionStringBuilder.Port = 6432;
        }

        if (settings.MaxPoolSize.HasValue)
        {
            connectionStringBuilder.MaxPoolSize = settings.MaxPoolSize.Value;
        }

        var connectionString = connectionStringBuilder.ToString();

        var key = $"db-{settings.ServerName}-{user.RoleName.Replace("_", "-")}";

        try
        {
            var secret = await secretClient.GetSecretAsync(key, cancellationToken: cancellationToken);
            if (!string.Equals(connectionString, secret.Value.Value, StringComparison.Ordinal))
            {
                await secretClient.SetSecretAsync(key, connectionString, cancellationToken: cancellationToken);
                WriteOperationSucceeded($"Update connection string for role '{user.RoleName}' in key vault.");
            }
            else
            {
                WriteOperationSucceeded($"Same connection string already exists in key vault, skipping operation");
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            try
            {
                await secretClient.SetSecretAsync(key, connectionString, cancellationToken: cancellationToken);
                WriteOperationSucceeded($"Create key '{key}' in key vault containing connection string");
            }
            catch
            {
                WriteOperationFailed($"Create key '{key}' containing connection string to key vault.");
                throw;
            }
        }
        catch
        {
            WriteOperationFailed($"Update '{key}' containing connection string to key vault.");
            throw;
        }
    }

    private async Task<PasswordResult> GetOrCreatePassword(SecretClient secretClient, string roleName, Settings settings, CancellationToken cancellationToken)
    {
        var secretName = $"{settings.ServerName}-db-{roleName.Replace('_', '-')}-pw".ToLower();
        Response<KeyVaultSecret> secret;
        bool updated = false;
        try
        {
            secret = await secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
            var pw = GenerateRandomPw();
            secret = await secretClient.SetSecretAsync(secretName, pw, cancellationToken: cancellationToken);
            updated = true;
        }

        return new PasswordResult(secret.Value.Value, updated);
    }

    private static void WriteOperationFailed(string msg) =>
        AnsiConsole.MarkupLineInterpolated($"[grey]LOG:[/] [bold red]:cross_mark_button: {msg}[/]");

    private static void WriteOperationSucceeded(string msg) =>
        AnsiConsole.MarkupLineInterpolated($"[grey]LOG:[/] [bold green]:check_mark_button: {msg}[/]\n");

    private static string GenerateRandomPw()
    {
        const string VALID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+";
        return RandomNumberGenerator.GetString(VALID_CHARS.AsSpan(), 64);
    }

    private record PasswordResult(string Password, bool Updated);

    private record Result(string RoleName, string Password);

    /// <summary>
    /// Settings for the bootstrap command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        /// <summary>
        /// Principal name
        /// </summary>
        [CommandOption("--principal-name <PRINCIPAL_NAME>")]
        [Description("Principal name of signed in principal.")]
        [ExpandEnvironmentVariables]
        public string? PrincipalName { get; init; }

        /// <summary>
        /// Gets the tenant ID for the Azure AD application.
        /// </summary>
        [CommandOption("--tenant <TENANT_ID>")]
        [Description("Tenant ID for the Azure AD application.")]
        [ExpandEnvironmentVariables]
        public string? TenantId { get; init; }

        /// <summary>
        /// Gets the ID of subscription for the Postgres Flex server.
        /// </summary>
        [Required]
        [CommandOption("--server-subscription <SUBSCRIPTION_ID>")]
        [Description("ID of subscription for the Postgres Flex server.")]
        [ExpandEnvironmentVariables]
        public required string ServerSubscriptionId { get; init; }

        /// <summary>
        /// Gets the resource group for the Postgres Flex server.
        /// </summary>
        [Required]
        [CommandOption("--server-resource-group <RESOURCE_GROUP>")]
        [Description("Postgres Flex server's resource group.")]
        [ExpandEnvironmentVariables]
        public required string ServerResourceGroup { get; init; }

        /// <summary>
        /// Gets the name of the Postgres Flex server.
        /// </summary>
        [Required]
        [CommandOption("--server-name <SERVER_NAME>")]
        [Description("Name of the Postgres Flex server.")]
        [ExpandEnvironmentVariables]
        public required string ServerName { get; init; }

        /// <summary>
        /// Gets the ID of subscription for the Key Vault.
        /// </summary>
        [Required]
        [CommandOption("--kv-subscription <SUBSCRIPTION_ID>")]
        [Description("ID of subscription for the Key Vault.")]
        [ExpandEnvironmentVariables]
        public required string KeyVaultSubscriptionId { get; init; }

        /// <summary>
        /// Gets the resource group for the Key Vault.
        /// </summary>
        [Required]
        [CommandOption("--kv-resource-group <RESOURCE_GROUP>")]
        [Description("Key Vault's resource group.")]
        [ExpandEnvironmentVariables]
        public required string KeyVaultResourceGroup { get; init; }

        /// <summary>
        /// Gets the name of the Key Vault.
        /// </summary>
        [Required]
        [CommandOption("--kv-name <KEY_VAULT_NAME>")]
        [Description("Name of the Key Vault.")]
        [ExpandEnvironmentVariables]
        public required string KeyVaultName { get; init; }

        [CommandOption("--use-pgbouncer <USE_PGBOUNCER>")]
        [Description("Indicates that the connection should use the PgBouncer port.")]
        public bool UsePgbouncer { get; init; }

        /// <summary>
        /// Gets the maximum pool size for the database connection.
        /// </summary>
        [CommandOption("--max-pool-size <MAX_POOL_SIZE>")]
        [Description("Maximum pool size for the database connection.")]
        public int? MaxPoolSize { get; init; }

        /// <summary>
        /// Gets the path to conf.json for the app.
        /// </summary>
        [RequiredFileExists]
        [CommandArgument(0, "<CONFIG_FILE>")]
        [Description("Path to conf.json for app.")]
        public required FileInfo ConfigFile { get; init; }
    }
}
