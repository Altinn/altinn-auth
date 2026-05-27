using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Cli.Utils;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Identity;

[ExcludeFromCodeCoverage]
public class HashCommand(CancellationToken cancellationToken)
    : BaseCommand<HashCommand.Settings>(cancellationToken)
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settings.UserName) || string.IsNullOrEmpty(settings.Password))
        {
            AnsiConsole.MarkupLine("[red]Both username and password must be provided.[/]");
            return -1;
        }

        var hash = PasswordHash.Create(settings.UserName, settings.Password);
        AnsiConsole.WriteLine(hash);

        return 0;
    }

    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        [Description("The user name of the identity to hash.")]
        [CommandArgument(0, "<USERNAME>")]
        [ExpandEnvironmentVariables]
        public string? UserName { get; init; }

        [Description("The password of the identity to hash.")]
        [CommandArgument(1, "<PASSWORD>")]
        [ExpandEnvironmentVariables]
        public string? Password { get; init; }
    }
}
