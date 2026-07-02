using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using Altinn.Authorization.Tests.Fixtures;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.Authorization.Tests.Integration
{
    /// <summary>
    /// Verifies that <see cref="DecisionTelemetry"/> emits the <c>altinn.pdp.decisions</c>
    /// counter with the <c>pdp.api.kind</c> dimension set according to which PDP API served
    /// the request: <c>internal</c> for <c>authorization/api/v1/decision</c> and
    /// <c>external</c> for <c>authorization/api/v1/authorize</c>.
    /// </summary>
    [IntegrationTest]
    public class PdpDecisionTelemetryTests : IClassFixture<AuthorizationApiFixture>
    {
        private const string ApiKindTag = "pdp.api.kind";

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
            using var collector = new PdpDecisionMetricCollector(_fixture.Services);

            HttpRequestMessage request = TestSetupUtil.CreateXacmlRequest("AltinnApps0001");
            HttpResponseMessage response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            IReadOnlyList<IReadOnlyDictionary<string, object?>> measurements = collector.Measurements;
            Assert.NotEmpty(measurements);
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.InternalApiDimensionValue, tags[ApiKindTag]));
        }

        [Fact]
        public async Task PDP_ExternalAuthorizeApi_RecordsDecisionMetric_WithExternalApiKind()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            HttpClient client = _fixture.BuildClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            using var collector = new PdpDecisionMetricCollector(_fixture.Services);

            HttpRequestMessage request = TestSetupUtil.CreateXacmlRequestExternal("AltinnApps0008");
            HttpResponseMessage response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            IReadOnlyList<IReadOnlyDictionary<string, object?>> measurements = collector.Measurements;
            Assert.NotEmpty(measurements);
            Assert.All(
                measurements,
                tags => Assert.Equal(DecisionTelemetry.ExternalApiDimensionValue, tags[ApiKindTag]));
        }

        /// <summary>
        /// Captures <c>altinn.pdp.decisions</c> measurements for a single test host.
        /// The instrument is resolved through the host's <see cref="IMeterFactory"/>;
        /// since the factory caches meters by name, this is the very same
        /// <see cref="Meter"/> instance <see cref="DecisionTelemetry"/> records on,
        /// and a different host (other test class) has a different instance.
        /// </summary>
        private sealed class PdpDecisionMetricCollector : IDisposable
        {
            private readonly MeterListener _listener;
            private readonly List<IReadOnlyDictionary<string, object?>> _measurements = [];
            private readonly object _gate = new();

            public PdpDecisionMetricCollector(IServiceProvider services)
            {
                IMeterFactory meterFactory = services.GetRequiredService<IMeterFactory>();
                Meter meter = meterFactory.Create(DecisionTelemetry.MeterName);

                _listener = new MeterListener
                {
                    InstrumentPublished = (instrument, listener) =>
                    {
                        if (ReferenceEquals(instrument.Meter, meter)
                            && instrument.Name == "altinn.pdp.decisions")
                        {
                            listener.EnableMeasurementEvents(instrument);
                        }
                    }
                };

                _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
                {
                    Dictionary<string, object?> snapshot = new(tags.Length);
                    foreach (KeyValuePair<string, object?> tag in tags)
                    {
                        snapshot[tag.Key] = tag.Value;
                    }

                    lock (_gate)
                    {
                        _measurements.Add(snapshot);
                    }
                });

                // Start() also replays already-published instruments, so this works
                // whether or not the DecisionTelemetry singleton was constructed by
                // an earlier request on this shared host.
                _listener.Start();
            }

            public IReadOnlyList<IReadOnlyDictionary<string, object?>> Measurements
            {
                get
                {
                    lock (_gate)
                    {
                        return _measurements.ToArray();
                    }
                }
            }

            public void Dispose() => _listener.Dispose();
        }
    }
}
