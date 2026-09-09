using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Altinn.AccessManagement.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration tests for the Maskinporten delegations lookup endpoint served from the ServiceOwner API.
    /// The delegation lookup itself is stubbed so the tests exercise routing, authorization, input
    /// validation and the controller's mapping/serialization to the external contract.
    /// </summary>
    [IntegrationTest]
    public class MaskinportenDelegationsControllerTest : IClassFixture<AccessMgmtApiFixture>
    {
        private const string SupplierOrgNumber = "810418672";
        private const string ConsumerOrgNumber = "810418192";
        private const string Scope = "altinn:test/theworld.write";
        private const string ResourceId = "nav_aa_distribution";
        private static readonly Guid DelegationSchemeId = Guid.Parse("f0e5c3d2-1a2b-4c3d-9e8f-0a1b2c3d4e5f");
        private static readonly DateTime Created = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        private readonly AccessMgmtApiFixture _fixture;
        private readonly HttpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskinportenDelegationsControllerTest"/> class.
        /// </summary>
        public MaskinportenDelegationsControllerTest(AccessMgmtApiFixture fixture)
        {
            _fixture = fixture;
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProviderMock>();
                services.RemoveAll<IPDP>();
                services.AddSingleton<IPDP, PepWithPDPAuthorizationMock>();

                // Stub the lookup so the happy path returns a known delegation and the test can
                // assert the controller's mapping to the external JSON contract.
                services.RemoveAll<IMaskinportenDelegationLookupService>();
                services.AddSingleton<IMaskinportenDelegationLookupService>(new StubLookupService());
            });

            _client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [Fact]
        public async Task GetMaskinportenDelegations_NoToken_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?scope={Scope}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMaskinportenDelegations_AdminToken_NoParameters_ReturnsBadRequest()
        {
            _client.DefaultRequestHeaders.Authorization = AdminBearer();

            HttpResponseMessage response = await _client.GetAsync("accessmanagement/api/v1/maskinporten/delegations/", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMaskinportenDelegations_AdminToken_InvalidSupplierOrg_ReturnsBadRequest()
        {
            _client.DefaultRequestHeaders.Authorization = AdminBearer();

            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg=123&scope={Scope}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMaskinportenDelegations_ScopeNotOwnedByConsumer_ReturnsForbidden()
        {
            // Non-admin token whose consumer_prefix ("skd") does not match the requested scope prefix.
            string token = PrincipalUtil.GetOrgToken("SKD", "974761076", "altinn:maskinporten/delegations", consumerPrefix: new[] { "skd" });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={SupplierOrgNumber}&consumerorg={ConsumerOrgNumber}&scope=altinn:instances.read", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMaskinportenDelegations_NonAdminToken_OwnedScope_Returns200()
        {
            // Token for the calling service owner whose consumer_prefix ("mp") owns the requested scope
            // prefix ("mp:..."), so it is authorized to look up that scope.
            string token = PrincipalUtil.GetOrgToken("SERVICEOWNER", "310202398", "altinn:maskinporten/delegations", consumerPrefix: new[] { "mp" });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?consumerorg={ConsumerOrgNumber}&scope=mp:test/theworld.read", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetMaskinportenDelegations_AdminToken_Valid_Returns200WithMappedDelegations()
        {
            _client.DefaultRequestHeaders.Authorization = AdminBearer();

            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={SupplierOrgNumber}&consumerorg={ConsumerOrgNumber}&scope={Scope}", TestContext.Current.CancellationToken);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using JsonDocument doc = JsonDocument.Parse(content);
            JsonElement root = doc.RootElement;
            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.Equal(1, root.GetArrayLength());

            JsonElement item = root[0];

            // supplier_org / consumer_org are intentionally mapped from covered/offered on the domain model.
            Assert.Equal(SupplierOrgNumber, item.GetProperty("supplier_org").GetString());
            Assert.Equal(ConsumerOrgNumber, item.GetProperty("consumer_org").GetString());
            Assert.Equal(ResourceId, item.GetProperty("resourceid").GetString());
            Assert.Equal(DelegationSchemeId, item.GetProperty("delegation_scheme_Id").GetGuid());

            List<string> scopes = item.GetProperty("scopes").EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.Equal(new[] { Scope }, scopes);
        }

        private static AuthenticationHeaderValue AdminBearer()
        {
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            return new AuthenticationHeaderValue("Bearer", token);
        }

        private sealed class StubLookupService : IMaskinportenDelegationLookupService
        {
            public Task<List<Delegation>> GetMaskinportenDelegations(string? supplierOrg, string? consumerOrg, string? scope, CancellationToken cancellationToken = default)
            {
                Delegation delegation = new()
                {
                    // The external supplier_org maps from CoveredBy, consumer_org from OfferedBy.
                    CoveredByOrganizationNumber = SupplierOrgNumber,
                    OfferedByOrganizationNumber = ConsumerOrgNumber,
                    Created = Created,
                    ResourceId = ResourceId,
                    ResourceReferences = new List<ResourceReference>
                    {
                        new() { ReferenceType = ReferenceType.MaskinportenScope, Reference = Scope },
                        new() { ReferenceType = ReferenceType.DelegationSchemeId, Reference = DelegationSchemeId.ToString() },
                    },
                };

                return Task.FromResult(new List<Delegation> { delegation });
            }
        }
    }
}
