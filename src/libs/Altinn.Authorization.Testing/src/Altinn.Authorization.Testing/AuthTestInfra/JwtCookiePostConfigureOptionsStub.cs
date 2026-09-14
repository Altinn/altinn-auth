using AltinnCore.Authentication.JwtCookie;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Test stub for <see cref="JwtCookiePostConfigureOptions"/> that swaps the real
/// OpenID configuration manager for <see cref="ConfigurationManagerStub"/>, so
/// JWT-cookie authentication validates tokens against the in-memory
/// <see cref="TestSigningCertificate"/> instead of a live metadata endpoint.
/// </summary>
public class JwtCookiePostConfigureOptionsStub : IPostConfigureOptions<JwtCookieOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string name, JwtCookieOptions options)
    {
        if (string.IsNullOrEmpty(options.JwtCookieName))
        {
            options.JwtCookieName = JwtCookieDefaults.CookiePrefix + name;
        }

        if (options.CookieManager == null)
        {
            options.CookieManager = new ChunkingCookieManager();
        }

        if (!string.IsNullOrEmpty(options.MetadataAddress))
        {
            if (!options.MetadataAddress.EndsWith("/", StringComparison.Ordinal))
            {
                options.MetadataAddress += "/";
            }
        }

        options.MetadataAddress += ".well-known/openid-configuration";
        options.ConfigurationManager = new ConfigurationManagerStub();
    }
}
