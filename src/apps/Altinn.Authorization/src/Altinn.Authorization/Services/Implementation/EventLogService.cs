using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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

        /// <summary>
        /// Instantiation for event log servcie
        /// </summary>
        /// <param name="queueClient">queue client to store event in event log</param>
        /// <param name="timeProvider">handler for datetime service</param>
        public EventLogService(IEventsQueueClient queueClient, TimeProvider timeProvider)
        {
            _queueClient = queueClient;
            _timeProvider = timeProvider;
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
                }
            }
        }
    }
}
