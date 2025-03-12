using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Altinn.Authorization.Cli.Utils;
using Azure.Core;
using Azure.Identity;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Database;

/// <summary>
/// Command for getting database login using signed-in Entra ID user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CredentialsCommand(CancellationToken cancellationToken)
    : BaseCommand<CredentialsCommand.Settings>(cancellationToken)
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var result = await GetToken(settings.Interactive, cancellationToken);

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var username = parsedToken.Claims.First(static claim => claim.Type == "unique_name");

        AnsiConsole.MarkupLineInterpolated($"[green]username[/]: {username.Value}");
        AnsiConsole.MarkupInterpolated($"[green]password[/]: ");
        Console.WriteLine($"{result.Token}");

        return 0;
    }

    private static async Task<AccessToken> GetToken(bool interactive, CancellationToken cancellationToken)
    {
        if (interactive)
        {
            return await new InteractiveBrowserCredential().GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), cancellationToken);
        }

        return await new DefaultAzureCredential().GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), cancellationToken);
    }

    /// <summary>
    /// Settings for the credentials command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        /// <summary>
        /// Gets a value indicating whether to truncate the target tables before copying.
        /// </summary>
        [Description("login using interactive UI in browser.")]
        [CommandOption("-i|--interactive")]
        public bool Interactive { get; init; } = false;
    }
}
