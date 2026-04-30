using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AccessMgmt.Tests.Mocks;
using AccessMgmt.Tests.Moqdata;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using static Altinn.AccessMgmt.Persistence.Services.Models.SystemUserClientConnectionDto;

namespace AccessMgmt.Tests.Controllers.Enterprise
{
    /// <summary>
    /// Tests for maskinporten controller for consent
    /// </summary>
    public class ConsentControllerTestEnterpriseFetchStatusChanges : IAsyncLifetime
    {
        private readonly Mock<IAmPartyRepository> _mockAmPartyRepository;
        private LegacyApiFixture _fixture = null!;
        private readonly ITestOutputHelper _output;

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.ResourceType ConsentResourceType = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000001"),
            Name = "Consent",
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Resource ResourceSkattegrunnlag = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000002"),
            Name = "Skattegrunnlag",
            Description = "Consent resource for skattegrunnlag",
            RefId = "ttd_skattegrunnlag",
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = ConsentResourceType.Id,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Resource ResourceInntektsopplysninger = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000003"),
            Name = "Inntektsopplysninger",
            Description = "Consent resource for inntektsopplysninger",
            RefId = "ttd_inntektsopplysninger",
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = ConsentResourceType.Id,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Resource ResourceSkattegrunnlag3 = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000004"),
            Name = "Skattegrunnlag3",
            Description = "Consent resource for skattegrunnlag3",
            RefId = "ttd_skattegrunnlag3",
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = ConsentResourceType.Id,
        };

