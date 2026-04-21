using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AccessMgmt.Tests.Moqdata;
using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;

// Migrated from WebApplicationFixture to LegacyApiFixture as part of
// sub-step 16.4a (Phase 2.2). The consent flow goes through the Dapper-backed
// ConsentRepository which binds to the Yuniql-provisioned consent.status_type
// enum; LegacyApiFixture overlays Yuniql on top of ApiFixture's EF schemas.
namespace AccessMgmt.Tests.Controllers.MaskinPorten
{
    /// <summary>
    /// Tests for maskinporten controller for consent
    /// </summary>
    public class ConsentControllerTest : IClassFixture<LegacyApiFixture>
    {
        private readonly Mock<IAmPartyRepository> _mockAmPartyRepository;
        private readonly ApiFixture _fixture;
        private readonly ITestOutputHelper _output;

        public ConsentControllerTest(LegacyApiFixture fixture, ITestOutputHelper output)
        {
            _mockAmPartyRepository = new Mock<IAmPartyRepository>();
            _output = output;
            _fixture = fixture;

            fixture.ConfigureServices(services =>
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
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                services.AddSingleton<IAltinn2ConsentClient, Altinn2ConsentClientMock>();
                services.AddSingleton<IProfileClient, ProfileClientMock>();

                // Register the SAME mock instance
                services.AddSingleton<IAmPartyRepository>(_mockAmPartyRepository.Object);
            });
        }

        private void SetupMockPartyRepository()
        {
            MockParyRepositoryPopulator.SetupMockPartyRepository(_mockAmPartyRepository);
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [Fact]
        public async Task GetConsentFromA2_Valid()
        {
            SetupMockPartyRepository();

            Guid requestId = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf1");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new ConsentLookupDto()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            var task = await repositgo.GetRequest(requestId, default);
            string responseContent = await response.Content.ReadAsStringAsync();
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(requestId == consentInfo.Id);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
        }

        [Fact]
        public async Task GetConsentFromA2_Expired()
        {
            SetupMockPartyRepository();

            Guid requestId = Guid.Parse("4a73a516-7a91-435c-8a0e-0f4659588594");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new ConsentLookupDto()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            var task = await repositgo.GetRequest(requestId, default);
            string responseContent = await response.Content.ReadAsStringAsync();
            ProblemDetails problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal("Consent is expired", problemDetails.Detail);
        }

        [Fact]
        public async Task GetConsentFromA2_InvalidMetadata()
        {
            SetupMockPartyRepository();

            Guid requestId = Guid.Parse("4a73a516-7a91-435c-8a0e-0f4659588595");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new ConsentLookupDto()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            var task = await repositgo.GetRequest(requestId, default);
            string responseContent = await response.Content.ReadAsStringAsync();
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(requestId == consentInfo.Id);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
        }

        [Fact]
        public async Task GetConsent_CreatedExpired_BadRequest()
        {
            SetupMockPartyRepository();

            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new ConsentLookupDto()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal(StdProblemDescriptors.ErrorCodes.MultipleProblems, problemDetails.ErrorCode);
            Assert.Equal(2, problemDetails.Problems.Count());
            Assert.Equal(Problems.ConsentExpired.ErrorCode, problemDetails.Problems.ToList()[0].ErrorCode);
            Assert.Equal(Problems.ConsentNotAccepted.ErrorCode, problemDetails.Problems.ToList()[1].ErrorCode);
        }

        [Fact]
        public async Task GetConsent_ValidFromOrg()
        {
            SetupMockPartyRepository();
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed77");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentRequest request = await GetRequest(requestId);
            request.ValidTo = DateTime.UtcNow.AddDays(10);
            await repositgo.CreateRequest(request, Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, Guid.NewGuid(), consentContextExternal.ToConsentContext());

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new ConsentLookupDto()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("910493353")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            string responseContent = await response.Content.ReadAsStringAsync();
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(DateTime.UtcNow.AddDays(-2) < consentInfo.Consented);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("910493353")), consentInfo.From);
        }

        /// <summary>
        /// In this scenario the to party matches the handled by party, and the from party is an org. 
        /// This should be allowed
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetConsent_ValidHandledByOrg()
        {
            SetupMockPartyRepository();
            Guid requestId = Guid.Parse("d1bedb7d-a682-4668-9f84-7a56b3d733ab");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentRequest request = await GetRequest(requestId);
            request.ValidTo = DateTime.UtcNow.AddDays(10);
            await repositgo.CreateRequest(request, Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, Guid.NewGuid(), consentContextExternal.ToConsentContext());

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("910493353")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            string responseContent = await response.Content.ReadAsStringAsync();
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(DateTime.UtcNow.AddDays(-2) < consentInfo.Consented);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("910493353")), consentInfo.From);
        }

        [Fact]
        public async Task GetConsent_Created_BadRequest()
        {
            SetupMockPartyRepository();
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed46");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentRequest request = await GetRequest(requestId);
            request.ValidTo = DateTime.UtcNow.AddDays(10);
            await repositgo.CreateRequest(request, Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/maskinporten/consent/lookup/";

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:maskinporten/consent.read");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentLookupDto consentLookup = new ConsentLookupDto()
            {
                Id = requestId,
                From = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512"))
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal(Problems.ConsentNotAccepted.ErrorCode, problemDetails.ErrorCode);
        }

        private HttpClient GetTestClient()
        {
            HttpClient client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private Task<ConsentRequest> GetRequest(Guid id)
        {
            Stream dataStream = File.OpenRead($"Data/Consent/consent_request_{id.ToString()}.json");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            ConsentRequest result = JsonSerializer.Deserialize<ConsentRequest>(dataStream, options);
            return Task.FromResult(result);
        }
    }
}
