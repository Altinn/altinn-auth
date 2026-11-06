#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Altinn.Platform.Authentication.Helpers
{
    /// <summary>
    /// Picks the certificate that Altinn tokens are signed with, from the versions of the signing certificate
    /// in Key Vault.
    /// </summary>
    public static class SigningCertificateSelector
    {
        /// <summary>
        /// Returns the newest certificate that can sign now.
        /// </summary>
        /// <remarks>
        /// Only certificates with a private key whose validity has started are candidates. Unexpired ones are
        /// preferred, and among those the newest that has existed longer than <paramref name="rolloverDelayHours"/>,
        /// which gives a freshly published certificate time to reach token consumers. When every candidate has
        /// expired, the newest is still used, so a missed rotation does not stop sign-in, and an error is logged
        /// on every use so the rotation is noticed.
        /// <c>NotBefore</c>/<c>NotAfter</c> are local time on <see cref="X509Certificate2"/>, so they are compared in UTC.
        /// </remarks>
        /// <param name="certificates">The available signing certificates.</param>
        /// <param name="rolloverDelayHours">How long a certificate must have existed before it is preferred for signing.</param>
        /// <param name="now">The current time.</param>
        /// <param name="logger">Logs the expired-certificate fallback.</param>
        /// <returns>The certificate to sign with.</returns>
        /// <exception cref="InvalidOperationException">No certificate with a private key has started its validity.</exception>
        public static X509Certificate2 Select(IEnumerable<X509Certificate2> certificates, int rolloverDelayHours, DateTimeOffset now, ILogger logger)
        {
            DateTime nowUtc = now.UtcDateTime;

            List<X509Certificate2> started = certificates
                .Where(c => c.HasPrivateKey && c.NotBefore.ToUniversalTime() <= nowUtc)
                .ToList();

            if (started.Count == 0)
            {
                throw new InvalidOperationException("No JWT signing certificate with a private key is valid yet.");
            }

            List<X509Certificate2> unexpired = started
                .Where(c => c.NotAfter.ToUniversalTime() > nowUtc)
                .ToList();

            if (unexpired.Count == 0)
            {
                X509Certificate2 newestExpired = started.MaxBy(c => c.NotBefore.ToUniversalTime())!;
                logger.LogError(
                    "Every JWT signing certificate has expired; signing with {Thumbprint}, which expired {NotAfter:o}. Rotate the signing certificate in Key Vault.",
                    newestExpired.Thumbprint,
                    newestExpired.NotAfter.ToUniversalTime());
                return newestExpired;
            }

            DateTime rolloverCutoffUtc = now.AddHours(-rolloverDelayHours).UtcDateTime;
            List<X509Certificate2> settled = unexpired
                .Where(c => c.NotBefore.ToUniversalTime() <= rolloverCutoffUtc)
                .ToList();

            return (settled.Count > 0 ? settled : unexpired).MaxBy(c => c.NotBefore.ToUniversalTime())!;
        }
    }
}
