using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Test stub of <see cref="IConfigurationManager{OpenIdConnectConfiguration}"/>
/// that serves an OpenID configuration whose only signing key is
/// <see cref="TestSigningCertificate.SecurityKey"/>. Pairs with tokens minted by
/// <see cref="JwtTokenMock"/>, which sign with the same certificate.
/// </summary>
public class ConfigurationManagerStub : IConfigurationManager<OpenIdConnectConfiguration>
{
    /// <inheritdoc />
    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
    {
        OpenIdConnectConfiguration configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(TestSigningCertificate.SecurityKey);
        return Task.FromResult(configuration);
    }

    /// <inheritdoc />
    public void RequestRefresh()
    {
    }
}
