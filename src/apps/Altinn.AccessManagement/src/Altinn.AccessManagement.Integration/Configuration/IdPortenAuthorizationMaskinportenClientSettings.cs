namespace Altinn.AccessManagement.Integration.Configuration
{
    /// <summary>
    /// Configuration settings for the Maskinporten client used to integrate with the ID-porten authorizations API
    /// </summary>
    public class IdPortenAuthorizationMaskinportenClientSettings
    {
        /// <summary>
        /// Id-Porten API base url
        /// </summary>
        public string IdPortenApiEndpoint { get; set; } = string.Empty;
            
        /// <summary>
        /// The Maskinporten environment. Valid values are test or prod
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// ClientID to use to connect to Maskinporten
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Scopes to request. Must be provisioned on the supplied client.
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Base64 Encoded Json Web Key 
        /// </summary>
        public string EncodedJwk { get; set; } = string.Empty;
    }
}
