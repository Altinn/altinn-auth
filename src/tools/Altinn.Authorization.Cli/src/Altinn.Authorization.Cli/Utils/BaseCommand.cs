using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Base command class.
/// </summary>
/// <typeparam name="TSettings"></typeparam>
public abstract class BaseCommand<TSettings>(CancellationToken cancellationToken)
    : AsyncCommand<TSettings>
    where TSettings : BaseCommandSettings
{
    public Task<int> ExecuteAsync(CommandContext context, TSettings settings)
        => ExecuteAsync(context, settings, cancellationToken);

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    /// <inheritdoc cref="AsyncCommand{TSettings}.ExecuteAsync(CommandContext, TSettings, CancellationToken)"/>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public override abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
}