        #region Test Entities

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity ElenaFjaerEntity = new()
        {
            Id = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"),
            Name = "ELENA FJÆR",
            RefId = "01025161013",
            PersonIdentifier = "01025161013",
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            PartyId = 513370001,
            UserId = null,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity SmekkFullBankEntity = new()
        {
            Id = Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181"),
            Name = "SmekkFull Bank AS",
            RefId = "810419512",
            OrganizationIdentifier = "810419512",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 501235,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity DigitaliseringsdirektoratetEntity = new()
        {
            Id = Guid.Parse("cdda2f11-95c5-4be4-9690-54206ff663f6"),
            Name = "DIGITALISERINGSDIREKTORATET",
            RefId = "991825827",
            OrganizationIdentifier = "991825827",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 501236,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity KolsaasOgFlaamEntity = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0005-000000004219"),
            Name = "KOLSAAS OG FLAAM",
            RefId = "810418192",
            OrganizationIdentifier = "810418192",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 50004219,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity LepsoyOgTonstadEntity = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0005-000000006078"),
            Name = "LEPSØY OG TONSTAD",
            RefId = "910493353",
            OrganizationIdentifier = "910493353",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 50006078,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity ConsentToOrgEntity = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0005-000000004646"),
            Name = "CONSENT TO ORG",
            RefId = "310419512",
            OrganizationIdentifier = "310419512",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 50004646,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity KariNordmannEntity = new()
        {
            Id = Guid.Parse("d47ac10b-58cc-4372-a567-0e02b2c3d483"),
            Name = "Kari Nordmann",
            RefId = "01025181049",
            PersonIdentifier = "01025181049",
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            PartyId = 501238,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity[] ConsentTestEntities =
        [
            ElenaFjaerEntity,
            SmekkFullBankEntity,
            DigitaliseringsdirektoratetEntity,
            KolsaasOgFlaamEntity,
            LepsoyOgTonstadEntity,
            ConsentToOrgEntity,
            KariNordmannEntity,
        ];

        #endregion

        public ConsentControllerTestEnterpriseFetchStatusChanges(ITestOutputHelper output)
        {
            _output = output;
            _mockAmPartyRepository = new Mock<IAmPartyRepository>();
        }

        public async ValueTask InitializeAsync()
        {            
            _fixture = new LegacyApiFixture();
            _fixture.ConfigureServices(services =>
            {
                // PlatformAccessToken / maskinporten tokens are signed by
                // {issuer}-org.pem; default PublicSigningKeyProviderMock only
                // accepts the static test key.
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();

                // Replace ApiFixture's default PermitPdpMock with the legacy
                // PdpPermitMock flavour used by these tests.
                services.RemoveAll<IPDP>();
                services.AddSingleton<IPDP, PdpPermitMock>();

                services.AddSingleton<IPartiesClient, PartiesClientMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IProfileClient, ProfileClientMock>();
                services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                services.AddSingleton<IPDP, PdpPermitMock>();

                // Register the SAME mock instance
                services.AddSingleton<IAmPartyRepository>(_mockAmPartyRepository.Object);
            });
            await _fixture.InitializeAsync();
            SeedResources();
            SetupMockPartyRepository();
            await CreateAndUpdateConsentsForGet(10);
        }

        /// <summary>
        /// Test: Getting consent status changes without authorization returns Unauthorized.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_NoToken_ReturnsUnauthorized()
        {
            HttpClient client = GetTestClient();

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges";
            HttpResponseMessage response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test: Getting consent status changes with wrong scope returns Forbidden.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_WrongScope_ReturnsForbidden()
        {
            HttpClient client = GetTestClient();

            // Use write scope instead of read scope
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.wite");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges";
            HttpResponseMessage response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test: Getting consent status changes returns OK with paginated data ordered newest first.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_ValidRequest_ReturnsOkWithDataOrderedNewestFirst()
        {
            HttpClient client = GetTestClient();
            Guid partyUuid = Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181");

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges?pageSize=5";
            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(responseContent);
            PaginatedResult<ConsentStatusChangeDto> result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent, _jsonOptions);
            AssertResponseForGetStatusChanges(result);
            if (result.Links.Next != null)
            {
                // Fetch next page
                var nextResponse = await client.GetAsync(result.Links.Next);
                PaginatedResult<ConsentStatusChangeDto> nextResult = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(await nextResponse.Content.ReadAsStringAsync(), _jsonOptions);
                AssertResponseForGetStatusChanges(nextResult);
            }
        }

        [Fact]
        public async Task GetConsentStatusChanges_Paging_DoesNotReturnOlderEventsForSameConsentRequest()
        {
            HttpClient client = GetTestClient();

            int numberOfConsents = 10;

            // Calculate expected numbers based on the updated helper logic
            int accepted = numberOfConsents / 2;
            int rejected = numberOfConsents - accepted;
            int revoked = Math.Min(3, accepted);

            // Fetch first page (pageSize=5)
            string readToken = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readToken);
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges?pageSize=5";
            HttpResponseMessage responsePage1 = await client.GetAsync(url);
            string responseContent1 = await responsePage1.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, responsePage1.StatusCode);
            var resultPage1 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent1, _jsonOptions);

            // The first page should have up to 5 items
            Assert.True(resultPage1.Items.Count() <= 5);
            var page1ConsentIds = resultPage1.Items.Select(i => i.ConsentRequestId).ToHashSet();

            // Fetch next page using continuation token
            if (resultPage1.Links.Next != null)
            {
                HttpResponseMessage responsePage2 = await client.GetAsync(resultPage1.Links.Next);
                string responseContent2 = await responsePage2.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.OK, responsePage2.StatusCode);
                var resultPage2 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent2, _jsonOptions);

                // The second page should have the remaining items
                Assert.Equal(numberOfConsents - resultPage1.Items.Count(), resultPage2.Items.Count());
                var page2ConsentIds = resultPage2.Items.Select(i => i.ConsentRequestId).ToHashSet();

                // No consentrequest should appear on both pages
                Assert.Empty(page1ConsentIds.Intersect(page2ConsentIds));

                // All returned events should be the latest for their consentrequest
                var allItems = resultPage1.Items.Concat(resultPage2.Items).ToList();
                Assert.Equal(numberOfConsents, allItems.Count);
                
                Assert.Equal(revoked, allItems.FindAll(i => i.EventType.Equals("revoked", StringComparison.OrdinalIgnoreCase)).Count());
                Assert.Equal(rejected, allItems.FindAll(i => i.EventType.Equals("rejected", StringComparison.OrdinalIgnoreCase)).Count());
                Assert.Equal(accepted - revoked, allItems.FindAll(i => i.EventType.Equals("accepted", StringComparison.OrdinalIgnoreCase)).Count());
            }
        }

