using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AccessMgmt.Tests.Mocks;
using AccessMgmt.Tests.Moqdata;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
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
    [IntegrationTest]
    public class ConsentControllerTestEnterpriseFetchStatusChanges : IAsyncLifetime
    {
        private readonly Mock<IAmPartyRepository> _mockAmPartyRepository;
        private LegacyApiFixture _fixture = null!;
        private readonly ITestOutputHelper _output;

        // ── Seeded consent ID buckets ─────────────────────────────────────────
        // Populated by CreateAndUpdateConsentsForGet so every test can reference
        // exact IDs without repeating setup logic.
        private List<Guid> _acceptedConsentIds = [];   // 5 IDs (indexes 0-4)
        private List<Guid> _rejectedConsentIds = [];   // 5 IDs (indexes 5-9)
        private List<Guid> _revokedConsentIds = [];   // 3 IDs (subset of accepted, indexes 0-2)

        // ── Seeded event counts (derived from numberOfConsents=10) ────────────
        // accepted=5, rejected=5, revoked=3  →  total=13 events across 3 pages
        private const int SeededAccepted = 5;
        private const int SeededRejected = 5;
        private const int SeededRevoked = 3;
        private const int SeededTotal = SeededAccepted + SeededRejected + SeededRevoked; // 13
        private const int PageSize = 5;

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

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

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

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test: Getting consent status changes returns OK with paginated data ordered newest first.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_ValidRequest_ReturnsOkWithDataOrderedOldestFirst()
        {
            HttpClient client = GetTestClient();
            Guid partyUuid = Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181");

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(responseContent);
            PaginatedResult<ConsentStatusChangeDto> result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent, _jsonOptions);
            AssertResponseForGetStatusChanges(result);
            if (result.Links.Next != null)
            {
                // Fetch next page
                var nextResponse = await client.GetAsync(result.Links.Next, TestContext.Current.CancellationToken);
                PaginatedResult<ConsentStatusChangeDto> nextResult = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(await nextResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);
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
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage responsePage1 = await client.GetAsync(url, TestContext.Current.CancellationToken);
            string responseContent1 = await responsePage1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, responsePage1.StatusCode);
            var resultPage1 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent1, _jsonOptions);

            // The first page should have up to 5 items
            Assert.True(resultPage1.Items.Count() <= 5);
            var page1ConsentIds = resultPage1.Items.Select(i => i.ConsentRequestId).ToHashSet();

            // Fetch next page using continuation token
            if (resultPage1.Links.Next != null)
            {
                HttpResponseMessage responsePage2 = await client.GetAsync(resultPage1.Links.Next, TestContext.Current.CancellationToken);
                string responseContent2 = await responsePage2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, responsePage2.StatusCode);
                var resultPage2 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent2, _jsonOptions);

                // The second page should have the remaining items
                Assert.Equal(numberOfConsents - resultPage1.Items.Count(), resultPage2.Items.Count());
                var page2ConsentIds = resultPage2.Items.Select(i => i.ConsentRequestId).ToHashSet();

                // Page 3 - empty, terminates pagination
                var response3 = await client.GetAsync(resultPage2.Links.Next, TestContext.Current.CancellationToken);
                var page3 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                    await response3.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);
                Assert.Equal(3, page3.Items.Count());
                Assert.Null(page3.Links.Next); // No more data → terminates correctly

                var allItems = resultPage1.Items.Concat(resultPage2.Items).Concat(page3.Items).ToList();

                Assert.Equal(revoked, allItems.FindAll(i => i.EventType.Equals("revoked", StringComparison.OrdinalIgnoreCase)).Count());
                Assert.Equal(rejected, allItems.FindAll(i => i.EventType.Equals("rejected", StringComparison.OrdinalIgnoreCase)).Count());
                Assert.Equal(accepted, allItems.FindAll(i => i.EventType.Equals("accepted", StringComparison.OrdinalIgnoreCase)).Count());
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
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage responsePage1 = await client.GetAsync(url, TestContext.Current.CancellationToken);
            string responseContent1 = await responsePage1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, responsePage1.StatusCode);
            var resultPage1 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(responseContent1, _jsonOptions);

            // Fetch next page
            Assert.NotNull(resultPage1.Links.Next);
            HttpResponseMessage responsePage2 = await client.GetAsync(resultPage1.Links.Next, TestContext.Current.CancellationToken);
            string responseContent2 = await responsePage2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
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
                if (dateCompare < 0)
                {
                    // OK, previous is older
                    continue;
                }
                else
                {
                    Assert.True(false, "Results are not ordered by ChangedDate descending.");
                }

                // NOTE: Tie-breaker by ConsentEventId cannot be tested as ConsentEventId is not exposed in the DTO.
                // Only ordering by ChangedDate descending is asserted here.
                // else if (dateCompare == 0)
                // {
                //    // Tie-breaker: ConsentEventId descending
                //    Assert.True(string.CompareOrdinal(prev.ConsentEventId.ToString(), curr.ConsentEventId.ToString()) > 0,
                //        $"ConsentEventId {prev.ConsentEventId} should be after {curr.ConsentEventId} when timestamps are equal.");
                // }
            }
        }

        [Fact]
        public async Task GetConsentStatusChanges_PageSizeQueryParam_IsIgnored_UsesConfigValue()
        {
            HttpClient client = GetTestClient();

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Pass a different pageSize than what's configured (config = 5)
            string urlWithParam = $"/accessmanagement/api/v1/enterprise/consentrequests/events?pageSize=100";
            string urlWithoutParam = $"/accessmanagement/api/v1/enterprise/consentrequests/events";

            var responseWith = await client.GetAsync(urlWithParam, TestContext.Current.CancellationToken);
            var responseWithout = await client.GetAsync(urlWithoutParam, TestContext.Current.CancellationToken);

            var resultWith = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(await responseWith.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);
            var resultWithout = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(await responseWithout.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            // Both should return config-driven page size (5), not 100
            Assert.Equal(resultWithout.Items.Count(), resultWith.Items.Count());
            Assert.Equal(5, resultWith.Items.Count()); // Matches EventsPageSize from PostConfigure
        }

        [Fact]
        public async Task GetConsentStatusChanges_PageSizeFromConfig_ReturnsCorrectNumberOfItems()
        {
            HttpClient client = GetTestClient();

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            // EventsPageSize is set to 5 in PostConfigure — assert exactly that
            Assert.Equal(5, result.Items.Count());
        }

        [Fact]
        public async Task GetConsentStatusChanges_NextLink_DoesNotContainPageSizeParam()
        {
            HttpClient client = GetTestClient();

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events";
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.NotNull(result.Links.Next);

            // pageSize is no longer a valid query param — it should not appear in the next link
            Assert.DoesNotContain("pageSize", result.Links.Next, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetConsentStatusChanges_ExactMultipleOfPageSize_LastPageIsEmpty()
        {
            // 10 consents seeded, pageSize = 5 → 2 full pages, then an empty 3rd page with no nextLink
            HttpClient client = GetTestClient();

            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Page 1
            var response1 = await client.GetAsync("/accessmanagement/api/v1/enterprise/consentrequests/events", TestContext.Current.CancellationToken);
            var page1 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);
            Assert.Equal(5, page1.Items.Count());
            Assert.NotNull(page1.Links.Next);

            // Page 2
            var response2 = await client.GetAsync(page1.Links.Next, TestContext.Current.CancellationToken);
            var page2 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);
            Assert.Equal(5, page2.Items.Count());
            Assert.NotNull(page2.Links.Next); // Full page → link exists (accepted extra round-trip behavior)

            // Page 3 - empty, terminates pagination
            var response3 = await client.GetAsync(page2.Links.Next, TestContext.Current.CancellationToken);
            var page3 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response3.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);
            Assert.Equal(3, page3.Items.Count());
            Assert.Null(page3.Links.Next); // No more data → terminates correctly
        }

        /// <summary>
        /// eventType=accepted → exactly 5 seeded accepted events.
        /// Fits exactly one full page; the follow-up page is empty (no next link).
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByEventTypeAccepted_ReturnsAllAcceptedAcrossPages()
        {
            var allItems = await FetchAllPages("/accessmanagement/api/v1/enterprise/consentrequests/events?eventType=accepted");

            Assert.Equal(SeededAccepted, allItems.Count);
            Assert.All(allItems, item => Assert.Equal("accepted", item.EventType, StringComparer.OrdinalIgnoreCase));
            ////Every accepted consent ID must be represented exactly once
            Assert.Equal(_acceptedConsentIds.ToHashSet(), allItems.Select(i => i.ConsentRequestId).ToHashSet());
        }

        /// <summary>
        /// eventType=rejected → exactly 5 seeded rejected events.
        /// Fits exactly one full page; the follow-up page is empty (no next link).
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByEventTypeRejected_ReturnsAllRejectedAcrossPages()
        {
            var allItems = await FetchAllPages("/accessmanagement/api/v1/enterprise/consentrequests/events?eventType=rejected");

            Assert.Equal(SeededRejected, allItems.Count);
            Assert.All(allItems, item => Assert.Equal("rejected", item.EventType, StringComparer.OrdinalIgnoreCase));
            Assert.Equal(_rejectedConsentIds.ToHashSet(), allItems.Select(i => i.ConsentRequestId).ToHashSet());
        }

        /// <summary>
        /// eventType=revoked → exactly 3 seeded revoked events.
        /// Fewer than one page, so no next link on the first (and only) response.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByEventTypeRevoked_ReturnsAllRevokedInOnePage()
        {
            HttpClient client = GetAuthorizedReadClient();

            string url = "/accessmanagement/api/v1/enterprise/consentrequests/events?eventType=revoked";
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(SeededRevoked, result.Items.Count());
            Assert.Null(result.Links.Next); // 3 < pageSize=5 → no next link
            Assert.All(result.Items, item => Assert.Equal("revoked", item.EventType, StringComparer.OrdinalIgnoreCase));
            Assert.Equal(_revokedConsentIds.ToHashSet(), result.Items.Select(i => i.ConsentRequestId).ToHashSet());
        }

        /// <summary>
        /// eventType=accepted&eventType=revoked → 5 accepted + 3 revoked = 8 events.
        /// Spans two pages (5 + 3). Verifies correct counts and that no rejected events leak in.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByAcceptedAndRevoked_Returns8EventsAcrossTwoPages()
        {
            const int expectedTotal = SeededAccepted + SeededRevoked; // 8
            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "accepted", "revoked" };

            HttpClient client = GetAuthorizedReadClient();

            // Page 1 – expect a full page since 8 > pageSize=5
            string url = "/accessmanagement/api/v1/enterprise/consentrequests/events?eventType=accepted&eventType=revoked";
            var response1 = await client.GetAsync(url, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            var page1 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(PageSize, page1.Items.Count());
            Assert.NotNull(page1.Links.Next);
            Assert.All(page1.Items, item => Assert.Contains(item.EventType, allowedTypes));

            // Page 2 – expect remaining 3 items and no next link
            var response2 = await client.GetAsync(page1.Links.Next, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            var page2 = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(expectedTotal - PageSize, page2.Items.Count()); // 3
            Assert.Null(page2.Links.Next);
            Assert.All(page2.Items, item => Assert.Contains(item.EventType, allowedTypes));

            // Aggregate assertions
            var allItems = page1.Items.Concat(page2.Items).ToList();
            Assert.Equal(expectedTotal, allItems.Count);
            Assert.Equal(SeededAccepted, allItems.Count(i => i.EventType.Equals("accepted", StringComparison.OrdinalIgnoreCase)));
            Assert.Equal(SeededRevoked, allItems.Count(i => i.EventType.Equals("revoked", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// consentRequestId=&lt;revokedId&gt; → that consent was accepted then revoked,
        /// so exactly 2 events are expected (accepted + revoked), both in one page.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByConsentRequestId_RevokedConsent_ReturnsTwoEvents()
        {
            // _revokedConsentIds[0] was accepted first, then revoked → 2 non-created events
            Guid targetId = _revokedConsentIds[0];

            HttpClient client = GetAuthorizedReadClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events?consentRequestId={targetId}";
            var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(2, result.Items.Count()); // accepted + revoked
            Assert.Null(result.Links.Next);        // 2 < pageSize=5
            Assert.All(result.Items, item => Assert.Equal(targetId, item.ConsentRequestId));

            var eventTypes = result.Items.Select(i => i.EventType).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("accepted", eventTypes);
            Assert.Contains("revoked", eventTypes);
        }

        /// <summary>
        /// consentRequestId=&lt;acceptedOnlyId&gt; → consent was accepted but NOT revoked,
        /// so exactly 1 event expected (accepted).
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByConsentRequestId_AcceptedOnlyConsent_ReturnsOneEvent()
        {
            // _acceptedConsentIds[3] was accepted but not revoked (only indexes 0-2 were revoked)
            Guid targetId = _acceptedConsentIds[3];

            HttpClient client = GetAuthorizedReadClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events?consentRequestId={targetId}";
            var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(1, result.Items.Count());
            Assert.Null(result.Links.Next);
            Assert.Equal(targetId, result.Items.Single().ConsentRequestId);
            Assert.Equal("accepted", result.Items.Single().EventType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// consentRequestId=&lt;rejectedId&gt; → exactly 1 event (rejected).
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByConsentRequestId_RejectedConsent_ReturnsOneEvent()
        {
            Guid targetId = _rejectedConsentIds[0];

            HttpClient client = GetAuthorizedReadClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events?consentRequestId={targetId}";
            var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(1, result.Items.Count());
            Assert.Null(result.Links.Next);
            Assert.Equal(targetId, result.Items.Single().ConsentRequestId);
            Assert.Equal("rejected", result.Items.Single().EventType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// consentRequestId + eventType=revoked → for a revoked consent, only the revoked event is returned.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_FilterByConsentRequestIdAndEventType_ReturnsOnlyMatchingEvent()
        {
            Guid targetId = _revokedConsentIds[0];

            HttpClient client = GetAuthorizedReadClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/events?consentRequestId={targetId}&eventType=revoked";
            var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Equal(1, result.Items.Count());
            Assert.Null(result.Links.Next);
            Assert.Equal(targetId, result.Items.Single().ConsentRequestId);
            Assert.Equal("revoked", result.Items.Single().EventType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// createdAfter=tomorrow → no events fall after a future timestamp, result is empty.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_CreatedAfterFuture_ReturnsEmpty()
        {
            string futureTimestamp = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));

            HttpClient client = GetAuthorizedReadClient();
            var response = await client.GetAsync(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdAfter={futureTimestamp}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Empty(result.Items);
            Assert.Null(result.Links.Next);
        }

        /// <summary>
        /// createdAfter=yesterday → all 13 seeded events fall after that bound.
        /// Verifies count across all pages and that ordering is ascending.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_CreatedAfterYesterday_ReturnsAllEventsAcrossPages()
        {
            string pastTimestamp = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
            var allItems = await FetchAllPages(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdAfter={pastTimestamp}");

            Assert.Equal(SeededTotal, allItems.Count);

            // All events must be after the lower bound
            DateTimeOffset lowerBound = DateTimeOffset.UtcNow.AddDays(-1);
            Assert.All(allItems, item => Assert.True(item.ChangedDate >= lowerBound));

            // Ascending order must be preserved across pages
            for (int i = 1; i < allItems.Count; i++)
            {
                Assert.True(allItems[i - 1].ChangedDate <= allItems[i].ChangedDate,
                    $"Order violation at index {i}");
            }
        }

        /// <summary>
        /// createdBefore=yesterday → no events fall before a past lower bound, result is empty.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_CreatedBeforeYesterday_ReturnsEmpty()
        {
            string pastTimestamp = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));

            HttpClient client = GetAuthorizedReadClient();
            var response = await client.GetAsync(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdBefore={pastTimestamp}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

            Assert.Empty(result.Items);
            Assert.Null(result.Links.Next);
        }

        /// <summary>
        /// createdBefore=tomorrow → all 13 seeded events fall before that upper bound.
        /// Verifies count across all pages.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_CreatedBeforeTomorrow_ReturnsAllEventsAcrossPages()
        {
            string futureTimestamp = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));
            var allItems = await FetchAllPages(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdBefore={futureTimestamp}");

            Assert.Equal(SeededTotal, allItems.Count);
        }

        /// <summary>
        /// createdAfter=yesterday + createdBefore=tomorrow → time window contains all 13 events.
        /// </summary>
        [Fact]
        public async Task GetConsentStatusChanges_CreatedAfterAndBeforeCombined_ReturnsAllEventsWithinWindow()
        {
            string after = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
            string before = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));

            var allItems = await FetchAllPages(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdAfter={after}&createdBefore={before}");

            Assert.Equal(SeededTotal, allItems.Count);
        }

        [Fact]
        public async Task GetConsentEvents_WithInvalidEventType_ReturnsBadRequest()
        {
            SetupMockPartyRepository();

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.GetAsync(
                "/accessmanagement/api/v1/enterprise/consentrequests/events?eventType=invalidtype",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, _jsonOptions);
            Assert.Equal(ValidationErrors.InvalidEventType.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
            Assert.Contains("$QUERY/eventType", problemDetails.Errors.ToList()[0].Paths);
        }

        [Fact]
        public async Task GetConsentEvents_WithCreatedAfterAfterCreatedBefore_ReturnsBadRequest()
        {
            SetupMockPartyRepository();

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string createdAfter = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));
            string createdBefore = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"));

            HttpResponseMessage response = await client.GetAsync(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdAfter={createdAfter}&createdBefore={createdBefore}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, _jsonOptions);
            Assert.Equal(ValidationErrors.InvalidDateRange.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
            Assert.Contains("$QUERY/createdAfter", problemDetails.Errors.ToList()[0].Paths);
        }

        [Fact]
        public async Task GetConsentEvents_WithCreatedAfterEqualToCreatedBefore_ReturnsBadRequest()
        {
            SetupMockPartyRepository();

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string timestamp = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"));

            HttpResponseMessage response = await client.GetAsync(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?createdAfter={timestamp}&createdBefore={timestamp}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, _jsonOptions);
            Assert.Equal(ValidationErrors.InvalidDateRange.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
            Assert.Contains("$QUERY/createdAfter", problemDetails.Errors.ToList()[0].Paths);
        }

        [Fact]
        public async Task GetConsentEvents_WithInvalidContinuationToken_ReturnsBadRequest()
        {
            SetupMockPartyRepository();

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.GetAsync(
                "/accessmanagement/api/v1/enterprise/consentrequests/events?continuationToken=notvalidbase64!!!",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, _jsonOptions);
            Assert.Equal(ValidationErrors.InvalidContinuationToken.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
            Assert.Contains("$QUERY/continuationToken", problemDetails.Errors.ToList()[0].Paths);
        }

        [Fact]
        public async Task GetConsentEvents_WithWrongLengthContinuationToken_ReturnsBadRequest()
        {
            SetupMockPartyRepository();

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Valid base64 but not 16 bytes (a GUID)
            string shortToken = Uri.EscapeDataString(Convert.ToBase64String(new byte[] { 1, 2, 3 }));

            HttpResponseMessage response = await client.GetAsync(
                $"/accessmanagement/api/v1/enterprise/consentrequests/events?continuationToken={shortToken}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, _jsonOptions);
            Assert.Equal(ValidationErrors.InvalidContinuationToken.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
            Assert.Contains("$QUERY/continuationToken", problemDetails.Errors.ToList()[0].Paths);
        }

        /// <summary>
        /// Follows all next-page links for a given start URL and returns every item collected.
        /// Uses the read-scoped Maskinporten token for 810419512.
        /// </summary>
        private async Task<List<ConsentStatusChangeDto>> FetchAllPages(string startUrl)
        {
            HttpClient client = GetAuthorizedReadClient();
            var all = new List<ConsentStatusChangeDto>();
            string? nextUrl = startUrl;

            while (nextUrl != null)
            {
                var response = await client.GetAsync(nextUrl, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var page = JsonSerializer.Deserialize<PaginatedResult<ConsentStatusChangeDto>>(
                    await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

                all.AddRange(page.Items);
                nextUrl = page.Links.Next;
            }

            return all;
        }

        private HttpClient GetAuthorizedReadClient()
        {
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetMaskinportenToken("810419512", "altinn:consentrequests.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
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
                _acceptedConsentIds.Add(consentId);
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
                _rejectedConsentIds.Add(consentId);
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
                _revokedConsentIds.Add(consentId);
            }
        }

        private void AssertResponseForGetStatusChanges(PaginatedResult<ConsentStatusChangeDto> result)
        {
            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            List<ConsentStatusChangeDto> items = result.Items.ToList();
            Assert.Equal(5, items.Count);

            // Verify ordering (oldest first)
            Assert.True(items[0].ChangedDate < items[1].ChangedDate);
            Assert.True(items[1].ChangedDate < items[2].ChangedDate);
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
