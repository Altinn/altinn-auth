#nullable enable
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authentication.Configuration;
using Altinn.Platform.Authentication.Core.Models.Oidc;
using Altinn.Platform.Authentication.Core.Services.Interfaces;
using Altinn.Platform.Authentication.Helpers;
using Altinn.Platform.Authentication.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.Platform.Authentication.Services
{
    /// <summary>
    /// Token issuer service for minting OAuth 2.0 / OIDC tokens.
    /// </summary>
    public class TokenIssuerService(IJwtSigningCertificateProvider jwtSigningCertificateProvider, IOptions<GeneralSettings> generalSettings, TimeProvider timeProvider, ILogger<TokenIssuerService> logger) : ITokenIssuer
    {
        private readonly IJwtSigningCertificateProvider _certificateProvider = jwtSigningCertificateProvider;
        private readonly GeneralSettings _generalSettings = generalSettings.Value;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly ILogger<TokenIssuerService> _logger = logger;

        /// <inheritdoc/>
        public async Task<string> CreateAccessTokenAsync(ClaimsPrincipal principal, DateTimeOffset tokenExpiration, CancellationToken cancellationToken = default)
        {
            string accessToken = await GenerateToken(principal, tokenExpiration);
            return accessToken;
        }

        /// <inheritdoc/>
        public async Task<string> CreateIdTokenAsync(ClaimsPrincipal principal, OidcClient client, DateTimeOffset tokenExpiration, CancellationToken cancellationToken = default)
        {
            return await GenerateToken(principal, tokenExpiration);
        }

        private async Task<string> GenerateToken(ClaimsPrincipal principal, DateTimeOffset tokenExpiration, string? audience = null)
        {
            List<X509Certificate2> certificates = await _certificateProvider.GetCertificates();

            X509Certificate2 certificate = SigningCertificateSelector.Select(
                certificates, _generalSettings.JwtSigningCertificateRolloverDelayHours, _timeProvider.GetUtcNow(), _logger);

            JwtSecurityTokenHandler tokenHandler = new();
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                IssuedAt = _timeProvider.GetUtcNow().UtcDateTime,
                Issuer = _generalSettings.AltinnOidcIssuerUrl,
                NotBefore = _timeProvider.GetUtcNow().UtcDateTime,
                Subject = new ClaimsIdentity(principal.Claims, (principal.Identity as ClaimsIdentity)?.AuthenticationType),
                Audience = audience,
                Expires = tokenExpiration.UtcDateTime,
                SigningCredentials = new X509SigningCredentials(certificate)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            string serializedToken = tokenHandler.WriteToken(token);

            return serializedToken;
        }
    }
}
