using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.IdentityModel.Tokens;

namespace Altinn.Platform.Authorization.IntegrationTests.Util;

/// <summary>
/// Provides in-memory, self-signed X.509 certificates for use in integration tests.
/// Eliminates the need for certificate files checked into the repository.
/// </summary>
/// <remarks>
/// The certificate is generated lazily on first access using <see cref="DateTimeOffset.UtcNow"/>
/// for its validity window, so the produced certificate (serial, thumbprint, NotBefore/NotAfter)
/// will vary between test runs. Within a single process it is cached and reused.
/// </remarks>
internal static class TestCertificates
{
    private static readonly Lazy<X509Certificate2> _default = new(CreateCertificate);

    /// <summary>
    /// Self-signed certificate used for signing and verifying test JWTs.
    /// Replaces the former <c>selfSignedTestCertificate.pfx</c> / <c>.cer</c> files.
    /// </summary>
    public static X509Certificate2 Default => _default.Value;

    /// <summary>
    /// Signing credentials wrapping <see cref="Default"/> for token generation.
    /// </summary>
    public static SigningCredentials SigningCredentials =>
        new X509SigningCredentials(Default, SecurityAlgorithms.RsaSha256);

    /// <summary>
    /// Security key wrapping <see cref="Default"/> for token validation.
    /// </summary>
    public static SecurityKey SecurityKey => new X509SecurityKey(Default);

    private static X509Certificate2 CreateCertificate()
    {
        // The RSA instance is only needed to build and sign the temporary certificate. Once the
        // PFX bytes are exported and re-imported with EphemeralKeySet the returned certificate
        // owns its own key material, so we dispose the original RSA (and the temp cert) here to
        // avoid leaking CNG / OpenSSL handles across large test runs.
        using var rsa = RSA.Create(2048);

        var req = new CertificateRequest(
            "CN=AuthorizationTest",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

        using var temp = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        return X509CertificateLoader.LoadPkcs12(
            temp.Export(X509ContentType.Pfx),
            password: null,
            keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet);
    }
}
