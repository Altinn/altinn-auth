using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Authorization.Api.Contracts.Party;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.AccessManagement.Api.Internal.IntegrationTests.Controllers
{
    [Collection("Internal PartyController Test")]
    public class PartyControllerTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private WebApplicationFactory<Program> Fixture { get; } = fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
            });
        });

        private HttpClient GetClient()
        {
            HttpClient client = Fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        [Fact]
        public async Task AddParty_AuthorizationFail_InvalidIssuer()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Systembruker",
                    EntityVariantType = "System",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("ttd", "authentication") } // Invalid issuer for this endpoint
                }
            };

            // Act
            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddParty_AuthorizationFail_InvalidAppClaim()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Organization",
                    EntityVariantType = "System",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "unittest") } // Valid issuer, but invalid app claim for this endpoint
                }
            };

            // Act
            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddParty_AuthorizationOk_InvalidEntityType()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Organization", // Invalid entity type (at this time only allows for creating type: SystemUser)
                    EntityVariantType = "System",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            // Act
            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(await response.Content.ReadAsStringAsync(), options);
            Assert.Equal("The Entitytype is not supported", actual.Detail);
        }

        [Fact]
        public async Task AddParty_AuthorizationOk_InvalidEntityVariantType()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Systembruker",
                    EntityVariantType = "BEDR", // Invalid variant type for SystemUser
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            // Act
            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(await response.Content.ReadAsStringAsync(), options);
            Assert.Equal("The EntityVariant is not found or not valid for the given EntityType", actual.Detail);
        }

        [Fact]
        public async Task AddParty_ValidParty_ReturnsOkAndTrue()
        {
            // Arrange
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Systembruker",
                    EntityVariantType = "System",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            // Act
            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>();
            Assert.True(result.PartyCreated);
        }
    }
}
