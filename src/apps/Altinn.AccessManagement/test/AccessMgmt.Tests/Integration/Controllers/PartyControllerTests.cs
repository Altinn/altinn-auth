using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.Party;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ApiFixture already provides one host per IClassFixture<ApiFixture> instance,
// so the previous static shared-factory pattern is no longer needed.
namespace Altinn.AccessManagement.Tests.Integration.Controllers
{
    [IntegrationTest]
    public class PartyControllerTests : IClassFixture<ApiFixture>
    {
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient _client;
        private readonly ApiFixture _fixture;

        public PartyControllerTests(ApiFixture fixture)
        {
            _fixture = fixture;
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProviderMock>();
            });

            _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private HttpClient GetClient() => _client;

        [Fact]
        public async Task AddParty_InvalidIssuer_Returns401InvalidTokenIssuer()
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
        public async Task AddParty_InvalidAppClaim_Returns401InvalidAppClaim()
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
        public async Task AddParty_InvalidEntityType_Returns400EntityTypeNotSupported()
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
        public async Task AddParty_InvalidEntityVariantType_Returns400EntityVariantNotValidForEntityType()
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
        public async Task AddParty_ValidPartyStandardSystem_Returns201WithPartyCreatedResult()
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
        public async Task AddParty_ValidPartyAgentSystem_Returns201WithPartyCreatedResult()
        {
            var partyUuid = Guid.NewGuid();
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = partyUuid,
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

            await _fixture.QueryDb(async db =>
            {
                var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == partyUuid, TestContext.Current.CancellationToken);
                Assert.NotNull(entity);
                Assert.Equal(partyUuid.ToString(), entity.RefId);
            });
        }

        [Fact]
        public async Task AddParty_ValidPartySelfIdentifiedSiEmailWithRegisterToken_Returns201WithPartyCreatedResult()
        {
            var partyUuid = Guid.NewGuid();
            int partyUserId = 1000041;
            string emailIdentifier = "test@example.com";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = partyUuid,
                    EntityType = EntityTypeConstants.SelfIdentified.Entity.Name,
                    EntityVariantType = EntityVariantConstants.SI_EMAIL.Entity.Name,
                    DisplayName = "Self Identified Epost User",
                    PartyId = partyUserId,
                    EmailIdentifier = emailIdentifier,
                    UserId = partyUserId
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "register") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.True(result.PartyCreated);

            await _fixture.QueryDb(async db =>
            {
                var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == partyUuid, TestContext.Current.CancellationToken);
                Assert.NotNull(entity);
                Assert.Null(entity.RefId);
                Assert.Equal(partyUserId, entity.PartyId);
                Assert.Equal(partyUserId, entity.UserId);
                Assert.Equal(emailIdentifier, entity.EmailIdentifier);
            });
        }

        [Fact]
        public async Task AddParty_ValidPartySelfIdentifiedSiEduWithRegisterToken_Returns201WithPartyCreatedResult()
        {
            var partyUuid = Guid.NewGuid();
            int partyUserId = 1000042;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/accessmanagement/api/v1/internal/party")
            {
                Content = JsonContent.Create(new PartyBaseDto
                {
                    PartyUuid = partyUuid,
                    EntityType = EntityTypeConstants.SelfIdentified.Entity.Name,
                    EntityVariantType = EntityVariantConstants.SI_EDU.Entity.Name,
                    DisplayName = "Self Identified Educational User",
                    PartyId = partyUserId,
                    UserId = partyUserId
                }),
                Headers =
                {
                    { "PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "register") }
                }
            };

            HttpResponseMessage response = await GetClient().SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AddPartyResultDto>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.True(result.PartyCreated);

            await _fixture.QueryDb(async db =>
            {
                var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == partyUuid, TestContext.Current.CancellationToken);
                Assert.NotNull(entity);
                Assert.Null(entity.RefId);
                Assert.Equal(partyUserId, entity.PartyId);
                Assert.Equal(partyUserId, entity.UserId);
                Assert.Null(entity.EmailIdentifier);
            });
        }
    }
}
