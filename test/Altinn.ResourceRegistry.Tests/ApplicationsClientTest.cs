using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Clients;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Integration.Clients;
using Altinn.ResourceRegistry.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.Common;
using Moq;
using Moq.Protected;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Tests
{
    public class ApplicationsClientTest
    {
        private readonly IOptions<PlatformSettings> _platformSettings;
        public ApplicationsClientTest()
        {
            _platformSettings = Options.Create(new PlatformSettings());
        }

        [Fact]
        public async Task GetApplicationList_ReturnsApplications_WithoutMigratedResources_Success()
        {
            // Arrange
            _platformSettings.Value.StorageApiEndpoint = "http://localhost:5117/storage/api/v1/";
            HttpRequestMessage? requestMessage = null;
            
            Mock<HttpMessageHandler> mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((rm, ct) => requestMessage = rm)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(await GetApplicationsData())
                });
            HttpClient applicationsClient = new HttpClient(mockHttpMessageHandler.Object);
            ApplicationsClient target = new ApplicationsClient(applicationsClient, _platformSettings);
            // Act
            ApplicationList result = await target.GetApplicationList(false, cancellationToken:default);
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Applications);
            Assert.DoesNotContain(result.Applications, r => r.Id == "ssb/a1-1021-7048:1");
            Assert.DoesNotContain(result.Applications, r => r.Id == "skd/a2-4223-160201");
        }

        [Fact]
        public async Task GetApplicationList_ReturnsApplications_WithMigratedResources_Success()
        {
            // Arrange
            _platformSettings.Value.StorageApiEndpoint = "http://localhost:5117/storage/api/v1/";
            HttpRequestMessage? requestMessage = null;

            Mock<HttpMessageHandler> mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((rm, ct) => requestMessage = rm)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = JsonContent.Create(await GetApplicationsData())
                });
            HttpClient applicationsClient = new HttpClient(mockHttpMessageHandler.Object);
            ApplicationsClient target = new ApplicationsClient(applicationsClient, _platformSettings);
            // Act
            ApplicationList result = await target.GetApplicationList(true, cancellationToken: default);
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Applications);
            Assert.Contains(result.Applications, r => r.Id == "ssb/a1-1021-7048:1");
            Assert.Contains(result.Applications, r => r.Id == "skd/a2-4223-160201");
        }

        private async Task<ApplicationList> GetApplicationsData()
        {
            ApplicationList? applicationList = new ApplicationList();
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            if (unitTestFolder != null)
            {
                string testDataPath = Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Altinn3Storage", "applications.json");
                
                if (File.Exists(testDataPath))
                {
                    string content = await File.ReadAllTextAsync(testDataPath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        applicationList = System.Text.Json.JsonSerializer.Deserialize<ApplicationList>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                    }
                }
            }

            return applicationList;
        }
    }
}
