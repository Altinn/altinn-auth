using System;

using AltinnCore.Authentication.JwtCookie;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    /// <summary>
    /// Represents a stub for the <see cref="JwtCookiePostConfigureOptions"/> class to be used in integration tests.
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

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = false,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime= false,
                ValidateLifetime= false,
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = false,
                TryAllIssuerSigningKeys= false,
                IssuerSigningKeys = null,
            };

            options.ConfigurationManager = new ConfigurationManagerStub();
        }
    }
}
