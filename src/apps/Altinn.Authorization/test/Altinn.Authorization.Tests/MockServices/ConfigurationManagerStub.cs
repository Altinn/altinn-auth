using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Altinn.Authorization.Tests.Util;

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.Authorization.Tests.MockServices
{
    /// <summary>
    /// Represents a stub of <see cref="ConfigurationManager{OpenIdConnectConfiguration}"/> to be used in integration tests.
    /// </summary>
    public class ConfigurationManagerStub : IConfigurationManager<OpenIdConnectConfiguration>
    {
        /// <inheritdoc />
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            OpenIdConnectConfiguration configuration = new OpenIdConnectConfiguration();
            configuration.SigningKeys.Add(TestCertificates.SecurityKey);
            return Task.FromResult(configuration);
        }

        /// <inheritdoc />
        public void RequestRefresh()
        {
            throw new NotImplementedException();
        }
    }
}
