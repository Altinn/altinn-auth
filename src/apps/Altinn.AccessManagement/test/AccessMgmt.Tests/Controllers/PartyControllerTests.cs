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
    public class PartyControllerTests : IClassFixture<WebApplicationFixture>
    {
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        // Static shared derived factory (and thus shared database) for all tests in this class.
        private static WebApplicationFactory<Program>? _sharedFactory;
        private static readonly object _factoryLock = new();

        public PartyControllerTests(WebApplicationFixture baseFixture)
        {
            // Initialize the derived factory only once. Subsequent test instances reuse it.
            if (_sharedFactory is null)
            {
                lock (_factoryLock)
                {
                    if (_sharedFactory is null)
                    {
                        _sharedFactory = baseFixture.WithWebHostBuilder(builder =>
                        {
                            builder.ConfigureTestServices(services =>
                            {
                                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                            });
                        });
                    }
                }
            }
        }

        private HttpClient GetClient()
        {
            // Host (and DB) already built on first call; subsequent calls reuse it.
            var client = _sharedFactory!.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        [Fact]
        public async Task AddParty_AuthorizationFail_InvalidIssuer()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Systembruker",
                    EntityVariantType = "StandardSystem",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("ttd", "authentication") } // Invalid issuer for this endpoint
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddParty_AuthorizationFail_InvalidAppClaim()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Organization",
                    EntityVariantType = "StandardSystem",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "unittest") } // Valid issuer, but invalid app claim for this endpoint
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddParty_AuthorizationOk_InvalidEntityType()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Organization", // Invalid entity type (at this time only allows for creating type: SystemUser)
                    EntityVariantType = "StandardSystem",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(
                await response.Content.ReadAsStringAsync(), _jsonOptions)!;
            Assert.Equal("The Entitytype is not supported", actual.Detail);
        }

        [Fact]
        public async Task AddParty_AuthorizationOk_InvalidEntityVariantType()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
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

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(
                await response.Content.ReadAsStringAsync(), _jsonOptions)!;
            Assert.Equal("The EntityVariant is not found or not valid for the given EntityType", actual.Detail);
        }

        [Fact]
        public async Task AddParty_ValidParty_StandardSystem_ReturnsOkAndTrue()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Systembruker",
                    EntityVariantType = "StandardSystem",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>();
            Assert.True(result!.PartyCreated);
        }

        [Fact]
        public async Task AddParty_ValidParty_AgentSystem_ReturnsOkAndTrue()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = Guid.NewGuid(),
                    EntityType = "Systembruker",
                    EntityVariantType = "AgentSystem",
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>();
            Assert.True(result!.PartyCreated);
        }
    }
}
