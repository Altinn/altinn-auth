using Altinn.Common.Authentication.Configuration;
using Altinn.Common.Authentication.Models;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class OidcProviderPostConfigureSettingsStub : IPostConfigureOptions<OidcProviderSettings>
    {
        public void PostConfigure(string name, OidcProviderSettings options)
        {
            OidcProvider provider = new OidcProvider();
            provider.Issuer = "www.altinn.no";
            provider.WellKnownConfigEndpoint = "https://testEndpoint.no";

            options = new OidcProviderSettings()
            {
                { "altinn", provider }
            };
        }
    }
}
