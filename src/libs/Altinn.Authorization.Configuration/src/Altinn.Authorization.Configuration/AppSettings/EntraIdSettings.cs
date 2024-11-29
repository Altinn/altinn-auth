using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;

namespace Altinn.Authorization.Configuration.AppSettings;

/// <summary>
/// Configuration settings for Azure identities services using Entra ID (formerly Azure Active Directory). 
/// This class contains details related to user-assigned identities, which are used to authenticate and 
/// authorize requests to Azure resources.
/// </summary>
[ExcludeFromCodeCoverage]
public class EntraIdSettings
{
    /// <summary>
    /// Contains configuration for user-assigned managed identities. 
    /// These identities are used to authenticate and authorize secure access to Azure resources, 
    /// allowing services and applications to interact with Azure using assigned identities.
    /// </summary>
    public UserAssignedIdentitiesSettings Identities { get; set; } = new();

    /// <summary>
    /// Represents the settings for user-assigned managed identities, enabling secure authentication
    /// for different Azure services or components.
    /// </summary>
    public class UserAssignedIdentitiesSettings
    {
        /// <summary>
        /// Represents the managed identity used for authenticating services in Azure.
        /// This identity is typically assigned to applications or services that need to access 
        /// other Azure resources securely.
        /// </summary>
        public PrincipalSettings Service { get; set; } = new();

        /// <summary>
        /// Represents the managed identity used for authenticating administrative operations 
        /// in Azure-managed PostgreSQL databases.
        /// This identity is assigned to perform administrative tasks on the database securely.
        /// </summary>
        public PrincipalSettings PostgresAdmin { get; set; } = new();

        /// <summary>
        /// Holds configuration details for a specific managed identity, including the client ID 
        /// and the corresponding token credential, which is used to authenticate requests to Azure.
        /// </summary>
        public class PrincipalSettings
        {
            /// <summary>
            /// The client ID of the user-assigned managed identity. 
            /// This ID uniquely identifies the managed identity and is used in conjunction 
            /// with Entra's identity and access management services to authenticate requests.
            /// </summary>
            public string ClientId { get; set; } = DefaultTokenHandler.DefaultClientId;

            /// <summary>
            /// Gets the <see cref="TokenCredential"/> for the user-assigned managed identity.
            /// This credential is used to authenticate requests to Azure services.
            /// If the <see cref="PrincipalSettings.ClientId"/> is valid, it retrieves the corresponding managed identity token credential 
            /// from the <see cref="DefaultTokenHandler"/>; otherwise, it defaults to use <see cref="DefaultAzureCredential"/>.
            /// </summary>
            public TokenCredential TokenCredential => DefaultTokenHandler.UseCredentials(ClientId);
        }

        /// <summary>
        /// Handles token credential retrieval for managed identities.
        /// </summary>
        internal static class DefaultTokenHandler
        {
            private static SemaphoreSlim Semaphore { get; } = new(1);

            /// <summary>
            /// Dictionary to cache TokenCredential instances by their client IDs.
            /// </summary>
            internal static Dictionary<string, TokenCredential> TokenHandlers { get; set; } = [];

            /// <summary>
            /// Represents the default Client ID used for managed identity authentication in scenarios
            /// where no specific Client ID is provided and fallback to use default credentials. 
            /// </summary>
            internal const string DefaultClientId = "00000000-0000-0000-0000-000000000000";

            /// <summary>
            /// Retrieves a TokenCredential based on the provided client ID.
            /// </summary>
            /// <param name="clientId">The client ID of the managed identity.</param>
            /// <returns>A TokenCredential for the specified client ID.</returns>
            public static TokenCredential UseCredentials(string clientId)
            {
                Semaphore.Wait();
                try
                {
                    if (string.IsNullOrEmpty(clientId) || clientId == DefaultClientId)
                    {
                        return UseDefaultToken();
                    }

                    return UseManagedIdentity(clientId);
                }
                finally
                {
                    Semaphore.Release();
                }
            }

            private static TokenCredential UseDefaultToken()
            {
                if (TokenHandlers.TryGetValue(DefaultClientId, out var credential))
                {
                    return credential;
                }

                TokenHandlers[DefaultClientId] = new DefaultAzureCredential();
                return TokenHandlers[DefaultClientId];
            }

            private static TokenCredential UseManagedIdentity(string clientId)
            {
                if (TokenHandlers.TryGetValue(clientId, out var credential))
                {
                    return credential;
                }

                TokenHandlers[clientId] = new ManagedIdentityCredential(clientId);

                return TokenHandlers[clientId];
            }
        }
    }
}
