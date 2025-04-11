namespace Altinn.Authorization.Integration.Platform.Appsettings
{
    /// <summary>
    /// Represents the platform-related configuration settings for Altinn authorization.
    /// This class provides endpoints for interacting with key platform services.
    /// </summary>
    public class PlatformSettings
    {
        /// <summary>
        /// Gets or sets the endpoint URI for the Resource Register service.
        /// This service provides metadata about resources in the Altinn ecosystem.
        /// </summary>
        /// <remarks>
        /// The endpoint should be a valid URI, typically pointing to an API service.
        /// </remarks>
        public EndpointOptions ResourceRegister { get; set; } = new();

        /// <summary>
        /// Gets or sets the endpoint URI for the Register service.
        /// This service is used for managing organizational and individual registry data within Altinn.
        /// </summary>
        /// <remarks>
        /// The endpoint should be a valid URI, typically pointing to an API service.
        /// </remarks>
        public EndpointOptions Register { get; set; } = new();

        /// <summary>
        /// Gets or sets the token-related configuration options.
        /// This includes settings for key vault and test tools.
        /// </summary>
        public TokenOptions Token { get; set; } = new();

        /// <summary>
        /// Represents token-related configuration options, including key vault and test tool settings.
        /// </summary>
        public class TokenOptions
        {
            /// <summary>
            /// Gets or sets the endpoint options for the Key Vault service.
            /// This service manages cryptographic keys and secrets.
            /// </summary>
            public KeyVaultOptions KeyVault { get; set; } = new();

            /// <summary>
            /// Gets or sets the test tool configuration options.
            /// </summary>
            public TestToolOptions TestTool { get; set; } = new();

            /// <summary>
            /// Options for Key vault
            /// </summary>
            public class KeyVaultOptions : EndpointOptions
            {
            }

            /// <summary>
            /// Represents configuration settings for the test tool, including authentication details.
            /// </summary>
            public class TestToolOptions : EndpointOptions
            {
                /// <summary>
                /// Gets or sets the username for authenticating with the test tool.
                /// </summary>
                public string Username { get; set; }

                /// <summary>
                /// Gets or sets the password for authenticating with the test tool.
                /// </summary>
                public string Password { get; set; }

                /// <summary>
                /// Gets or sets the environment in which the test tool operates.
                /// This can be used to differentiate between development, test, and production environments.
                /// </summary>
                public string Environment { get; set; }
            }
        }

        /// <summary>
        /// Represents an endpoint configuration, encapsulating a URI for service communication.
        /// </summary>
        public class EndpointOptions
        {
            /// <summary>
            /// Gets or sets the endpoint URI for the service.
            /// </summary>
            public Uri Endpoint { get; set; }
        }
    }
}
