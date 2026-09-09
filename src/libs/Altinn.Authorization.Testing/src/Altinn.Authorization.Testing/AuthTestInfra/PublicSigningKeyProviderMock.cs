using Altinn.Common.AccessToken.Services;

using Microsoft.IdentityModel.Tokens;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Test implementation of <see cref="IPublicSigningKeyProvider"/> that returns
/// the single <see cref="TestSigningCertificate.SecurityKey"/> for every issuer.
/// Use in tests where key rotation and per-issuer keys are not required.
/// </summary>
public class PublicSigningKeyProviderMock : IPublicSigningKeyProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<SecurityKey>> GetSigningKeys(string issuer) =>
        Task.FromResult<IEnumerable<SecurityKey>>([TestSigningCertificate.SecurityKey]);
}
