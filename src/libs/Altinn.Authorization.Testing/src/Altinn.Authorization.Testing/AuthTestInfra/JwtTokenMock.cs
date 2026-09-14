using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.IdentityModel.Tokens;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Creates JSON Web Tokens for tests, signed with the in-memory
/// <see cref="TestSigningCertificate"/>. The matching validation key is served
/// by <see cref="ConfigurationManagerStub"/> / <see cref="PublicSigningKeyProviderMock"/>.
/// </summary>
public static class JwtTokenMock
{
    /// <summary>
    /// Generates a token signed with the shared test certificate.
    /// </summary>
    /// <param name="principal">The claims principal to include in the token.</param>
    /// <param name="tokenExpiry">How long the token should be valid for.</param>
    /// <param name="issuer">The token issuer.</param>
    /// <returns>A serialized JWT.</returns>
    public static string GenerateToken(ClaimsPrincipal principal, TimeSpan tokenExpiry, string issuer = "UnitTest")
    {
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(principal.Identity),
            Expires = DateTime.UtcNow.AddSeconds(tokenExpiry.TotalSeconds),
            SigningCredentials = TestSigningCertificate.SigningCredentials,
            Audience = "altinn.no",
            Issuer = issuer
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
