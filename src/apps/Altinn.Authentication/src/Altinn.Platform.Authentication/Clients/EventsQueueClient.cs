using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authentication.Clients.Interfaces;
using Altinn.Platform.Authentication.Configuration;
using Altinn.Platform.Authentication.Model;
using Azure;
using Azure.Core;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authentication.Clients
{
    /// <summary>
    /// Implementation of the <see ref="IEventsQueueClient"/> using Azure Storage Queues.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EventsQueueClient : IEventsQueueClient
    {
        /// <summary>
        /// Audit writes are awaited on the sign-in, refresh, logout and token exchange paths, so an
        /// unavailable queue must fail fast rather than hold the request through the SDK's default
        /// retries (minutes). The whole write, queue creation and send together, is cut off at this
        /// deadline; the caller logs the lost event and the request continues.
        /// </summary>
        private static readonly TimeSpan WriteDeadline = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Short retries keep a transient fault inside <see cref="WriteDeadline"/>.
        /// </summary>
        private static readonly QueueClientOptions QueueOptions = new()
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                MaxRetries = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                MaxDelay = TimeSpan.FromSeconds(1),
                NetworkTimeout = TimeSpan.FromSeconds(3),
            },
        };

        private readonly QueueStorageSettings _settings;

        private QueueClient _authenticationEventQueueClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventsQueueClient"/> class.
        /// </summary>
        /// <param name="settings">The queue storage settings</param>
        public EventsQueueClient(IOptions<QueueStorageSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<QueuePostReceipt> EnqueueAuthenticationEvent(string content)
        {
            using CancellationTokenSource deadline = new(WriteDeadline);

            try
            {
                QueueClient client = await GetAuthenticationEventQueueClient(deadline.Token);
                TimeSpan timeToLive = TimeSpan.FromDays(_settings.TimeToLive);
                await client.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(content)), null, timeToLive, deadline.Token);
            }
            catch (Exception ex)
            {
                // EventLogService logs the failure once, with the event type.
                return new QueuePostReceipt { Success = false, Exception = ex };
            }

            return new QueuePostReceipt { Success = true };
        }

        private async Task<QueueClient> GetAuthenticationEventQueueClient(CancellationToken cancellationToken)
        {
            if (_authenticationEventQueueClient == null)
            {
                QueueClient client = new QueueClient(_settings.EventLogConnectionString, _settings.AuthenticationEventQueueName, QueueOptions);
                await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                // Cache only once the queue exists, so a failed first attempt is retried on the next event.
                _authenticationEventQueueClient = client;
            }

            return _authenticationEventQueueClient;
        }
    }
}
