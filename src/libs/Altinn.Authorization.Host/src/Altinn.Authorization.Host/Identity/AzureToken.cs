using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;

namespace Altinn.Authorization.Host.Identity;

/// <summary>
/// DefaultTokenCredential
/// </summary>
[ExcludeFromCodeCoverage]
public class AzureToken
{
    private static readonly Lazy<TokenCredential> _instance = new(
        () =>
        {
            return new DefaultAzureCredential();
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    private AzureToken() { }

    /// <summary>
    /// Instance
    /// </summary>
    public static TokenCredential Default => _instance.Value;
}
