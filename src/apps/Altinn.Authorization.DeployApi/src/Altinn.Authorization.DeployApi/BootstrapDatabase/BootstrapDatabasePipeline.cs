using System.Text.Json.Serialization;
using Altinn.Authorization.DeployApi.Pipelines;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.PostgreSql.FlexibleServers;
using Azure.Security.KeyVault.Secrets;
using Npgsql;

namespace Altinn.Authorization.DeployApi.BootstrapDatabase;

internal sealed class BootstrapDatabasePipeline
    : TaskPipeline
{
    [JsonPropertyName("resources")]
    public required ResourcesConfig Resources { get; init; }

    [JsonPropertyName("databaseName")]
    public required string DatabaseName { get; init; }

    [JsonPropertyName("userPrefix")]
    public required string UserPrefix { get; init; }

    [JsonPropertyName("schemas")]
    public required IReadOnlyDictionary<string, SchemaBootstrapModel> Schemas { get; init; }

    protected internal override async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        var cred = context.GetRequiredService<TokenCredential>();
        var client = new ArmClient(cred, defaultSubscriptionId: Resources.SubscriptionId);

        var subscription = await context.RunTask(
            "Get subscription info",
            (_, ct) => client.GetDefaultSubscriptionAsync(ct),
            cancellationToken);

        var resourceGroup = await context.RunTask(
            "Get resource group info",
            (_, ct) => subscription.GetResourceGroupAsync(Resources.ResourceGroupName, ct),
            cancellationToken);

        var keyVault = await context.RunTask(
            "Get key vault info",
            (_, ct) => resourceGroup.Value.GetKeyVaultAsync(Resources.KeyVaultName, ct),
            cancellationToken);

        var server = await context.RunTask(
            "Get server info",
            (_, ct) => resourceGroup.Value.GetPostgreSqlFlexibleServerAsync(Resources.ServerName, ct),
            cancellationToken);

        var token = await context.RunTask(
            "Get db auth token",
            (_, ct) => cred.GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), ct).AsTask(),
            cancellationToken);

        var secretClient = new SecretClient(keyVault.Value.Data.Properties.VaultUri, cred);

        var serverUrl = server.Value.Data.FullyQualifiedDomainName;
        var connStringBuilder = new NpgsqlConnectionStringBuilder()
        {
            Host = serverUrl,
            Database = "postgres",
            Username = Resources.User,
            Password = token.Token,
            Port = 5432,
            SslMode = SslMode.Require,
            Pooling = false,
        };

        var serverConnString = connStringBuilder.ToString();
        await using var serverConn = new NpgsqlConnection(serverConnString);
        await context.RunTask(
            "Connecting to database server",
            (_, ct) => serverConn.OpenAsync(ct),
            cancellationToken);

        var migratorUser = await context.RunTask(new CreateDatabaseRoleTask(secretClient, serverConn, $"{UserPrefix}_migrator", Resources.User), cancellationToken);
        var appUser = await context.RunTask(new CreateDatabaseRoleTask(secretClient, serverConn, $"{UserPrefix}_app", Resources.User), cancellationToken);
        await context.RunTask(new CreateDatabaseTask(serverConn, DatabaseName), cancellationToken);
        await context.RunTask(new GrantDatabasePrivilegesTask(serverConn, DatabaseName, migratorUser.RoleName, "CREATE, CONNECT"), cancellationToken);
        await context.RunTask(new GrantDatabasePrivilegesTask(serverConn, DatabaseName, appUser.RoleName, "CONNECT"), cancellationToken);

        connStringBuilder.Database = DatabaseName;
        var dbConnString = connStringBuilder.ToString();
        await using var dbConn = new NpgsqlConnection(dbConnString);
        await context.RunTask(
            $"Connecting to database '[cyan]{DatabaseName}[/]'",
            (_, ct) => dbConn.OpenAsync(ct),
            cancellationToken);

        foreach (var (schemaName, schemaCfg) in Schemas)
        {
            await context.RunTask(new CreateDatabaseSchemaTask(dbConn, migratorUser.RoleName, schemaName, schemaCfg), cancellationToken);
        }

        connStringBuilder = new NpgsqlConnectionStringBuilder()
        {
            Host = serverUrl,
            Database = DatabaseName,
            Username = Resources.User,
            Password = token.Token,
            Port = 5432,
            SslMode = SslMode.Require,
        };

        var connectionStrings = new Dictionary<string, string>();

        connStringBuilder.Username = migratorUser.RoleName;
        connStringBuilder.Password = migratorUser.Password;
        connectionStrings[$"db-{UserPrefix}-migrator"] = connStringBuilder.ToString();

        connStringBuilder.Username = appUser.RoleName;
        connStringBuilder.Password = appUser.Password;
        connectionStrings[$"db-{UserPrefix}-app"] = connStringBuilder.ToString();

        await context.RunTask(new SaveConnectionStringsTask(secretClient, connectionStrings), cancellationToken);
    }

    public record ResourcesConfig
    {
        [JsonPropertyName("subscriptionId")]
        public required string SubscriptionId { get; init; }

        [JsonPropertyName("resourceGroup")]
        public required string ResourceGroupName { get; init; }

        [JsonPropertyName("serverName")]
        public required string ServerName { get; init; }

        [JsonPropertyName("user")]
        public required string User { get; init; } = Environment.GetEnvironmentVariable("ManagedIdentity__ClientId");

        [JsonPropertyName("keyVaultName")]
        public required string KeyVaultName { get; init; }
    }

    public record SchemaBootstrapModel
    {
    }
}
