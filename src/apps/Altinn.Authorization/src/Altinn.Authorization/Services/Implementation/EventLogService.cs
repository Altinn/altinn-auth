using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Authorization.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// Implementation for authentication event log
    /// </summary>
    public partial class EventLogService : IEventLog
    {
        private readonly IEventsQueueClient _queueClient;
        private readonly TimeProvider _timeProvider;
        private readonly AuthorizationEventDuplicateTracker _duplicateTracker;
        private readonly DecisionTelemetry _telemetry;
        private readonly ILogger<EventLogService> _logger;
        private readonly AuditLogSuppressScope _suppressScope;

        private int _measurementFailureLogged;

        /// <summary>
        /// Instantiation for event log servcie
        /// </summary>
        /// <param name="queueClient">queue client to store event in event log</param>
        /// <param name="timeProvider">handler for datetime service</param>
        /// <param name="duplicateTracker">tracker for identifying repeated events</param>
        /// <param name="telemetry">PDP telemetry, for counting repeated events</param>
        /// <param name="logger">the logger</param>
        /// <param name="deduplicationSettings">which duplicates are dropped, when that is switched on</param>
        public EventLogService(IEventsQueueClient queueClient, TimeProvider timeProvider, AuthorizationEventDuplicateTracker duplicateTracker, DecisionTelemetry telemetry, ILogger<EventLogService> logger, IOptions<AuditLogDeduplicationSettings> deduplicationSettings)
        {
            _queueClient = queueClient;
            _timeProvider = timeProvider;
            _duplicateTracker = duplicateTracker;
            _telemetry = telemetry;
            _logger = logger;
            _suppressScope = deduplicationSettings.Value.SuppressScope;
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
                    bool measure = await featureManager.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement);

                    if (await featureManager.IsEnabledAsync(FeatureFlags.AuditLogDuplicateSuppression))
                    {
                        EnqueueUnlessDuplicate(authorizationEvent, contextRequest, measure, cancellationToken);
                        return;
                    }

                    _ = _queueClient.EnqueueAuthorizationEvent(authorizationEvent, cancellationToken);

                    if (measure)
                    {
                        MeasureDuplicate(authorizationEvent, contextRequest);
                    }
                }
            }
        }

        private void EnqueueUnlessDuplicate(AuthorizationEvent authorizationEvent, XacmlContextRequest contextRequest, bool measure, CancellationToken cancellationToken)
        {
            // When classifying fails, nothing is known to be a duplicate, and the event is queued: logging
            // too much is safer than logging too little.
            AuthorizationEventClassification? classification = null;
            try
            {
                classification = _duplicateTracker.Classify(authorizationEvent, EventLogHelper.GetResourceInstanceIds(contextRequest));

                if (measure)
                {
                    _telemetry.RecordAuditLogEvent(classification.Value.Kind);
                }
            }
            catch (Exception ex)
            {
                RecordMeasurementFailure(ex);
            }

            if (classification is { } duplicate && IsSuppressed(duplicate.Kind))
            {
                try
                {
                    // A repeat from another trace is dropped because its event is already in the log, so
                    // its own trace can be remembered too.
                    _duplicateTracker.Remember(duplicate);
                    _telemetry.RecordAuditLogEventSuppressed(duplicate.Kind);
                }
                catch (Exception ex)
                {
                    RecordMeasurementFailure(ex);
                }

                return;
            }

            _ = EnqueueAndRemember(authorizationEvent, classification, cancellationToken);
        }

        private async Task EnqueueAndRemember(AuthorizationEvent authorizationEvent, AuthorizationEventClassification? classification, CancellationToken cancellationToken)
        {
            QueuePostReceipt receipt;
            try
            {
                receipt = await _queueClient.EnqueueAuthorizationEvent(authorizationEvent, cancellationToken);
            }
            catch
            {
                // The queue client logs its own failures. An event that was not queued is not remembered.
                return;
            }

            // Remembered only once the queue has the event, so that a send that fails never makes a later
            // event look like a duplicate.
            if (classification is { } queued && receipt?.Success == true)
            {
                try
                {
                    _duplicateTracker.Remember(queued);
                }
                catch (Exception ex)
                {
                    RecordMeasurementFailure(ex);
                }
            }
        }

        private bool IsSuppressed(AuthorizationEventDuplicateKind duplicateKind) => duplicateKind switch
        {
            AuthorizationEventDuplicateKind.SameTrace => true,
            AuthorizationEventDuplicateKind.Window => _suppressScope == AuditLogSuppressScope.Window,
            _ => false,
        };

        private void MeasureDuplicate(AuthorizationEvent authorizationEvent, XacmlContextRequest contextRequest)
        {
            // The measurement only counts, so nothing in it, including recording or logging a failure,
            // may fail the decision the event belongs to.
            try
            {
                _telemetry.RecordAuditLogEvent(_duplicateTracker.Track(authorizationEvent, EventLogHelper.GetResourceInstanceIds(contextRequest)));
            }
            catch (Exception ex)
            {
                RecordMeasurementFailure(ex);
            }
        }

        private void RecordMeasurementFailure(Exception exception)
        {
            // Failures are counted with every event, but logged only once per instance: a failure that
            // repeats for every decision would otherwise flood the logs. Both are best effort, since the
            // meter or the logger may be what failed.
            try
            {
                _telemetry.RecordAuditLogEventMeasurementFailure();
            }
            catch
            {
                // Best effort, see above.
            }

            if (Interlocked.Exchange(ref _measurementFailureLogged, 1) == 0)
            {
                try
                {
                    Log.DuplicateMeasurementFailed(_logger, exception);
                }
                catch
                {
                    // Best effort, see above.
                }
            }
        }

        private static partial class Log
        {
            [LoggerMessage(1, LogLevel.Warning, "Measuring duplicate authorization events failed. Further failures are counted in altinn.pdp.auditlog.events as auditlog.duplicate=error, but not logged.")]
            public static partial void DuplicateMeasurementFailed(ILogger logger, Exception exception);
        }
    }
}
