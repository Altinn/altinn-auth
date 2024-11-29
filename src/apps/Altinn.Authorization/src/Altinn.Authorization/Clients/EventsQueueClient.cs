using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Clients
{
    /// <summary>
    /// Implementation of the <see ref="IEventsQueueClient"/> using Azure Storage Queues.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EventsQueueClient : IEventsQueueClient
    {
        private readonly QueueStorageSettings _settings;

        private QueueClient _authenticationEventQueueClient;
        private readonly ILogger<EventsQueueClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventsQueueClient"/> class.
        /// </summary>
        /// <param name="settings">The queue storage settings</param>
        /// <param name="logger">the logger handler</param>
        public EventsQueueClient(
            IOptions<QueueStorageSettings> settings,
            ILogger<EventsQueueClient> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<QueuePostReceipt> EnqueueAuthorizationEvent(string content, CancellationToken cancellationToken = default)
        {
            try
            {
                QueueClient client = await GetAuthorizationEventQueueClient();
                TimeSpan timeToLive = TimeSpan.FromDays(_settings.TimeToLive);
                await client.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(content)), null, timeToLive, cancellationToken);      
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new QueuePostReceipt { Success = false, Exception = ex };
            }

            return new QueuePostReceipt { Success = true };
        }

        private async Task<QueueClient> GetAuthorizationEventQueueClient()
        {
            if (_authenticationEventQueueClient == null)
            {
                _authenticationEventQueueClient = new QueueClient(_settings.EventLogConnectionString, _settings.AuthorizationEventQueueName);
                await _authenticationEventQueueClient.CreateIfNotExistsAsync();
            }

            return _authenticationEventQueueClient;
        }
    }
}
