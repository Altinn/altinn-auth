using System;
using System.Text;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Altinn.Platform.Authorization.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Clients
{
    /// <summary>
    /// Maskinporten client definition for OED Authz API integration
    /// </summary>
    public class OedAuthzMaskinportenClientDefinition : IClientDefinition
    {
        /// <inheritdoc/>
        public IMaskinportenSettings ClientSettings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OedAuthzMaskinportenClientDefinition"/> class
        /// </summary>
        /// <param name="clientSettings">Maskinporten client settings</param>
        public OedAuthzMaskinportenClientDefinition(IOptions<OedAuthzMaskinportenClientSettings> clientSettings) => ClientSettings = clientSettings.Value;

        /// <inheritdoc/>
        public Task<ClientSecrets> GetClientSecrets()
        {
            ClientSecrets clientSecrets = new ClientSecrets();

            byte[] bytesFromBase64Jwk = Convert.FromBase64String(ClientSettings.EncodedJwk);
            string jwkJson = Encoding.UTF8.GetString(bytesFromBase64Jwk);
            clientSecrets.ClientKey = new Microsoft.IdentityModel.Tokens.JsonWebKey(jwkJson);
            return Task.FromResult(clientSecrets);
        }
    }
}