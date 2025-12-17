using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Altinn.Common.AccessToken.Services;
using Altinn.Platform.Authorization.Controllers;
using Altinn.Platform.Authorization.IntegrationTests.MockServices;
using Altinn.Platform.Authorization.IntegrationTests.Util;
using Altinn.Platform.Authorization.IntegrationTests.Webfactory;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Platform.Authorization.IntegrationTests
{
    public class PartiesControllerTest : IClassFixture<CustomWebApplicationFactory<PartiesController>>
    {
        private readonly Mock<IFeatureManager> _featureManageMock = new Mock<IFeatureManager>();
        private readonly CustomWebApplicationFactory<PartiesController> _factory;
        private readonly HttpClient _client;
  
        public PartiesControllerTest(CustomWebApplicationFactory<PartiesController> fixture)
        {
            _factory = fixture;
            _featureManageMock
                .Setup(m => m.IsEnabledAsync("AccessManagementAuthorizedParties"))
                .Returns(Task.FromResult(true));
            _client = GetTestClient();
        }

        /// <summary>
        /// Test case: Get the party list for for the authenticated user.
        /// Expected: Should return status code 200 OK with the expected party list
        /// </summary>
        [Fact]
        public async Task GetPartyList_AsAuthenticatedUser_Ok()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage newResponse = await _client.GetAsync("authorization/api/v1/parties?userid=20000490");

            _featureManageMock
                .Setup(m => m.IsEnabledAsync("AccessManagementAuthorizedParties"))
                .Returns(Task.FromResult(false));

            HttpResponseMessage originalResponse = await _client.GetAsync("authorization/api/v1/parties?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, newResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, originalResponse.StatusCode);

            var originalPartiesList = await originalResponse.Content.ReadFromJsonAsync<List<Party>>();
            var newPartiesList = await newResponse.Content.ReadFromJsonAsync<List<Party>>();
            AssertionUtil.AssertCollections(originalPartiesList, newPartiesList, AssertionUtil.AssertParty);
        }

        /// <summary>
        /// Test case: Get the party list without specifying user id
        /// Expected: Should return 404 NotFound.
        /// </summary>
        [Fact]
        public async Task GetPartyList_WithoutUserQuery_NotFound()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Test case: Get the party list for a user not equal to the logged in user.
        /// Expected: Should return 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetPartyList_NotAsAuthenticatedUser_Forbidden()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties?userid=1337");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: Validate a party in the list of the authenticated user
        /// Expected: Should return status code 200 OK with body: true
        /// </summary>
        [Fact]
        public async Task ValidateParty_AsAuthenticatedUser_ValidParty_True()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50002598/validate?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await response.Content.ReadFromJsonAsync<bool>());
        }

        /// <summary>
        /// Test case: Validate a subunit party in the list of the authenticated user
        /// Expected: Should return status code 200 OK with body: true
        /// </summary>
        [Fact]
        public async Task ValidateParty_AsAuthenticatedUser_ValidPartySubUnit_True()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50074838/validate?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await response.Content.ReadFromJsonAsync<bool>());
        }

        /// <summary>
        /// Test case: Validate a party NOT in the list of the authenticated user
        /// Expected: Should return status code 200 OK with body: false
        /// </summary>
        [Fact]
        public async Task ValidateParty_AsAuthenticatedUser_NotValidParty_False()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/1337/validate?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(await response.Content.ReadFromJsonAsync<bool>());
        }

        /// <summary>
        /// Test case: Validate a party in the list of a different user than the authenticated user
        /// Expected: Should return status code 403 Forbidden
        /// </summary>
        [Fact]
        public async Task ValidateParty_NotAsAuthenticatedUser_Forbidden()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50002598/validate?userid=1337");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: Validate a party without specifying user id query param
        /// Expected: Should return status code 404 NotFound
        /// </summary>
        [Fact]
        public async Task ValidateParty_WithoutUserQuery_NotFound()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50002598/validate");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private HttpClient GetTestClient()
        {
            HttpClient client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                    services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProviderMock>();
                    services.AddSingleton<IParties, PartiesMock>();
                    services.AddSingleton<IAccessManagementWrapper, AccessManagementWrapperMock>();
                    services.AddSingleton(_featureManageMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            return client;
        }
    }
}
