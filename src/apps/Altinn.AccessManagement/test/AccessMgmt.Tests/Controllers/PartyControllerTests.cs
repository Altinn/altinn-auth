using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.Party;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// Migrated from WebApplicationFixture to ApiFixture as part of Phase 2.2
// Sub-step 16.3 (Step 16 — AccessMgmt.Tests WAF consolidation). The
// static shared-factory pattern is no longer needed — ApiFixture already
// provides one host per IClassFixture<ApiFixture> instance.
namespace Altinn.AccessManagement.Api.Internal.IntegrationTests.Controllers
{
    public class PartyControllerTests : IClassFixture<ApiFixture>
    {
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient _client;

        public PartyControllerTests(ApiFixture fixture)
        {
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
            });

            _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private HttpClient GetClient() => _client;

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

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
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

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
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

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions)!;
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
                    EntityType = EntityTypeConstants.SystemUser.Entity.Name,
                    EntityVariantType = "BEDR", // Invalid variant type for SystemUser
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions)!;
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
                    EntityType = EntityTypeConstants.SystemUser.Entity.Name,
                    EntityVariantType = EntityVariantConstants.StandardSystem.Entity.Name,
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>(cancellationToken: TestContext.Current.CancellationToken);
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
                    EntityType = EntityTypeConstants.SystemUser.Entity.Name,
                    EntityVariantType = EntityVariantConstants.AgentSystem.Entity.Name,
                    DisplayName = "Test User"
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "authentication") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(result!.PartyCreated);
        }
    }
}
