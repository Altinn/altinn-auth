using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.EventLog;
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

                // Prepare the message as UTF-8 bytes and Base64 encode (if you must keep Base64)
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(content);
                string base64Content = Convert.ToBase64String(utf8Bytes);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                options.Converters.Add(new JsonStringEnumConverter());
                // Azure Storage Queue message size limit is 64 KB (65536 bytes)
                if (base64Content.Length > 64 * 1024)
                {
                    AuthorizationEvent? authorizationEvent = JsonSerializer.Deserialize<AuthorizationEvent>(content, options);
                    
                    // Replace with a small JSON message and a GitHub link
                    var fallbackJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        message = "ContextRequestJson is not available due to size limitations.",
                        info = "See the following link for more details https://github.com/Altinn/altinn-authorization-tmp/issues/1858",
                    });

                    authorizationEvent.ContextRequestJson = fallbackJson;

                    base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(authorizationEvent, options)));
                }

                await client.SendMessageAsync(base64Content, null, timeToLive, cancellationToken);      
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
