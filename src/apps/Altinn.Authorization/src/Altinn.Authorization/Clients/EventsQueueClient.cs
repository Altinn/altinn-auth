using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.EventLog;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace Altinn.Platform.Authorization.Clients
{
    /// <summary>
    /// Implementation of the <see ref="IEventsQueueClient"/> using Azure Storage Queues.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class EventsQueueClient : IEventsQueueClient
    {
        private static readonly int MAX_MESSAGE_LENGTH = (64 * 1024) / 2;

        private static readonly JsonElement FallbackContextRequestJson = JsonSerializer.SerializeToElement(new
        {
            message = "ContextRequestJson is not available due to size limitations.",
            info = "See the following link for more details https://github.com/Altinn/altinn-authorization-tmp/issues/1858",
        });

        private readonly QueueStorageSettings _settings;
        private readonly ILogger<EventsQueueClient> _logger;
        private static readonly RecyclableMemoryStreamManager _manager = new();

        /// <summary>
        /// Gets or sets the client used to enqueue and process raw authentication events.
        /// </summary>
        /// <remarks>This is used in testing to override the client used.</remarks>
        internal IRawEventsQueueClient AuthenticationEventQueueClient { get; set; }

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
        public async Task<QueuePostReceipt> EnqueueAuthorizationEvent(AuthorizationEvent authorizationEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = await GetAuthorizationEventQueueClient();
                TimeSpan timeToLive = TimeSpan.FromDays(_settings.TimeToLive);

                using var stream = _manager.GetStream();
                CompressAuthorizationEvent(stream, authorizationEvent);

                if (stream.Length > MAX_MESSAGE_LENGTH)
                {
                    Log.AuthorizationEventTooLarge(_logger, stream.Length);
                    authorizationEvent.ContextRequestJson = FallbackContextRequestJson;

                    stream.SetLength(0);
                    CompressAuthorizationEvent(stream, authorizationEvent);
                }

                var buffer = stream.GetBuffer();
                var encoded = DoubleBase64Encode(buffer, checked((int)stream.Length));
                var data = BinaryData.FromBytes(encoded);
                await client.SendMessageAsync(data, null, timeToLive, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                Log.OperationCanceled(_logger, ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(_logger, ex);
                return new QueuePostReceipt { Success = false, Exception = ex };
            }

            return new QueuePostReceipt { Success = true };
        }

        private async Task<IRawEventsQueueClient> GetAuthorizationEventQueueClient()
        {
            if (AuthenticationEventQueueClient == null)
            {
                var client = new QueueClient(_settings.EventLogConnectionString, _settings.AuthorizationEventQueueName);
                await client.CreateIfNotExistsAsync();

                AuthenticationEventQueueClient = new StorageQueueRawClient(client);
            }

            return AuthenticationEventQueueClient;
        }

        private void CompressAuthorizationEvent(Stream stream, AuthorizationEvent authorizationEvent)
        {
            stream.Write("01"u8 /* version header */);

            using (var compressor = new BrotliStream(stream, CompressionLevel.Fastest, leaveOpen: true))
            {
                JsonSerializer.Serialize(compressor, authorizationEvent, JsonSerializerOptions.Web);
            }

            stream.Position = 0;
        }

        private static ReadOnlyMemory<byte> DoubleBase64Encode(byte[] buffer, int length)
        {
            var base64Length = Base64.GetMaxEncodedToUtf8Length(Base64.GetMaxEncodedToUtf8Length(length));
            if (buffer.Length >= base64Length)
            {
                int bytesWritten;
                Check(Base64.EncodeToUtf8InPlace(buffer, length, out bytesWritten));

                length = bytesWritten;
                Check(Base64.EncodeToUtf8InPlace(buffer, length, out bytesWritten));

                return buffer.AsMemory(0, bytesWritten);
            }

            return EncodeToNewArray(buffer.AsSpan(0, length), base64Length);

            static ReadOnlyMemory<byte> EncodeToNewArray(ReadOnlySpan<byte> input, int base64Length)
            {
                byte[] buffer = new byte[base64Length];
                input.CopyTo(buffer);

                return DoubleBase64Encode(buffer, input.Length);
            }

            static void Check(OperationStatus status)
            {
                if (status != OperationStatus.Done)
                {
                    throw new InvalidOperationException("Base64 encoding failed.");
                }
            }
        }

        private sealed class StorageQueueRawClient : IRawEventsQueueClient
        {
            private readonly QueueClient _queueClient;

            public StorageQueueRawClient(QueueClient queueClient)
            {
                _queueClient = queueClient;
            }

            public async Task SendMessageAsync(
                BinaryData message,
                TimeSpan? visibilityTimeout = default,
                TimeSpan? timeToLive = default,
                CancellationToken cancellationToken = default)
            {
                await _queueClient.SendMessageAsync(message, visibilityTimeout, timeToLive, cancellationToken);
            }
        }

        private static partial class Log
        {
            [LoggerMessage(1, LogLevel.Warning, "Authorization event size {size} bytes exceeds maximum allowed size for queue messages.")]
            public static partial void AuthorizationEventTooLarge(ILogger logger, long size);

            [LoggerMessage(2, LogLevel.Warning, "Operation was canceled.")]
            public static partial void OperationCanceled(ILogger logger, Exception exception);

            [LoggerMessage(3, LogLevel.Error, "An error occurred while enqueuing authorization event.")]
            public static partial void Error(ILogger logger, Exception exception);
        }
    }

    public interface IRawEventsQueueClient
    {
        Task SendMessageAsync(
            BinaryData message,
            TimeSpan? visibilityTimeout = default,
            TimeSpan? timeToLive = default,
            CancellationToken cancellationToken = default);
    }
}
