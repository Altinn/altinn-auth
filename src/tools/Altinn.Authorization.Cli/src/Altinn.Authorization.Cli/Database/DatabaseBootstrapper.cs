using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using Altinn.Authorization.Cli.Config;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.PostgreSql.FlexibleServers;
using Azure.Security.KeyVault.Secrets;
using Npgsql;
using Spectre.Console;

namespace Altinn.Authorization.Cli.Database;

/// <summary>
/// Handles the initialization of PostgreSQL databases, roles, and connection strings, 
/// storing credentials securely in Azure Key Vault.
/// </summary>
/// <param name="console">Console for displaying messages and progress.</param>
/// <param name="options">Configuration options for the bootstrapper.</param>
public class DatabaseBootstrapper(IAnsiConsole console, DatabaseBootstrapperOptions options)
{
    /// <summary>
    /// Console for displaying messages and progress.
    /// </summary>
    public IAnsiConsole Console { get; } = console;

    /// <summary>
    /// Configuration options for the bootstrapper.
    /// </summary>
    public DatabaseBootstrapperOptions Options { get; } = options;

    /// <summary>
    /// Runs the bootstrapping process to set up PostgreSQL resources.
    /// </summary>
    public async Task<int> Run()
    {
        try
        {
            var token = new DefaultAzureCredential();
            var postgresArm = new ArmClient(token, Options.ServerSubscription);
            var keyVaultArm = new ArmClient(token, Options.KeyVaultSubscription);

            if (!Options.ConfigFile.Exists)
            {
                throw new ArgumentException($"file {Options.ConfigFile.FullName} does not exist");
            }

            var content = await File.ReadAllTextAsync(Options.ConfigFile.FullName);
            var config = JsonSerializer.Deserialize<AppsConfig>(content);

            var postgresResource = await GetPostgresFlexibleServerResource(postgresArm);
            var keyVaultResource = await GetArmKeyVaultResource(keyVaultArm);
            var connectionString = await CreateAdminConnectionString(token, postgresResource);
            var secretClient = new SecretClient(keyVaultResource.Data.Properties.VaultUri, token);

            await using var conn = new NpgsqlConnection(connectionString.ToString());
            await conn.OpenAsync();

            var migratorUser = await CreateDatabaseRole(conn, secretClient, $"{config.Database.Prefix}_migrator", connectionString.Username);
            var appUser = await CreateDatabaseRole(conn, secretClient, $"{config.Database.Prefix}_app", connectionString.Username);
            await CreateDatabase(conn, config.Database.Name);
            await GrantDatabasePrivileges(conn, config.Database.Name, migratorUser.RoleName, "CREATE, CONNECT");
            await GrantDatabasePrivileges(conn, config.Database.Name, appUser.RoleName, "CONNECT");

            foreach (var (schemaName, schemaCfg) in config.Database.Schemas)
            {
                await CreateDatabaseSchema(conn, migratorUser, schemaName, schemaCfg);
            }

            await StoreConnectionString(config, migratorUser, postgresResource.Data.FullyQualifiedDomainName, secretClient);
            await StoreConnectionString(config, appUser, postgresResource.Data.FullyQualifiedDomainName, secretClient);
        }
        catch (Exception ex)
        {
            Console.WriteException(ex);
            return 1;
        }

        return 0;
    }

    private async Task StoreConnectionString(AppsConfig config, Result user, string serverUrl, SecretClient secretClient, CancellationToken cancellationToken = default)
    {
        var connectionString = new NpgsqlConnectionStringBuilder()
        {
            Username = user.RoleName,
            Password = user.Password,
            Host = serverUrl,
            Database = config.Database.Name,
            Port = 5432,
            SslMode = SslMode.Require,
        }.ToString();
        var key = $"db-{Options.ServerName}-{user.RoleName.Replace("_", "-")}";

        await Console.Status().StartAsync($"Upserting '{key}' containing connection string to key vault...", async ctx =>
        {
            try
            {
                var secret = await secretClient.GetSecretAsync(key, cancellationToken: cancellationToken);
                if (string.Equals(connectionString, secret.Value.Value, StringComparison.Ordinal))
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
                await secretClient.SetSecretAsync(key, connectionString, cancellationToken: cancellationToken);
                WriteOperationSucceeded($"Create key '{key}' in key vault containing connection string");
            }
            catch
            {
                WriteOperationFailed($"Upsert '{key}' containing connection string to key vault.");
                throw;
            }
        });
    }

