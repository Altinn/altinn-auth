using System.Security.Cryptography.X509Certificates;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.ResourceRegistry.Tests.Utils;

namespace Altinn.ResourceRegistry.Tests.Mocks

{
    public class AccessTokenGeneratorMock : IAccessTokenGenerator
    {
        public string GenerateAccessToken(string issuer, string app)
        {
            return PrincipalUtil.GetAccessToken("ResourceRegister");
        }

        public string GenerateAccessToken(string issuer, string app, X509Certificate2 certificate)
        {
            return PrincipalUtil.GetAccessToken("ResourceRegister");
        }
    }
}
