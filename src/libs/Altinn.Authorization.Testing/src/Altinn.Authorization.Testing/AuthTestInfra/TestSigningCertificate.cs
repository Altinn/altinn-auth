using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.IdentityModel.Tokens;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Provides a single in-memory, self-signed X.509 certificate for signing and
/// validating JWTs in tests. Replaces per-app self-signed certificate files
/// checked into the repository (e.g. <c>selfSignedTestCertificate.pfx</c> /
/// <c>.cer</c>).
/// </summary>
/// <remarks>
/// The certificate is generated lazily on first access and cached for the life
/// of the process, so the signing key used to mint a token and the validation
/// key resolved by the test host always come from the same certificate. Because
/// the validity window is built from <see cref="DateTimeOffset.UtcNow"/>, the
/// concrete certificate (serial, thumbprint, NotBefore/NotAfter) varies between
/// runs but is stable within a run.
/// </remarks>
public static class TestSigningCertificate
{
    private static readonly Lazy<X509Certificate2> _default = new(CreateCertificate);

    /// <summary>
    /// Self-signed certificate used for signing and verifying test JWTs.
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
            "CN=AltinnAuthorizationTest",
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
