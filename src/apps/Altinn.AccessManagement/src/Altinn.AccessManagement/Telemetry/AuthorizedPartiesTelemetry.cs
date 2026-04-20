using System.Diagnostics.Metrics;

namespace Altinn.AccessManagement.Telemetry
{
    /// <summary>
    /// Telemetry definitions for the resource owner AuthorizedParties API, exposing metrics that
    /// attribute usage back to the calling service owner so that volume can be allocated per
    /// tjenesteier.
    /// </summary>
    public sealed class AuthorizedPartiesTelemetry
    {
        /// <summary>
        /// Meter name registered with the OpenTelemetry meter provider in AccessManagementHost. Any
        /// change here must be reflected in the <c>AddMeter</c> call, otherwise the instrument will
        /// be silently dropped.
        /// </summary>
        public const string MeterName = "Altinn.AccessManagement.AuthorizedParties";

        /// <summary>
        /// Dimension value used when the caller's orgcode cannot be resolved.
        /// </summary>
        public const string UnknownDimensionValue = "unknown";

        private const string OwnerOrgTag = "resource.owner.org";

        private readonly Counter<long> _resourceOwnerRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizedPartiesTelemetry"/> class.
        /// Registered as a singleton so the underlying <see cref="Meter"/> lifetime is owned by
        /// <see cref="IMeterFactory"/> and integrates with OpenTelemetry resource attribution.
        /// </summary>
        public AuthorizedPartiesTelemetry(IMeterFactory meterFactory)
        {
            Meter meter = meterFactory.Create(MeterName);
            _resourceOwnerRequests = meter.CreateCounter<long>(
                "altinn.authorizedparties.resourceowner.requests",
                unit: "1",
                description: "Number of resource owner AuthorizedParties API requests");
        }

        /// <summary>
        /// Records a single resource owner AuthorizedParties request attributed to the given
        /// calling tjenesteier orgcode.
        /// </summary>
        public void RecordResourceOwnerRequest(string ownerOrg)
        {
            _resourceOwnerRequests.Add(
                1,
                new KeyValuePair<string, object?>(OwnerOrgTag, ownerOrg.ToLowerInvariant()));
        }
    }
}
