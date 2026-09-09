using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Altinn.Platform.Authorization.Telemetry
{
    /// <summary>
    /// Telemetry definitions for the Policy Decision Point, exposing metrics that attribute PDP
    /// usage back to the resource owner so that volume and cost can be allocated per service owner.
    /// </summary>
    public sealed class DecisionTelemetry
    {
        /// <summary>
        /// Meter name registered with the OpenTelemetry meter provider in Program.cs. Any change
        /// here must be reflected in the <c>AddMeter</c> call, otherwise the instrument will be
        /// silently dropped.
        /// </summary>
        public const string MeterName = "Altinn.Authorization.Pdp";

        /// <summary>
        /// Dimension value used when an owner or resource identifier cannot be resolved.
        /// </summary>
        public const string UnknownDimensionValue = "unknown";

        /// <summary>
        /// Dimension value for requests arriving on the internal PDP API
        /// (<c>authorization/api/v1/decision</c>).
        /// </summary>
        public const string InternalApiDimensionValue = "internal";

        /// <summary>
        /// Dimension value for requests arriving on the external PDP API
        /// (<c>authorization/api/v1/authorize</c>).
        /// </summary>
        public const string ExternalApiDimensionValue = "external";

        /// <summary>
        /// Caller dimension value for decisions that should be attributed to the resource owner. Note
        /// that this says where the cost belongs, not that the caller is the owner: it is the value
        /// used for every external caller that is not a known cross-owner consumer.
        /// </summary>
        public const string OwnerCallerDimensionValue = "owner";

        /// <summary>
        /// Caller dimension value for decisions requested by Digdir, which evaluates access to
        /// resources owned by others in connection with the formidlingstjenester. These should not be
        /// billed to the resource owner.
        /// </summary>
        public const string DigdirCallerDimensionValue = "digdir";

        /// <summary>
        /// Caller dimension value for requests on the internal PDP API, which has no external consumer.
        /// Holding this constant keeps the internal traffic — by far the larger volume — from
        /// multiplying the number of time series.
        /// </summary>
        public const string InternalCallerDimensionValue = "internal";

        private const string OwnerOrgTag = "resource.owner.org";
        private const string ResourceIdTag = "resource.id";
        private const string ApiKindTag = "pdp.api.kind";
        private const string CallerKindTag = "pdp.caller.kind";

        private readonly Counter<long> _pdpDecisions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecisionTelemetry"/> class. Registered as a
        /// singleton so the underlying <see cref="Meter"/> lifetime is owned by <see cref="IMeterFactory"/>
        /// and integrates with OpenTelemetry resource attribution.
        /// </summary>
        public DecisionTelemetry(IMeterFactory meterFactory)
        {
            Meter meter = meterFactory.Create(MeterName);
            _pdpDecisions = meter.CreateCounter<long>(
                "altinn.pdp.decisions",
                unit: "1",
                description: "Number of PDP authorization decisions evaluated");
        }

        /// <summary>
        /// Records a single PDP decision with the given owner and resource identifier.
        /// </summary>
        /// <param name="ownerOrg">Resource owner org code, or <see cref="UnknownDimensionValue"/>.</param>
        /// <param name="resourceId">Resource identifier, or <see cref="UnknownDimensionValue"/>.</param>
        /// <param name="apiKind">
        /// Which PDP API the request arrived on, either <see cref="InternalApiDimensionValue"/> or
        /// <see cref="ExternalApiDimensionValue"/>.
        /// </param>
        /// <param name="callerKind">
        /// Who the decision should be attributed to, one of <see cref="OwnerCallerDimensionValue"/>,
        /// <see cref="DigdirCallerDimensionValue"/> or <see cref="InternalCallerDimensionValue"/>.
        /// Deliberately a small, closed set of values rather than the consumer identity itself, which
        /// would grow the number of time series with the number of calling organizations.
        /// </param>
        public void RecordDecision(string ownerOrg, string resourceId, string apiKind, string callerKind)
        {
            // TagList rather than the params overload: it stores up to eight tags inline, so recording
            // a decision stays allocation-free on a path that runs for every authorization request.
            TagList tags = new()
            {
                { OwnerOrgTag, ownerOrg.ToLowerInvariant() },
                { ResourceIdTag, resourceId.ToLowerInvariant() },
                { ApiKindTag, apiKind },
                { CallerKindTag, callerKind },
            };

            _pdpDecisions.Add(1, tags);
        }
    }
}
