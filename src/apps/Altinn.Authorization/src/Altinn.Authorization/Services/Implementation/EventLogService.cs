using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Authorization.Telemetry;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// Implementation for authentication event log
    /// </summary>
    public class EventLogService : IEventLog
    {
        private readonly IEventsQueueClient _queueClient;
        private readonly TimeProvider _timeProvider;
        private readonly AuthorizationEventDuplicateTracker _duplicateTracker;
        private readonly DecisionTelemetry _telemetry;

        /// <summary>
        /// Instantiation for event log servcie
        /// </summary>
        /// <param name="queueClient">queue client to store event in event log</param>
        /// <param name="timeProvider">handler for datetime service</param>
        /// <param name="duplicateTracker">tracker for identifying repeated events</param>
        /// <param name="telemetry">PDP telemetry, for counting repeated events</param>
        public EventLogService(IEventsQueueClient queueClient, TimeProvider timeProvider, AuthorizationEventDuplicateTracker duplicateTracker, DecisionTelemetry telemetry)
        {
            _queueClient = queueClient;
            _timeProvider = timeProvider;
            _duplicateTracker = duplicateTracker;
            _telemetry = telemetry;
        }

        /// <inheritdoc />
        public async Task CreateAuthorizationEvent(IFeatureManager featureManager, XacmlContextRequest contextRequest, HttpContext context, XacmlContextResponse contextResponse, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            if (await featureManager.IsEnabledAsync(FeatureFlags.AuditLog))
            {
                AuthorizationEvent authorizationEvent = EventLogHelper.MapAuthorizationEventFromContextRequest(contextRequest, context, contextResponse, _timeProvider.GetUtcNow());

                if (authorizationEvent != null)
                {
                    _ = _queueClient.EnqueueAuthorizationEvent(authorizationEvent, cancellationToken);

                    if (await featureManager.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement))
                    {
                        _telemetry.RecordAuditLogEvent(_duplicateTracker.Track(authorizationEvent, EventLogHelper.GetResourceInstanceIds(contextRequest)));
                    }
                }
            }
        }
    }
}
