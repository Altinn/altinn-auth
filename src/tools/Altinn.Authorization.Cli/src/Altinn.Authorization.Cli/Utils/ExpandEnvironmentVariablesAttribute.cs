using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Expands environment variables in the value.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class ExpandEnvironmentVariablesAttribute
    : ParameterValueProviderAttribute
{
    [GeneratedRegex("""\$\{([A-Z0-9_]+)\}""")]
    private static partial Regex GetEnvironmentVariableRegex();

    /// <inheritdoc/>
    public override bool TryGetValue(CommandParameterContext context, out object? result)
    {
        if (context.Value is not string value)
        {
            result = null;
            return false;
        }

        result = ExpandEnvironmentVariables(value);
        return true;
    }

    private static string ExpandEnvironmentVariables(string value)
    {
        var regex = GetEnvironmentVariableRegex();

        string before, after = value;
        do
        {
            before = after;
            after = regex.Replace(before, static match =>
            {
                var name = match.Groups[1].Value;
                return Environment.GetEnvironmentVariable(name) ?? string.Empty;
            });
        }
        while (before != after);

        return after;
    }
}
