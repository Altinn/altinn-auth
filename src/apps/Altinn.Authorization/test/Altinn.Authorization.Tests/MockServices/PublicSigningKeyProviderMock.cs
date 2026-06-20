using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Altinn.Authorization.Tests.Util;
using Altinn.Common.AccessToken.Services;

using Microsoft.IdentityModel.Tokens;

namespace Altinn.Authorization.Tests.MockServices;

public class PublicSigningKeyProviderMock : IPublicSigningKeyProvider
{
    public Task<IEnumerable<SecurityKey>> GetSigningKeys(string issuer)
    {
        List<SecurityKey> signingKeys = [TestCertificates.SecurityKey];
        return Task.FromResult(signingKeys.AsEnumerable());
    }
}
