using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.AccessManagement.TestUtils;

/// <summary>
/// Utility helper that generates JWTs for tests. It provides a deterministic
/// signing certificate and convenience helpers to create short-lived tokens
/// with configurable claims.
/// </summary>
public static class TestTokenGenerator
{
    /// <summary>
    /// Issuer value used when creating test tokens.
    /// </summary>
    public const string Issuer = "UnitTestIssuer";

    /// <summary>
    /// Audience value used when creating test tokens.
    /// </summary>
    public const string Audience = "altinn.no";

    /// <summary>
    /// Root RSA key used to generate the signing certificate for tests.
    /// </summary>
    public static readonly RSA RootCertificateKey = RSA.Create(2048);

    /// <summary>
    /// Predefined identity representing a person. Use as a template when
    /// creating tokens representing individual users.
    /// </summary>
    public static readonly ClaimsIdentity ClaimsIdentityPerson = new("person");

    /// <summary>
    /// Predefined identity representing an organization.
    /// </summary>
    public static readonly ClaimsIdentity ClaimsIdentityOrganization = new("organization");

    /// <summary>
    /// Predefined identity representing an application.
    /// </summary>
    public static readonly ClaimsIdentity ClaimsIdentityApp = new("app");

    private static readonly JwtSecurityTokenHandler _tokenHandler = new();

    private static readonly Lazy<X509Certificate2> _signingCert =
        new(() =>
        {
            var req = new CertificateRequest(
                "CN=Xunit",
                RootCertificateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

            var temp = req.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(1));

            return X509CertificateLoader.LoadPkcs12(
                temp.Export(X509ContentType.Pfx),
                password: null,
                keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet);
        });

    /// <summary>
    /// X.509 certificate used to sign generated tokens.
    /// </summary>
    public static X509Certificate2 SigningCert => _signingCert.Value;

    /// <summary>
    /// Security key wrapper around <see cref="SigningCert"/> for use with token validation.
    /// </summary>
    public static X509SecurityKey SigningKey => new(SigningCert);

    /// <summary>
    /// Creates a signed JWT using the test issuer/audience and the provided
    /// claims. Additional claims can be supplied via the <paramref name="configureClaims"/>
    /// callbacks which receive a mutable list that should be populated with
    /// <see cref="Claim"/> instances.
    /// </summary>
    /// <param name="claimsIdentity">Base identity for the token (not used to add claims by default).</param>
    /// <param name="configureClaims">Zero or more delegates that populate additional claims.</param>
    /// <returns>A serialized JWT as a string.</returns>
    public static string CreateToken(ClaimsIdentity claimsIdentity, params Action<List<Claim>>[] configureClaims)
    {
        var claims = new List<Claim>();
        foreach (var configure in configureClaims)
        {
            configure(claims);
        }

        var creds = new SigningCredentials(new RsaSecurityKey(RootCertificateKey), SecurityAlgorithms.RsaSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        return _tokenHandler.WriteToken(token);
    }
}
