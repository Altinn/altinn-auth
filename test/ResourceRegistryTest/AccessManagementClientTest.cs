using Microsoft.Extensions.Options;
using Moq.Protected;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.Extensions.Logging;
using Altinn.ResourceRegistry.Core.Configuration;
using Xunit;
using Altinn.ResourceRegistry.Core.Clients;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Tests.Mocks;

namespace Altinn.ResourceRegistry.Tests
{
    public class AccessManagementClientTest
    {
        public class PartiesWithInstancesClientTests
        {
            private readonly IOptions<PlatformSettings> _platformSettings;

            public PartiesWithInstancesClientTests()
            {
                _platformSettings = Options.Create(new PlatformSettings());
            }

            [Fact]
            public async Task AddResourceToAccessManagement_InputResource_Success()
            {
                // Arrange
                List<AccessManagementResource> requestData = new AccessManagementResource
                    {ResourceRegistryId = "test", ResourceType = "testType"}.ElementToList();

                _platformSettings.Value.AccessManagementEndpoint = "http://localhost:5117/accessmanagement/api/v1/internal/";
                _platformSettings.Value.AccessTokenIssuer = "UnitTest";

                HttpRequestMessage? requestMessage = null;
                List<AccessManagementResource> responseData = new AccessManagementResource
                {
                    ResourceRegistryId = "test", ResourceType = "testType", ResourceId = 3, Created = DateTime.Now,
                    Modified = DateTime.Now
                }.ElementToList();

                Mock<HttpMessageHandler> mockHttpMessageHandler = new Mock<HttpMessageHandler>();
                mockHttpMessageHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>((rm, ct) => requestMessage = rm)
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Created,
                        Content = JsonContent.Create(responseData)
                    });

                IAccessTokenGenerator accessTokenGenerator = new AccessTokenGeneratorMock();

                HttpClient httpClient = new HttpClient(mockHttpMessageHandler.Object);
                Mock<ILogger<AccessManagementClient>> logger = new Mock<ILogger<AccessManagementClient>>();

                AccessManagementClient target = new AccessManagementClient(httpClient, accessTokenGenerator, _platformSettings, logger.Object);

                // Act
                HttpResponseMessage result = await target.AddResourceToAccessManagement(requestData);

                // Assert
                Assert.NotNull(requestMessage);
                Assert.Equal("POST", requestMessage.Method.ToString());
                Assert.EndsWith("resources", requestMessage?.RequestUri?.ToString());
                Assert.Equal(HttpStatusCode.Created, result.StatusCode);

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                string jsonExpectedResult = JsonSerializer.Serialize(responseData, options);
                
                string jsonContent = await result.Content.ReadAsStringAsync();
                Assert.Equal(jsonExpectedResult, jsonContent);
            }
        }
    }
}
