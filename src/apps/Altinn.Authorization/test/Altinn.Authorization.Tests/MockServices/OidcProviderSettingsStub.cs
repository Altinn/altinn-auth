using System.Threading;
using System.Threading.Tasks;
using Altinn.Common.Authentication.Configuration;
using Altinn.Common.Authentication.Models;
using Microsoft.IdentityModel.Protocols;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class OidcProviderSettingsStub : IConfigurationManager<OidcProviderSettings>
    {
        /// <inheritdoc />
        public Task<OidcProviderSettings> GetConfigurationAsync(CancellationToken cancel)
        {
            OidcProvider provider = new OidcProvider();
            provider.Issuer = "www.altinn.no";
            provider.WellKnownConfigEndpoint = "https://testEndpoint.no";

            OidcProviderSettings settings = new OidcProviderSettings()
            {
                { "altinn", provider }
            };

            return Task.FromResult(settings);
        }

        public void RequestRefresh()
        {
            throw new System.NotImplementedException();
        }
    }
}
