#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Altinn.Platform.Authentication.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Altinn.Platform.Authentication.Tests.Helpers
{
    public class SigningCertificateSelectorTests
    {
        private const int RolloverDelayHours = 48;
        private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Select_NewCertificateInsideRolloverDelay_KeepsSigningWithTheSettledOne()
        {
            using X509Certificate2 settled = CreateCertificate(Now.AddDays(-300), Now.AddDays(65));
            using X509Certificate2 fresh = CreateCertificate(Now.AddHours(-2), Now.AddDays(365));

            X509Certificate2 selected = Select(settled, fresh);

            Assert.Same(settled, selected);
        }

        [Fact]
        public void Select_NewCertificatePastRolloverDelay_SignsWithTheNewOne()
        {
            using X509Certificate2 old = CreateCertificate(Now.AddDays(-300), Now.AddDays(65));
            using X509Certificate2 fresh = CreateCertificate(Now.AddHours(-RolloverDelayHours - 1), Now.AddDays(365));

            X509Certificate2 selected = Select(old, fresh);

            Assert.Same(fresh, selected);
        }

        [Fact]
        public void Select_NewestCertificateHasExpired_SignsWithTheOlderValidOne()
        {
            using X509Certificate2 valid = CreateCertificate(Now.AddDays(-300), Now.AddDays(65));
            using X509Certificate2 newestButExpired = CreateCertificate(Now.AddDays(-30), Now.AddDays(-1));

            X509Certificate2 selected = Select(valid, newestButExpired);

            Assert.Same(valid, selected);
        }

        [Fact]
        public void Select_SettledCertificateHasExpired_SignsWithTheNewOneInsideTheDelay()
        {
            using X509Certificate2 settledButExpired = CreateCertificate(Now.AddDays(-365), Now.AddHours(-1));
            using X509Certificate2 fresh = CreateCertificate(Now.AddHours(-2), Now.AddDays(365));

            X509Certificate2 selected = Select(settledButExpired, fresh);

            Assert.Same(fresh, selected);
        }

        [Fact]
        public void Select_EveryCertificateHasExpired_SignsWithTheNewestAndLogsAnError()
        {
            using X509Certificate2 older = CreateCertificate(Now.AddDays(-730), Now.AddDays(-365));
            using X509Certificate2 newest = CreateCertificate(Now.AddDays(-365), Now.AddDays(-1));
            FakeLogger logger = new();

            X509Certificate2 selected = SigningCertificateSelector.Select([older, newest], RolloverDelayHours, Now, logger);

            Assert.Same(newest, selected);
            FakeLogRecord record = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Error, record.Level);
            Assert.Contains(newest.Thumbprint, record.Message);
        }

        [Fact]
        public void Select_NewestCertificateHasNoPrivateKey_SignsWithOneThatHas()
        {
            using X509Certificate2 withKey = CreateCertificate(Now.AddDays(-300), Now.AddDays(65));
            using X509Certificate2 newest = CreateCertificate(Now.AddDays(-100), Now.AddDays(265));
            using X509Certificate2 publicOnly = X509CertificateLoader.LoadCertificate(newest.RawData);

            X509Certificate2 selected = Select(withKey, publicOnly);

            Assert.Same(withKey, selected);
        }

        [Fact]
        public void Select_NoCertificateHasStarted_Throws()
        {
            using X509Certificate2 future = CreateCertificate(Now.AddDays(1), Now.AddDays(366));

            Assert.Throws<InvalidOperationException>(() => Select(future));
        }

        [Fact]
        public void Select_NoRolloverDelay_SignsWithTheNewestUnexpired()
        {
            using X509Certificate2 old = CreateCertificate(Now.AddDays(-300), Now.AddDays(65));
            using X509Certificate2 fresh = CreateCertificate(Now.AddMinutes(-5), Now.AddDays(365));

            X509Certificate2 selected = SigningCertificateSelector.Select([old, fresh], 0, Now, NullLogger.Instance);

            Assert.Same(fresh, selected);
        }

        private static X509Certificate2 Select(params X509Certificate2[] certificates) =>
            SigningCertificateSelector.Select(new List<X509Certificate2>(certificates), RolloverDelayHours, Now, NullLogger.Instance);

        private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using RSA key = RSA.Create(2048);
            CertificateRequest request = new("CN=Altinn signing test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(notBefore, notAfter);
        }
    }
}
