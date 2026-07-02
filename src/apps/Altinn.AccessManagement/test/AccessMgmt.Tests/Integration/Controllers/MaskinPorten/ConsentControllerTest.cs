using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Moqdata;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

// Uses LegacyApiFixture: the consent flow goes through the Dapper-backed
// ConsentRepository which binds to the consent.status_type enum. The consent
// schema is provisioned by EF Core (ConsentSchema_Baseline); LegacyApiFixture
// additionally overlays the Yuniql accessmanagement/delegation schemas.
namespace Altinn.AccessManagement.Tests.Integration.Controllers.MaskinPorten
{
    /// <summary>
    /// Tests for maskinporten controller for consent
    /// </summary>
    [IntegrationTest]
    [Collection(ConsentDbCollection.Name)]
    public class ConsentControllerTest
    {
        private readonly ConsentApiFixture _fixture;
        private readonly ITestOutputHelper _output;

        public ConsentControllerTest(ConsentApiFixture fixture, ITestOutputHelper output)
        {
            _output = output;
            _fixture = fixture;
        }

        private void SetupMockPartyRepository()
        {
            MockParyRepositoryPopulator.SetupMockPartyRepository(_fixture.AmPartyRepository);
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [Fact]
        public async Task GetConsentFromA2_Valid_Returns200WithConsentInfo()
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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            var task = await repositgo.GetRequest(requestId, TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(requestId == consentInfo.Id);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
        }

        [Fact]
        public async Task GetConsentFromA2_Expired_ReturnsProblem()
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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            var task = await repositgo.GetRequest(requestId, TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            ProblemDetails problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal("Consent is expired", problemDetails.Detail);
        }

        [Fact]
        public async Task GetConsentFromA2_InvalidMetadata_Returns200WithConsentInfo()
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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            var task = await repositgo.GetRequest(requestId, TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(requestId == consentInfo.Id);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
        }

        [Fact]
        public async Task GetConsent_CreatedExpired_Returns500MultipleProblemsExpiredAndNotAccepted()
        {
            SetupMockPartyRepository();

            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), TestContext.Current.CancellationToken);

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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal(StdProblemDescriptors.ErrorCodes.MultipleProblems, problemDetails.ErrorCode);
            Assert.Equal(2, problemDetails.Problems.Count());
            Assert.Equal(Problems.ConsentExpired.ErrorCode, problemDetails.Problems.ToList()[0].ErrorCode);
            Assert.Equal(Problems.ConsentNotAccepted.ErrorCode, problemDetails.Problems.ToList()[1].ErrorCode);
        }

        [Fact]
        public async Task GetConsent_ValidFromOrg_Returns200WithConsentInfo()
        {
            SetupMockPartyRepository();
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed77");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentRequest request = await GetRequest(requestId);
            request.ValidTo = DateTime.UtcNow.AddDays(10);
            await repositgo.CreateRequest(request, Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), TestContext.Current.CancellationToken);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, Guid.NewGuid(), consentContextExternal.ToConsentContext(), TestContext.Current.CancellationToken);

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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
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
        public async Task GetConsent_ValidHandledByOrg_Returns200WithConsentInfo()
        {
            SetupMockPartyRepository();
            Guid requestId = Guid.Parse("d1bedb7d-a682-4668-9f84-7a56b3d733ab");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentRequest request = await GetRequest(requestId);
            request.ValidTo = DateTime.UtcNow.AddDays(10);
            await repositgo.CreateRequest(request, Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), TestContext.Current.CancellationToken);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, Guid.NewGuid(), consentContextExternal.ToConsentContext(), TestContext.Current.CancellationToken);

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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            ConsentInfoMaskinportenDto consentInfo = JsonSerializer.Deserialize<ConsentInfoMaskinportenDto>(responseContent, _jsonOptions);
            Assert.True(DateTime.UtcNow.AddDays(-2) < consentInfo.Consented);
            Assert.Equal(2, consentInfo.ConsentRights.Count());
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("910493353")), consentInfo.From);
        }

        [Fact]
        public async Task GetConsent_Created_Returns400ConsentNotAccepted()
        {
            SetupMockPartyRepository();
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed46");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            ConsentRequest request = await GetRequest(requestId);
            request.ValidTo = DateTime.UtcNow.AddDays(10);
            await repositgo.CreateRequest(request, Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), TestContext.Current.CancellationToken);

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

            HttpResponseMessage response = await client.PostAsJsonAsync(url, consentLookup, cancellationToken: TestContext.Current.CancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
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
