using Altinn.Platform.Authorization.Models.EventLog;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// The result of classifying an authorization event without remembering it, see
    /// <see cref="AuthorizationEventDuplicateTracker.Classify"/>. Pass it to
    /// <see cref="AuthorizationEventDuplicateTracker.Remember"/> once the event has been queued.
    /// </summary>
    public readonly struct AuthorizationEventClassification
    {
        internal AuthorizationEventClassification(AuthorizationEventDuplicateKind kind, ulong eventKey, ulong traceKey, long classifiedAtTicks)
        {
            Kind = kind;
            EventKey = eventKey;
            TraceKey = traceKey;
            ClassifiedAtTicks = classifiedAtTicks;
        }

        /// <summary>
        /// Whether, and how, the event repeats one already remembered.
        /// </summary>
        public AuthorizationEventDuplicateKind Kind { get; }

        internal ulong EventKey { get; }

        internal ulong TraceKey { get; }

        internal long ClassifiedAtTicks { get; }
    }
}
