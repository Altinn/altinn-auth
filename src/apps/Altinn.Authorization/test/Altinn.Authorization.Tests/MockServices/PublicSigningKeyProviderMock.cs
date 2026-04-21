using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Altinn.Common.AccessToken.Services;
using Altinn.Platform.Authorization.IntegrationTests.Util;

using Microsoft.IdentityModel.Tokens;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices;

public class PublicSigningKeyProviderMock : IPublicSigningKeyProvider
{
    public Task<IEnumerable<SecurityKey>> GetSigningKeys(string issuer)
    {
        List<SecurityKey> signingKeys = [TestCertificates.SecurityKey];
        return Task.FromResult(signingKeys.AsEnumerable());
    }
}