        [Fact]
        public async Task GetConsentStatusChanges_IdenticalTimestamps_TieBreakerByEventId_OverPages()
        {
            HttpClient client = GetTestClient();

            int numberOfConsents = 10;

            string readToken = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readToken);

            // Fetch first page
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges?pageSize=5";
            HttpResponseMessage responsePage1 = await client.GetAsync(url);
            string responseContent1 = await responsePage1.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, responsePage1.StatusCode);
            var resultPage1 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent1, _jsonOptions);

            // Fetch next page
            Assert.NotNull(resultPage1.Links.Next);
            HttpResponseMessage responsePage2 = await client.GetAsync(resultPage1.Links.Next);
            string responseContent2 = await responsePage2.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, responsePage2.StatusCode);
            var resultPage2 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent2, _jsonOptions);

            // Combine all results
            var allItems = resultPage1.Items.Concat(resultPage2.Items).ToList();

            // 1. Assert all consentrequests are present (no data loss)
            Assert.Equal(numberOfConsents, allItems.Count);
            Assert.Equal(numberOfConsents, allItems.Select(i => i.ConsentRequestId).Distinct().Count());

            // 2. Assert correct ordering: by ChangedDate descending, then ConsentEventId descending
            for (int i = 1; i < allItems.Count; i++)
            {
                var prev = allItems[i - 1];
                var curr = allItems[i];
                int dateCompare = prev.ChangedDate.CompareTo(curr.ChangedDate);
                if (dateCompare > 0)
                {
                    // OK, previous is newer
                    continue;
                }
                else
                {
                    Assert.True(false, "Results are not ordered by ChangedDate descending.");
                }

                // NOTE: Tie-breaker by ConsentEventId cannot be tested as ConsentEventId is not exposed in the DTO.
                // Only ordering by ChangedDate descending is asserted here.
                //else if (dateCompare == 0)
                //{
                //    // Tie-breaker: ConsentEventId descending
                //    Assert.True(string.CompareOrdinal(prev.ConsentEventId.ToString(), curr.ConsentEventId.ToString()) > 0,
                //        $"ConsentEventId {prev.ConsentEventId} should be after {curr.ConsentEventId} when timestamps are equal.");
                //}
            }
        }

        [Fact]
        public async Task GetConsentStatusChanges_PageSizeOne_PaginatesCorrectly()
        {
            HttpClient client = GetTestClient();

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges?pageSize=1";
            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent, _jsonOptions);

            Assert.Single(result.Items);
            Assert.NotNull(result.Links.Next);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1001)]
        public async Task GetConsentStatusChanges_InvalidPageSize_ReturnsBadRequest(int pageSize)
        {
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/latestchanges?pageSize={pageSize}";
            HttpResponseMessage response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, _jsonOptions);

            Assert.Equal(1, problemDetails.Errors.Count);
            Assert.Contains(ValidationErrors.InvalidPageSizeForConsentStatusChanges.ErrorCode, problemDetails.Errors.Select(r => r.ErrorCode));
            Assert.Equal("Page size must be between 1 and 1000.", problemDetails.Errors.Select(r => r.Detail).FirstOrDefault());
        }

        private async Task CreateAndUpdateConsentsForGet(int numberOfConsents)
        {
            var createdConsentIds = new List<Guid>();
            for (int i = 0; i < numberOfConsents; i++)
            {
                Guid requestID = Guid.CreateVersion7();
                var consentRequest = new ConsentRequestDto
                {
                    Id = requestID,
                    From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                    To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                    ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                    ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                    RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                    RedirectUrl = "https://www.dnb.no"
                };

                HttpClient client = GetTestClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.write");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                StringContent stringContent = new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("/accessmanagement/api/v1/enterprise/consentrequests", stringContent);

                response.EnsureSuccessStatusCode();
                var created = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsDto>();
                createdConsentIds.Add(created.Id);
            }

            int take = numberOfConsents / 2; // Accept first half
            int skip = take;                 // Reject the rest
            int rejectTake = numberOfConsents - take; // Number to reject

            var acceptedConsentIdsToBeRevoked = new List<Guid>();
            foreach (var consentId in createdConsentIds.Take(take))
            {
                // Accept first 5
                string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);

                ConsentContextDto consentContextExternal = new ConsentContextDto
                {
                    Language = "nb",
                };

                // Serialize the object to JSON
                string jsonContent = JsonSerializer.Serialize(consentContextExternal);

                // Create HttpContent from the JSON string
                HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                HttpClient acceptClient = GetTestClient();
                acceptClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage acceptResponse = await acceptClient.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{consentId}/accept/", httpContent);
                acceptResponse.EnsureSuccessStatusCode();
                if (acceptedConsentIdsToBeRevoked.Count < 3)
                {
                    acceptedConsentIdsToBeRevoked.Add(consentId);
                }                   
            }

            foreach (var consentId in createdConsentIds.Skip(skip).Take(rejectTake))
            {
                HttpClient rejectClient = GetTestClient();
                IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
               
                string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
                rejectClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage rejectResponse = await rejectClient.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{consentId}/reject/", null);
                rejectResponse.EnsureSuccessStatusCode();
            }

            // Optionally revoke some
            foreach (var consentId in acceptedConsentIdsToBeRevoked)
            {
                Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
                HttpClient revokeClient = GetTestClient();
                string token = PrincipalUtil.GetToken(20001337, 50003899, 2, performedBy, AuthzConstants.SCOPE_PORTAL_ENDUSER);
                revokeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage revokeResponse = await revokeClient.PostAsync($"accessmanagement/api/v1/bff/consents/{consentId}/revoke/", null);
                revokeResponse.EnsureSuccessStatusCode();
            }
        }

        private void AssertResponseForGetStatusChanges(PaginatedResult<ConsentStatusChangeDto> result)
        {
            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            List<ConsentStatusChangeDto> items = result.Items.ToList();
            Assert.Equal(5, items.Count);

            // Verify ordering (newest first)
            Assert.True(items[0].ChangedDate > items[1].ChangedDate);
            Assert.True(items[1].ChangedDate > items[2].ChangedDate);
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private void SetupMockPartyRepository()
        {
            MockParyRepositoryPopulator.SetupMockPartyRepository(_mockAmPartyRepository);
        }

        private HttpClient GetTestClient()
        {
            HttpClient client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private void SeedResources()
        {
            using IServiceScope scope = _fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!db.ResourceTypes.AsNoTracking().Any(t => t.Id == ConsentResourceType.Id))
            {
                db.ResourceTypes.Add(ConsentResourceType);
                db.SaveChanges();
            }

            if (!db.Resources.AsNoTracking().Any(r => r.Id == ResourceSkattegrunnlag.Id))
            {
                db.Resources.Add(ResourceSkattegrunnlag);
            }

            if (!db.Resources.AsNoTracking().Any(r => r.Id == ResourceInntektsopplysninger.Id))
            {
                db.Resources.Add(ResourceInntektsopplysninger);
            }

            if (!db.Resources.AsNoTracking().Any(r => r.Id == ResourceSkattegrunnlag3.Id))
            {
                db.Resources.Add(ResourceSkattegrunnlag3);
            }

            foreach (Entity entity in ConsentTestEntities)
            {
                if (!db.Entities.AsNoTracking().Any(e => e.Id == entity.Id))
                {
                    db.Entities.Add(entity);
                }
            }

            db.SaveChanges();

            // Self-referencing PrivatePerson assignment for Elena Fjær
            if (!db.Assignments.AsNoTracking().Any(a => a.FromId == ElenaFjaerEntity.Id && a.ToId == ElenaFjaerEntity.Id && a.RoleId == RoleConstants.PrivatePerson.Id))
            {
                db.Assignments.Add(new Altinn.AccessMgmt.PersistenceEF.Models.Assignment
                {
                    FromId = ElenaFjaerEntity.Id,
                    ToId = ElenaFjaerEntity.Id,
                    RoleId = RoleConstants.PrivatePerson.Id,
                });
                db.SaveChanges();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _fixture.DisposeAsync();
        }
    }
}
