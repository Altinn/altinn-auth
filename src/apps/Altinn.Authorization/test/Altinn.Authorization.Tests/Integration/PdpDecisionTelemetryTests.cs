using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using Altinn.Authorization.Tests.Fixtures;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Telemetry;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Authorization.Tests.Integration
{
    /// <summary>
    /// Verifies that <see cref="DecisionTelemetry"/> emits the <c>altinn.pdp.decisions</c>
    /// counter with the <c>pdp.api.kind</c> dimension set according to which PDP API served
    /// the request: <c>internal</c> for <c>authorization/api/v1/decision</c> and
    /// <c>external</c> for <c>authorization/api/v1/authorize</c>.
    /// <para>
    /// Also verifies the <c>pdp.caller.kind</c> dimension, which separates decisions that should be
    /// billed to the resource owner from those requested by a consumer evaluating access to someone
    /// else's resource.
    /// </para>
    /// <para>
    /// Also verifies the <c>altinn.pdp.auditlog.events</c> counter, which measures how many of the
    /// events queued for the audit log repeat one already seen.
    /// </para>
    /// </summary>
    [IntegrationTest]
    public class PdpDecisionTelemetryTests : IClassFixture<AuthorizationApiFixture>
    {
        private const string ApiKindTag = "pdp.api.kind";
        private const string CallerKindTag = "pdp.caller.kind";
        private const string AuditLogDuplicateTag = "auditlog.duplicate";
        private const string DecisionsInstrument = "altinn.pdp.decisions";
        private const string AuditLogEventsInstrument = "altinn.pdp.auditlog.events";

        /// <summary>
        /// Digdir's organization number, the same in test and production. Callers on this number
        /// evaluate access to resources owned by others on behalf of the formidlingstjenester.
        /// </summary>
        private const string DigdirOrgNumber = "991825827";

        private readonly AuthorizationApiFixture _fixture;

        public PdpDecisionTelemetryTests(AuthorizationApiFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task PDP_InternalDecisionApi_RecordsDecisionMetric_WithInternalApiKind()
        {
            HttpClient client = _fixture.BuildClient();

            // Listener is filtered to this host's Meter instance, so measurements from
            // other test classes (which build their own host) cannot leak in.
            using var collector = new PdpMetricCollector(_fixture.Services.GetRequiredService<IMeterFactory>(), DecisionsInstrument);

            HttpRequestMessage request = TestSetupUtil.CreateXacmlRequest("AltinnApps0001");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            IReadOnlyList<IReadOnlyDictionary<string, object?>> measurements = collector.Measurements;
            Assert.NotEmpty(measurements);
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.InternalApiDimensionValue, tags[ApiKindTag]));
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.InternalCallerDimensionValue, tags[CallerKindTag]));
        }

        [Fact]
        public async Task PDP_ExternalAuthorizeApi_RecordsDecisionMetric_WithExternalApiKind()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            HttpClient client = _fixture.BuildClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            using var collector = new PdpMetricCollector(_fixture.Services.GetRequiredService<IMeterFactory>(), DecisionsInstrument);

            HttpRequestMessage request = TestSetupUtil.CreateXacmlRequestExternal("AltinnApps0008");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            IReadOnlyList<IReadOnlyDictionary<string, object?>> measurements = collector.Measurements;
            Assert.NotEmpty(measurements);
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.ExternalApiDimensionValue, tags[ApiKindTag]));

            // skd owns the resource being evaluated, so the decision is billed to the owner as before.
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.OwnerCallerDimensionValue, tags[CallerKindTag]));
        }

        [Fact]
        public async Task PDP_ExternalAuthorizeApi_DigdirConsumer_RecordsDecisionMetric_WithDigdirCallerKind()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", DigdirOrgNumber, "altinn:authorization/authorize");
            HttpClient client = _fixture.BuildClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            using var collector = new PdpMetricCollector(_fixture.Services.GetRequiredService<IMeterFactory>(), DecisionsInstrument);

            HttpRequestMessage request = TestSetupUtil.CreateXacmlRequestExternal("AltinnApps0008");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            IReadOnlyList<IReadOnlyDictionary<string, object?>> measurements = collector.Measurements;
            Assert.NotEmpty(measurements);

            // The resource is still owned by someone else; only the caller dimension changes, so that
            // the billing query can exclude these without losing the owner attribution.
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.ExternalApiDimensionValue, tags[ApiKindTag]));
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.DigdirCallerDimensionValue, tags[CallerKindTag]));
        }

        [Fact]
        public async Task PDP_RepeatedDecision_AuditLogDuplicateMeasurement_CountsDuplicate_AndStillQueuesEveryEvent()
        {
            Mock<IFeatureManager> featureManager = new();
            featureManager.Setup(m => m.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
            featureManager.Setup(m => m.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
            Mock<IEventsQueueClient> eventQueue = new();
            eventQueue
                .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueuePostReceipt { Success = true });

            // A host of its own, so the duplicate tracker starts out empty.
            WebApplicationFactory<Program> factory = _fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(featureManager.Object);
                services.AddSingleton(eventQueue.Object);
            }));
            HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            using var collector = new PdpMetricCollector(factory.Services.GetRequiredService<IMeterFactory>(), AuditLogEventsInstrument);

            for (int i = 0; i < 2; i++)
            {
                HttpResponseMessage response = await client.SendAsync(TestSetupUtil.CreateXacmlRequest("AltinnApps0001"), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // Measuring must not change what is logged.
            eventQueue.Verify(
                q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            IReadOnlyList<IReadOnlyDictionary<string, object?>> measurements = collector.Measurements;
            Assert.Equal(2, measurements.Count);
            Assert.Equal("none", measurements[0][AuditLogDuplicateTag]);

            // Two HTTP requests are two traces, so the repeat is a duplicate within the window.
            Assert.Equal("window", measurements[1][AuditLogDuplicateTag]);
        }
    }
}
