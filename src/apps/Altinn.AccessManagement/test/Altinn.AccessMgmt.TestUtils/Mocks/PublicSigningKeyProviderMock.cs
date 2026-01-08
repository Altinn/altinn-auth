using Altinn.Common.AccessToken.Services;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.AccessMgmt.TestUtils.Mocks;

/// <summary>
/// Test implementation of <see cref="IPublicSigningKeyProvider"/> that
/// returns a single static signing key from <see cref="TestTokenGenerator"/>.
/// Use this mock to provide a deterministic signing key for token
/// validation in integration tests.
/// </summary>
/// <remarks>
/// - The mock always returns the same signing key irrespective of the
///   provided issuer.
/// - This should only be used in tests where key rotation and multiple
///   issuers are not required.
/// </remarks>
public class PublicSigningKeyProviderMock : IPublicSigningKeyProvider
{
    public Task<IEnumerable<SecurityKey>> GetSigningKeys(string issuer) =>
        Task.FromResult<IEnumerable<SecurityKey>>([TestTokenGenerator.SigningKey]);
}