    private async Task GrantDatabasePrivileges(NpgsqlConnection conn, string database, string role, string privileges, CancellationToken cancellationToken = default)
    {
        await Console.Status().StartAsync($"Granting privileges '{privileges}' for role '{role}' in database...", async ctx =>
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
        });
    }

    private async Task CreateDatabaseSchema(NpgsqlConnection conn, Result migratorUser, string schemaName, object schemaCfg, CancellationToken cancellationToken = default)
    {
        await Console.Status().StartAsync($"Upserting schema '{schemaName}' in database...", async ctx =>
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
        });
    }

    private async Task<NpgsqlConnectionStringBuilder> CreateAdminConnectionString(TokenCredential token, PostgreSqlFlexibleServerResource postgres)
    {
        try
        {
            NpgsqlConnectionStringBuilder connectionString = null;
            await Console.Status().StartAsync("Creating admin connection string for postgres database...", async ctx =>
            {
                var cred = await token.GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), CancellationToken.None);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(cred.Token);
                var username = jwtToken.Claims.First(claim => claim.Type == "unique_name");

                connectionString = new NpgsqlConnectionStringBuilder()
                {
                    Host = postgres.Data.FullyQualifiedDomainName,
                    Database = "postgres",
                    Username = username.Value,
                    Password = cred.Token,
                    Port = 5432,
                    SslMode = SslMode.Require,
                    Pooling = false,
                };
            });

            WriteOperationSucceeded("Create admin connection string for postgres server.");
            return connectionString;
        }
        catch
        {
            WriteOperationFailed("Create admin connection string for postgres server.");
            throw;
        }
    }

    private async Task<KeyVaultResource> GetArmKeyVaultResource(ArmClient arm)
    {
        try
        {
            KeyVaultResource keyVault = null;
            await Console.Status().StartAsync("Fetching azure key vault...", async ctx =>
            {
                var serverSubscription = await arm.GetDefaultSubscriptionAsync();
                var serverResourceGroup = await serverSubscription.GetResourceGroupAsync(Options.KeyVaultResourceGroup);
                keyVault = await serverResourceGroup.Value.GetKeyVaultAsync(Options.KeyVaultName);
            });
            WriteOperationSucceeded("Fetched azure key vault.");
            return keyVault;
        }
        catch
        {
            WriteOperationFailed("Fetched azure key vault.");
            throw;
        }
    }

    private async Task<PostgreSqlFlexibleServerResource> GetPostgresFlexibleServerResource(ArmClient arm)
    {
        try
        {
            PostgreSqlFlexibleServerResource pg = null;
            await Console.Status().StartAsync("Fetching azure postgreSQL flexible server...", async ctx =>
            {
                var serverSubscription = await arm.GetDefaultSubscriptionAsync();
                var serverResourceGroup = await serverSubscription.GetResourceGroupAsync(Options.ServerResourceGroup);
                pg = await serverResourceGroup.Value.GetPostgreSqlFlexibleServerAsync(Options.ServerName);
            });
            WriteOperationSucceeded("Fetched azure postgreSQL flexible server.");
            return pg;
        }
        catch
        {
            WriteOperationFailed("Fetched azure postgreSQL flexible server.");
            throw;
        }
    }

    private async Task CreateDatabase(NpgsqlConnection conn, string database, CancellationToken cancellationToken = default)
    {
        await Console.Status().StartAsync($"Creating database '{database}'...", async ctx =>
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
        });
    }

    private async Task<Result> CreateDatabaseRole(NpgsqlConnection conn, SecretClient secretClient, string roleName, string user, CancellationToken cancellationToken = default)
    {
        try
        {
            Result result = null;
            await Console.Status().StartAsync($"Upserting password for role '{roleName}'...", async ctx =>
            {
                var pw = await GetOrCreatePassword(secretClient, roleName);
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
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateObject)
                {
                    if (pw.Updated)
                    {
                        ctx.Status($"Updating password for role '{roleName}'...");
                        cmd.CommandText = /*strpsql*/
                        $"""
                        ALTER ROLE "{roleName}"
                        WITH LOGIN
                        PASSWORD '{pw.Password}'
                        """;

                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                cmd.CommandText = /*strpsql*/
                $"""
                GRANT "{roleName}" TO "{user}"
                """;

                ctx.Status($"Granting Role '{roleName}' to '{user}'...");
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                WriteOperationSucceeded($"Role creation '{roleName}' and assigment to '{user}' upserted successfully.");
                result = new Result(roleName, pw.Password);
            });

            return result;
        }
        catch
        {
            WriteOperationFailed($"Role Creation '{roleName}' and assigment to '{user}' failed.");
            throw;
        }
    }

    private async Task<PasswordResult> GetOrCreatePassword(SecretClient secretClient, string roleName, CancellationToken cancellationToken = default)
    {
        var secretName = $"{Options.ServerName}-db-{roleName.Replace('_', '-')}-pw".ToLower();
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

    private void WriteOperationFailed(string msg) =>
        Console.Write(new Markup($"[bold red]:cross_mark_button: {msg}[/]\n"));

    private void WriteOperationSucceeded(string msg) =>
        Console.Write(new Markup($"[bold green]:check_mark_button: {msg}[/]\n"));

    private static string GenerateRandomPw()
    {
        const string VALID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+";
        return RandomNumberGenerator.GetString(VALID_CHARS.AsSpan(), 64);
    }

    private record PasswordResult(string Password, bool Updated);

    private record Result(string RoleName, string Password);
}

/// <summary>
/// Represents the configuration options required for the <see cref="DatabaseBootstrapper"/>.
/// These options define the Azure resources, database, and user details necessary for setting up the database environment.
/// </summary>
public class DatabaseBootstrapperOptions
{
    /// <summary>
    /// Gets or sets the name of the Azure resource group containing the PostgreSQL server.
    /// </summary>
    internal string ServerResourceGroup { get; set; }

    /// <summary>
    /// Gets or sets the Azure subscription ID where the PostgreSQL server is located.
    /// </summary>
    internal string ServerSubscription { get; set; }

    /// <summary>
    /// Gets or sets the name of the PostgreSQL server.
    /// </summary>
    internal string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the name of the Azure resource group containing the Key Vault.
    /// </summary>
    internal string KeyVaultResourceGroup { get; set; }

    /// <summary>
    /// Gets or sets the Azure subscription ID where the Key Vault is located.
    /// </summary>
    internal string KeyVaultSubscription { get; set; }

    /// <summary>
    /// Gets or sets the name of the Azure Key Vault used for storing database credentials.
    /// </summary>
    internal string KeyVaultName { get; set; }

    /// <summary>
    /// Gets or sets the configuration file containing database and schema details.
    /// </summary>
    internal FileInfo ConfigFile { get; set; }
}
