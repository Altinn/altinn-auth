using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform;

/// <summary>
/// Configuration options for Altinn platform integration.
/// </summary>
public class AltinnIntegrationOptions
{
    /// <summary>
    /// The name of the HTTP client to be used. Override this if using a custom HTTP client.
    /// </summary>
    public string HttpClientName { get; set; } = Options.DefaultName;

    /// <summary>
    /// Options for configuring platform access tokens.
    /// </summary>
    public PlatformAccessTokenOptions PlatformAccessToken { get; set; } = new();

    /// <summary>
    /// Configuration options for obtaining platform access tokens.
    /// </summary>
    public class PlatformAccessTokenOptions
    {
        /// <summary>
        /// Specifies the source of the token, either from Azure Key Vault or Test Tool.
        /// </summary>
        public TokenSource TokenSource { get; set; } = TokenSource.TestTool;

        /// <summary>
        /// The issuer of the platform access token. probably "platform"
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// The application claim associated with creator of the token.
        /// </summary>
        public string App { get; set; } = string.Empty;

        /// <summary>
        /// Options for generating tokens using certs and secrets from Key vault.
        /// </summary>
        public KeyVaultOptions KeyVault { get; set; } = new();

        /// <summary>
        /// Options for obtaining tokens using the test tool.
        /// </summary>
        public TestToolOptions TestTool { get; set; } = new();

        /// <summary>
        /// Configuration settings for using Azure Key Vault as a token source.
        /// </summary>
        public class KeyVaultOptions
        {
            /// <summary>
            /// The endpoint URI for the Azure Key Vault.
            /// </summary>
            public Uri Endpoint { get; set; }

            /// <summary>
            /// The timeout duration (in seconds) for caching the token.
            /// </summary>
            public int CacheTimeout { get; set; } = 30;
        }

        /// <summary>
        /// Configuration settings for using the test tool as a token source.
        /// </summary>
        public class TestToolOptions
        {
            /// <summary>
            /// The endpoint URI for the test tool API.
            /// </summary>
            public Uri Endpoint { get; set; }

            /// <summary>
            /// Basic Auth username.
            /// </summary>
            public string Username { get; set; } = string.Empty;

            /// <summary>
            /// Basuc Auth password.
            /// </summary>
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// Environment for the test tool to generate a token for. 
            /// </summary>
            public string Environment { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Enum representing the available token sources.
    /// </summary>
    public enum TokenSource
    {
        /// <summary>
        /// Tokens are generated using certs and secrets from Azure Key Vault.
        /// </summary>
        AzureKeyVault,

        /// <summary>
        /// Tokens are obtained from the Altinn test tool API.
        /// </summary>
        TestTool,
    }
}
