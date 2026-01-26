using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.EventLog;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.Authorization.Tests
{
    public class EventsQueueClientTests
    {
        private EventsQueueClient CreateClient()
        {
            var settings = Options.Create(new QueueStorageSettings
            {
                EventLogConnectionString = "UseDevelopmentStorage=true",
                AuthorizationEventQueueName = "testqueue",
                TimeToLive = 1
            });
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EventsQueueClient>.Instance;
            var client = new EventsQueueClient(settings, logger);
            return client;
        }

        [Fact]
        public async Task EnqueueAuthorizationEvent_SendsCompressedEventToQueue()
        {
            // Arrange
            var mockQueueClient = new Mock<IRawEventsQueueClient>();
            byte[]? sentData = null;
            mockQueueClient
                .Setup(q => q.SendMessageAsync(
                    It.IsAny<BinaryData>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BinaryData, TimeSpan?, TimeSpan?, CancellationToken>((data, _, _, _) => sentData = RawData(data))
                .Returns(Task.CompletedTask);

            var client = CreateClient();
            client.AuthenticationEventQueueClient = mockQueueClient.Object;

            var evt = new AuthorizationEvent
            {
                Operation = "read",
                ContextRequestJson = JsonSerializer.Deserialize<JsonElement>("""{"foo":"bar"}""")
            };

            // Act
            var receipt = await client.EnqueueAuthorizationEvent(evt);

            // Assert
            Assert.True(receipt.Success);
            Assert.NotNull(sentData);

            // 1. Check the version header
            Assert.True(sentData.Length > 2, "Data should be at least 3 bytes (2 header + data)");
            Assert.Equal((byte)'0', sentData[0]);
            Assert.Equal((byte)'1', sentData[1]);

            // 2. Decompress the payload (skip the first two bytes)
            using var ms = new MemoryStream(sentData, 2, sentData.Length - 2);
            using var brotli = new BrotliStream(ms, CompressionMode.Decompress);
            var decompressed = JsonSerializer.Deserialize<AuthorizationEvent>(brotli, JsonSerializerOptions.Web);

            // 3. Verify the decompressed content
            Assert.Equal(evt.Operation, decompressed.Operation);
            Assert.True(JsonElement.DeepEquals(evt.ContextRequestJson, decompressed.ContextRequestJson));
        }

        [Fact]
        public async Task EnqueueAuthorizationEvent_LargeJson_TriggersFallbackAndSendsFallbackMessage()
        {
            // Arrange
            var mockQueueClient = new Mock<IRawEventsQueueClient>();
            byte[]? sentData = null;
            mockQueueClient
                .Setup(q => q.SendMessageAsync(
                    It.IsAny<BinaryData>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BinaryData, TimeSpan?, TimeSpan?, CancellationToken>((data, _, _, _) => sentData = RawData(data))
                .Returns(Task.CompletedTask);

            var client = CreateClient();
            client.AuthenticationEventQueueClient = mockQueueClient.Object;

            // Create a large JSON array to exceed the 64KB limit after compression
            var values = Enumerable.Range(0, 70_000).ToArray();
            var largeJson = JsonSerializer.Serialize(values);
            var evt = new AuthorizationEvent
            {
                Operation = "write",
                ContextRequestJson = JsonSerializer.Deserialize<JsonElement>(largeJson)
            };

            // Act
            var receipt = await client.EnqueueAuthorizationEvent(evt);

            // Assert
            Assert.True(receipt.Success);
            Assert.NotNull(sentData);

            //var bytes = sentData.ToArray();

            // Decompress the payload (skip the first two bytes for version header)
            using var ms = new MemoryStream(sentData, 2, sentData.Length - 2);
            using var brotli = new BrotliStream(ms, CompressionMode.Decompress);
            var decompressed = JsonSerializer.Deserialize<AuthorizationEvent>(brotli, JsonSerializerOptions.Web);

            // The fallback message should be present
            Assert.Equal(evt.Operation, decompressed.Operation);
            Assert.True(decompressed.ContextRequestJson.TryGetProperty("message", out var msg));
            Assert.Contains("ContextRequestJson is not available", msg.GetString());
        }

        [Fact]
        public async Task EnqueueAuthorizationEvent_LargeButCompressibleJson_DoesNotTriggerFallback()
        {
            // Arrange
            var mockQueueClient = new Mock<IRawEventsQueueClient>();
            byte[]? sentData = null;
            mockQueueClient
                .Setup(q => q.SendMessageAsync(
                    It.IsAny<BinaryData>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BinaryData, TimeSpan?, TimeSpan?, CancellationToken>((data, _, _, _) => sentData = RawData(data))
                .Returns(Task.CompletedTask);

            var client = CreateClient();
            client.AuthenticationEventQueueClient = mockQueueClient.Object;

            // Create a large, highly compressible JSON
            var obj = new { foo = "bar", baz = 123 };
            var values = Enumerable.Repeat(obj, 20000).ToArray();
            var largeCompressibleJson = JsonSerializer.Serialize(values);
            var evt = new AuthorizationEvent
            {
                Operation = "compressible",
                ContextRequestJson = JsonSerializer.Deserialize<JsonElement>(largeCompressibleJson)
            };

            // Act
            var receipt = await client.EnqueueAuthorizationEvent(evt);

            // Assert
            Assert.True(receipt.Success);
            Assert.NotNull(sentData);

            // Decompress the payload (skip the first two bytes for version header)
            using var ms = new MemoryStream(sentData, 2, sentData.Length - 2);
            using var brotli = new BrotliStream(ms, CompressionMode.Decompress);
            var decompressed = JsonSerializer.Deserialize<AuthorizationEvent>(brotli, JsonSerializerOptions.Web);
           
            Assert.Equal(evt.Operation, decompressed.Operation);
            
            // Ensure the original JSON is preserved (fallback was NOT triggered)
            Assert.True(JsonElement.DeepEquals(evt.ContextRequestJson, decompressed.ContextRequestJson));
        }

        private static byte[] RawData(BinaryData data)
        {
            var str = Encoding.UTF8.GetString(data.ToMemory().Span);
            var decoded = Convert.FromBase64String(str);
            return decoded;
        }
    }
}
