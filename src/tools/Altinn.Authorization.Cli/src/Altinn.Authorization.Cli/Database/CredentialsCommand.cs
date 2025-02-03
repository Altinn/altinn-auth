using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Altinn.Authorization.Cli.Utils;
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
        var credentials = new DefaultAzureCredential();
        var token = await credentials.GetTokenAsync(new(["https://ossrdbms-aad.database.windows.net/.default"]), cancellationToken);

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
        var username = parsedToken.Claims.First(static claim => claim.Type == "unique_name");

        AnsiConsole.MarkupLineInterpolated($"[green]username[/]: {username.Value}");
        AnsiConsole.MarkupLineInterpolated($"[green]password[/]: {token.Token}");

        return 0;
    }

    /// <summary>
    /// Settings for the credentials command.
    /// </summary>
    public class Settings
        : BaseCommandSettings
    {
    }
}
