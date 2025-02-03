using Azure.Core;
using Azure.Identity;

namespace Altinn.Authorization.Host;

/// <summary>
/// DefaultTokenCredential
/// </summary>
public class DefaultTokenCredential
{
    private static readonly Lazy<TokenCredential> _instance = new(() =>
    {
        if (Environment.GetEnvironmentVariable("Azure__ClientID") is var clientId && !string.IsNullOrEmpty(clientId))
        {
            return new WorkloadIdentityCredential(new()
            {
                ClientId = clientId,
            });
        }

        return new DefaultAzureCredential();
    });

    private DefaultTokenCredential() { }

    /// <summary>
    /// Instance
    /// </summary>
    public static TokenCredential Instance => _instance.Value;
}
