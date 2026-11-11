using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authentication.Integration.Configuration;
using Altinn.Platform.Authentication.Core.Models.ResourceRegistry;
using Altinn.Platform.Authentication.Integration.ResourceRegister;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Altinn.Platform.Authentication.Tests.Clients
{
    /// <summary>
    /// Tests for <see cref="ResourceRegistryClient"/>, covering that resource lookups are cached (issue #4571).
    /// </summary>
    public class ResourceRegistryClientTests
    {
        private const string ResourceJson = "{\"identifier\":\"ske-krav-og-betalinger\",\"delegable\":true}";

        [Fact]
        public async Task GetResource_CalledTwice_CallsResourceRegistryOnce()
        {
            // Arrange
            Mock<HttpMessageHandler> handler = CreateHandler(HttpStatusCode.OK, ResourceJson);
            ResourceRegistryClient client = CreateClient(handler);

            // Act
            ServiceResource? first = await client.GetResource("ske-krav-og-betalinger");
            ServiceResource? second = await client.GetResource("ske-krav-og-betalinger");

            // Assert
            Assert.NotNull(first);
            Assert.Equal("ske-krav-og-betalinger", first.Identifier);
            Assert.Same(first, second);
            VerifyCalls(handler, Times.Once());
        }

        [Fact]
        public async Task GetResource_DifferentResources_AreCachedSeparately()
        {
            // Arrange
            Mock<HttpMessageHandler> handler = CreateHandler(HttpStatusCode.OK, ResourceJson);
            ResourceRegistryClient client = CreateClient(handler);

            // Act
            await client.GetResource("ske-krav-og-betalinger");
            await client.GetResource("ske-krav-og-betalinger-2");

            // Assert
            VerifyCalls(handler, Times.Exactly(2));
        }

        [Fact]
        public async Task GetResource_NotFound_IsNotCached()
        {
            // Arrange
            Mock<HttpMessageHandler> handler = CreateHandler(HttpStatusCode.NotFound, string.Empty);
            ResourceRegistryClient client = CreateClient(handler);

            // Act
            ServiceResource? first = await client.GetResource("unknown-resource");
            ServiceResource? second = await client.GetResource("unknown-resource");

            // Assert
            Assert.Null(first);
            Assert.Null(second);
            VerifyCalls(handler, Times.Exactly(2));
        }

        private static ResourceRegistryClient CreateClient(Mock<HttpMessageHandler> handler)
        {
            PlatformSettings platformSettings = new()
            {
                ApiResourceRegistryEndpoint = "https://platform.test/resourceregistry/api/v1/"
            };

            return new ResourceRegistryClient(
                new HttpClient(handler.Object),
                NullLogger<ResourceRegistryClient>.Instance,
                new Mock<IHttpContextAccessor>().Object,
                Options.Create(platformSettings),
                new MemoryCache(new MemoryCacheOptions()));
        }

        private static Mock<HttpMessageHandler> CreateHandler(HttpStatusCode statusCode, string responseBody)
        {
            Mock<HttpMessageHandler> handler = new();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });

            return handler;
        }

        private static void VerifyCalls(Mock<HttpMessageHandler> handler, Times times)
        {
            handler
                .Protected()
                .Verify("SendAsync", times, ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }
}
