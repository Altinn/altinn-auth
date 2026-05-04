using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Azure.Security.KeyVault.Secrets;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Expands environment variables in the value.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class ExpandEnvironmentVariablesAttribute
    : ParameterValueProviderAttribute
{
    private static readonly ImmutableArray<StringSubstitutor> Substitutors
        = [
            new EnvironmentVariableSubstitutor(),
            new AzureKeyVaultSecretReferenceSubstitutor(),
        ];

    /// <inheritdoc/>
    public override bool TryGetValue(CommandParameterContext context, out object? result)
    {
        if (context.Value is not string value)
        {
            result = null;
            return false;
        }

        result = Expand(value);
        return true;
    }

    private static string Expand(string value)
    {
        while (TrySubstitute(value, out var substituted))
        {
            value = substituted;
        }

        return value;
    }

    private static bool TrySubstitute(string input, [NotNullWhen(true)] out string? output)
    {
        foreach (var substitutor in Substitutors)
        {
            if (substitutor.TrySubstitute(input, out output))
            {
                return true;
            }
        }

        output = null;
        return false;
    }

    private abstract class StringSubstitutor
    {
        public abstract bool TrySubstitute(string input, [NotNullWhen(true)] out string? output);
    }

    private sealed partial class EnvironmentVariableSubstitutor
        : StringSubstitutor
    {
        [GeneratedRegex("""\$\{([A-Z0-9_]+)\}""")]
        private static partial Regex GetEnvironmentVariableRegex();

        public override bool TrySubstitute(string input, [NotNullWhen(true)] out string output)
        {
            output = GetEnvironmentVariableRegex().Replace(input, static match =>
            {
                var name = match.Groups[1].Value;
                return Environment.GetEnvironmentVariable(name) ?? string.Empty;
            });

            return output != input;
        }
    }

    private sealed partial class AzureKeyVaultSecretReferenceSubstitutor
        : StringSubstitutor
    {
        [GeneratedRegex("""\{kv://(?<vault>[0-9a-z-]{3,24})/(?<secret>[0-9a-zA-Z-]{1,127})(?:/(?<version>[0-9a-f]{32}))?(?<query>\?[^\}]*?)?\}""")]
        private static partial Regex GetKeyVaultReferenceRegex();

        public override bool TrySubstitute(string input, [NotNullWhen(true)] out string output)
        {
            output = GetKeyVaultReferenceRegex().Replace(input, static match =>
            {
                var vault = match.Groups["vault"].Value;
                var secret = match.Groups["secret"].Value;
                var version = match.Groups["version"].Success ? match.Groups["version"].Value : null;

                Uri.TryCreate(match.Value[1..^1], UriKind.Absolute, out var asUri);
                var credentials = AzureCredentialHelper.GetCredential(asUri);
                SecretClient client = new(new Uri($"https://{vault}.vault.azure.net/"), credentials);
                var response = version is null
                    ? client.GetSecret(secret)
                    : client.GetSecret(secret, version);

                return response.Value.Value;
            });

            return output != input;
        }
    }
}
