using System.CommandLine;
using System.IdentityModel.Tokens.Jwt;
using Azure.Identity;
using Spectre.Console;

namespace Altinn.Authorization.CLI.Database;

/// <summary>
/// Contains commands for managing PostgreSQL servers, such as bootstrapping basic roles
/// and creating connection strings stored in a Key Vault.
/// </summary>
public static class DatabaseCommand
{
    /// <summary>
    /// Creates the root "database" command with its subcommands.
    /// </summary>
    /// <param name="console">Console abstraction for writing output and interacting with the user.</param>
    /// <returns>A Command object representing the "database" command.</returns>
    public static Command Commands(IAnsiConsole console)
    {
        var cmd = new Command("database", "For Managing Postgres Servers");
        cmd.AddCommand(SubCommandBootstrap(console));
        cmd.AddCommand(SubCommandCredentials(console));
        return cmd;
    }

    /// <summary>
    /// Defines a CLI subcommand that generates login credentials for a PostgreSQL server using the authenticated Entra ID (Azure AD) user.
    /// </summary>
    /// <remarks>
    /// This command uses the <see cref="DefaultAzureCredential"/> to authenticate and retrieve an access token for PostgreSQL.
    /// The token is then decoded to extract the username, which is displayed along with the generated password.
    /// </remarks>
    public static Command SubCommandCredentials(IAnsiConsole console)
    {
        var credentials = new Command("cred", "Generates login credentials for PostgreSQL server using your signed-in Entra ID user.");

        credentials.SetHandler(async () =>
        {
            var token = new DefaultAzureCredential();
            var cred = await token.GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), CancellationToken.None);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(cred.Token);
            var username = jwtToken.Claims.First(claim => claim.Type == "unique_name");

            console.WriteLine($"username: {username.Value}");
            console.WriteLine($"password: {cred.Token}");
        });

        return credentials;
    }

    /// <summary>
    /// Creates the "bootstrap" subcommand for initializing database roles and storing connection strings in Key Vault.
    /// </summary>
    /// <param name="console">Console abstraction for user interaction.</param>
    /// <returns>A Command object representing the "bootstrap" command.</returns>
    public static Command SubCommandBootstrap(IAnsiConsole console)
    {
        var bootstrap = new Command("bootstrap", "Creates essential database roles, generates connection strings for migration and app run-time use, and stores them securely in Azure Key Vault.");
        var serverResourceGroupOption = new Option<string>("--server-resource-group", "Postgres Flex server's resource group.")
        {
            IsRequired = true,
        };

        var serverSubscriptionOption = new Option<string>("--server-subscription", "ID of subscription for Postgres Flex server.")
        {
            IsRequired = true,
        };

        var serverNameOption = new Option<string>("--server-name", "Name of the Postgres Flex server.")
        {
            IsRequired = true,
        };

        var keyvaultResourceGroupOption = new Option<string>("--kv-resource-group", "Key Vault's resource group.")
        {
            IsRequired = true,
        };

        var keyvaultSubscriptionOption = new Option<string>("--kv-subscription", "ID of subscription for Key Vault.")
        {
            IsRequired = true,
        };

        var keyvaultNameOption = new Option<string>("--kv-name", "Name of the Postgres Flex server.")
        {
            IsRequired = true,
        };

        var configFileOption = new Option<FileInfo>("--config-file", "path to conf.json for app")
        {
            IsRequired = true,
        };

        var options = new List<Option>()
        {
            serverResourceGroupOption,
            serverSubscriptionOption,
            serverNameOption,
            keyvaultResourceGroupOption,
            keyvaultSubscriptionOption,
            keyvaultNameOption,
            configFileOption,
        };

        options.ForEach(bootstrap.AddOption);

        bootstrap.SetHandler(
            async (serverResourceGroup, serverSubscription, serverName, keyvaultResourceGroup, keyvaultSubscription, keyvaultName, configFile) =>
            {
                await new DatabaseBootstrapper(console, new()
                {
                    ServerResourceGroup = serverResourceGroup,
                    ServerSubscription = serverSubscription,
                    ServerName = serverName,
                    KeyVaultResourceGroup = keyvaultResourceGroup,
                    KeyVaultName = keyvaultName,
                    KeyVaultSubscription = keyvaultSubscription,
                    ConfigFile = configFile,
                }).Run();
            },
            serverResourceGroupOption,
            serverSubscriptionOption,
            serverNameOption,
            keyvaultResourceGroupOption,
            keyvaultSubscriptionOption,
            keyvaultNameOption,
            configFileOption);

        return bootstrap;
    }
}
