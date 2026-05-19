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

        private const string OwnerOrgTag = "resource.owner.org";
        private const string ResourceIdTag = "resource.id";
        private const string ApiKindTag = "pdp.api.kind";

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
        public void RecordDecision(string ownerOrg, string resourceId, string apiKind)
        {
            _pdpDecisions.Add(
                1,
                new KeyValuePair<string, object?>(OwnerOrgTag, ownerOrg.ToLowerInvariant()),
                new KeyValuePair<string, object?>(ResourceIdTag, resourceId.ToLowerInvariant()),
                new KeyValuePair<string, object?>(ApiKindTag, apiKind));
        }
    }
}
