#nullable enable

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.Platform.Authentication.Clients.Interfaces;
using Altinn.Platform.Authentication.Configuration;
using Altinn.Platform.Authentication.Enum;
using Altinn.Platform.Authentication.Helpers;
using Altinn.Platform.Authentication.Model;
using Altinn.Platform.Authentication.Services.Interfaces;
using Azure.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authentication.Services
{
    /// <summary>
    /// Implementation for authentication event log
    /// </summary>
    public class EventLogService : IEventLog
    {
        private readonly IEventsQueueClient _queueClient;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<EventLogService> _logger;

        /// <summary>
        /// Instantiation for event log servcie
        /// </summary>
        /// <param name="queueClient">queue client to store event in event log</param>
        /// <param name="timeProvider">the timeprovider to get the current datetime</param>
        /// <param name="logger">the logger</param>
        public EventLogService(IEventsQueueClient queueClient, TimeProvider timeProvider, ILogger<EventLogService> logger)
        {
            _queueClient = queueClient;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <summary>
        /// Creates an authentication event
        /// </summary>
        /// <param name="featureManager">handler for feature manager service</param>
        /// <param name="authenticatedUser">authenticat</param>
        /// <param name="eventType">authentication event type</param>
        /// <param name="context">the http context</param>
        public async Task CreateAuthenticationEventAsync(IFeatureManager featureManager, UserAuthenticationModel authenticatedUser, AuthenticationEventType eventType, HttpContext context)
        {
            await EnqueueAsync(featureManager, eventType, () => EventlogHelper.MapAuthenticationEvent(authenticatedUser, eventType, context, _timeProvider.GetUtcNow()));
        }

        /// <summary>
        /// Creates an authentication event
        /// </summary>
        /// <param name="featureManager">handler for feature manager service</param>
        /// <param name="jwtToken">the token cookie with user information</param>
        /// <param name="eventType">authentication event type</param>
        /// <param name="ipadress">The ip adress of the caller</param>
        /// <param name="externalSessionId">the external session id</param>
        public async Task CreateAuthenticationEventAsync(
            IFeatureManager featureManager,
            string jwtToken,
            AuthenticationEventType eventType,
            System.Net.IPAddress ipadress,
            string? externalSessionId = null)
        {
            await EnqueueAsync(featureManager, eventType, () => EventlogHelper.MapAuthenticationEvent(jwtToken, eventType, ipadress, _timeProvider.GetUtcNow(), externalSessionId));
        }

        /// <summary>
        /// Checks the AuditLog flag, then maps and queues one audit event. Auditing must not break sign-in,
        /// refresh, logout or token exchange, so any failure, the flag check included, is logged with the
        /// event type instead of being thrown to the caller.
        /// </summary>
        private async Task EnqueueAsync(IFeatureManager featureManager, AuthenticationEventType eventType, Func<AuthenticationEvent?> mapEvent)
        {
            try
            {
                if (!await featureManager.IsEnabledAsync(FeatureFlags.AuditLog))
                {
                    return;
                }

                AuthenticationEvent? authenticationEvent = mapEvent();
                if (authenticationEvent is null)
                {
                    return;
                }

                QueuePostReceipt receipt = await _queueClient.EnqueueAuthenticationEvent(JsonSerializer.Serialize(authenticationEvent));
                if (!receipt.Success)
                {
                    _logger.LogError(receipt.Exception, "Audit event {EventType} could not be queued", eventType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit event {EventType} could not be created", eventType);
            }
        }
    }
}
