using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AccessMgmt.Tests.Mocks;
using Altinn.AccessManagement.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessManagement.Tests.Utils;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

// Migrated from CustomWebApplicationFactory<MaskinportenSchemaController> to ApiFixture
// as part of Phase 2.2 (Sub-step 16.2b — AccessMgmt.Tests WAF consolidation, Group A
// nested-class splits). The five tests that registered PdpPermitMock instead of
// PepWithPDPAuthorizationMock live in the sibling class MaskinportenSchemaPdpPermitControllerTest.
// Per-test HttpContextAccessor route-value customization is supported without
// rebuilding DI by using a shared MutableHttpContextAccessor (AsyncLocal-based override).
// See: overhaul part-1 step 16
namespace Altinn.AccessManagement.Tests.Controllers
{
    /// <summary>
    /// Shared IHttpContextAccessor whose <see cref="HttpContext"/> getter returns
    /// a test-scoped override (stored in an <see cref="AsyncLocal{T}"/>) when one
    /// is set via <see cref="SetOverride(string, string)"/>. Falls back to the
    /// inner <see cref="HttpContextAccessor"/> otherwise so the ambient request
    /// context continues to flow during normal request handling.
    /// </summary>
    internal sealed class MutableHttpContextAccessor : IHttpContextAccessor
    {
        private readonly Dictionary<string, object> _routeOverrides = new();
        private readonly HttpContextAccessor _default = new();

        public HttpContext HttpContext
        {
            get
            {
                var real = _default.HttpContext;
                Dictionary<string, object> overrides;
                lock (_routeOverrides)
                {
                    if (_routeOverrides.Count == 0 || real is null)
                    {
                        return real;
                    }

                    overrides = new Dictionary<string, object>(_routeOverrides);
                }

                // Return a lightweight fake that carries the real User and
                // request Headers so authorization handlers (which read both
                // from the injected IHttpContextAccessor) continue to work,
                // but with route values overridden per-test. Matches legacy
                // behavior where GetHttpContextAccessorMock injected a
                // dedicated accessor with only "party" route values set.
                var fake = new DefaultHttpContext { User = real.User };
                foreach (var h in real.Request.Headers)
                {
                    fake.Request.Headers[h.Key] = h.Value;
                }

                foreach (var kvp in overrides)
                {
                    fake.Request.RouteValues[kvp.Key] = kvp.Value;
                }

                return fake;
            }
            set => _default.HttpContext = value;
        }

        public void SetOverride(string partytype, string id)
        {
            lock (_routeOverrides)
            {
                _routeOverrides[partytype] = id;
            }
        }

        public void ClearOverride()
        {
            lock (_routeOverrides)
            {
                _routeOverrides.Clear();
            }
        }
    }

    /// <summary>
    /// Test class for <see cref="MaskinportenSchemaController"></see>
    /// </summary>
    public class MaskinportenSchemaControllerTest : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;
        private static readonly MutableHttpContextAccessor _httpContextAccessor = new();
        private HttpClient _client;

        private readonly JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Constructor setting up the shared <see cref="ApiFixture"/> with the
        /// mocks required by this controller's tests. IPDP defaults to
        /// <see cref="PepWithPDPAuthorizationMock"/> (the majority variant).
        /// </summary>
        /// <param name="fixture">Shared <see cref="ApiFixture"/>.</param>
        public MaskinportenSchemaControllerTest(ApiFixture fixture)
        {
            _fixture = fixture;
            _httpContextAccessor.ClearOverride();
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
                services.AddSingleton<IDelegationChangeEventQueue, DelegationChangeEventQueueMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPartiesClient, PartiesClientMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                services.RemoveAll<IHttpContextAccessor>();
                services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);
                services.RemoveAll<IPDP>();
                services.AddSingleton<IPDP, PepWithPDPAuthorizationMock>();
            });

            _client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string token = PrincipalUtil.GetAccessToken("sbl.authorization");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private HttpClient NewClient()
        {
            var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private DelegationMetadataRepositoryMock ResetDelegationMetadataRepository()
        {
            var repo = (DelegationMetadataRepositoryMock)_fixture.Services.GetRequiredService<IDelegationMetadataRepository>();
            repo.MetadataChanges = new Dictionary<string, List<DelegationChange>>();
            return repo;
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns a list of delegations offeredby has given coveredby
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns a list of delegations offeredby has given coveredby
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_Valid_OfferedByParty()
        {
            // Arrange
            List<MaskinportenSchemaDelegationExternal> expectedDelegations = TestDataUtil.GetOfferedMaskinportenSchemaDelegations(50004223);
            var token = PrincipalUtil.GetToken(4321, 87654321, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004223/maskinportenschema/offered");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<MaskinportenSchemaDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MaskinportenSchemaDelegationExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertMaskinportenSchemaDelegationExternalEqual);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns a list of delegations offeredby has given coveredby
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns a list of delegations offeredby has given coveredby
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_Valid_OfferedByOrg()
        {
            // Arrange
            List<MaskinportenSchemaDelegationExternal> expectedDelegations = TestDataUtil.GetOfferedMaskinportenSchemaDelegations(50004223);
            var httpContextAccessorMock = GetHttpContextAccessorMock("party", "50004223");
            _client = GetTestClient(httpContextAccessor: httpContextAccessorMock);
            var token = PrincipalUtil.GetToken(4321, 87654321, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418982");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/organization/maskinportenschema/offered");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<MaskinportenSchemaDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MaskinportenSchemaDelegationExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertMaskinportenSchemaDelegationExternalEqual);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns notfound when the query parameter is missing
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns notfound
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_Notfound_MissingOfferedBy()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1//maskinportenschema/offered");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns Forbidden when the query parameter is invalid
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns Forbidden
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_Forbidden_InvalidOfferedBy()
        {
            // Arrange
            _client = GetTestClient(new PepWithPDPAuthorizationMock());
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/123/maskinportenschema/offered");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns 200 with response message empty array when there are no delegations for the reportee
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns 200 with response message empty array when there are no delegations for the reportee
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_OfferedBy_NoDelegations()
        {
            // Arrange
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string expected = "[]";

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004225/maskinportenschema/offered");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(expected, responseContent);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns list of resources that were delegated. The resource metadata is set to not available if the resource in a delegation for some reason is  not found in resource registry
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns list of resources that were delegated. The resource metadata is set to not available if the resource in a delegation for some reason is  not found in resource registry
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_ResourceMetadataNotFound()
        {
            // Arrange
            List<MaskinportenSchemaDelegationExternal> expectedDelegations = TestDataUtil.GetOfferedMaskinportenSchemaDelegations(50004226);
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004226/maskinportenschema/offered");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<MaskinportenSchemaDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MaskinportenSchemaDelegationExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertMaskinportenSchemaDelegationExternalEqual);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not set
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not set
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_MissingBearerToken()
        {
            _client.DefaultRequestHeaders.Remove("Authorization");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004223/maskinportenschema/offered");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not valid
        /// Expected: GetOfferedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not valid
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_InvalidBearerToken()
        {
            // Arrange
            _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "This is an invalid token");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004223/maskinportenschema/offered");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns a list of delegations received by coveredby
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns a list of delegations received by coveredby
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_Valid_CoveredBy()
        {
            // Arrange
            List<MaskinportenSchemaDelegationExternal> expectedDelegations = TestDataUtil.GetReceivedMaskinportenSchemaDelegations(50004219);

            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004219/maskinportenschema/received");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<MaskinportenSchemaDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MaskinportenSchemaDelegationExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertMaskinportenSchemaDelegationExternalEqual);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns a list of delegations received by coveredby when the coveredby is an organisation number
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns a list of delegations received by coveredby
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_Valid_CoveredByOrg()
        {
            // Arrange
            List<MaskinportenSchemaDelegationExternal> expectedDelegations = TestDataUtil.GetReceivedMaskinportenSchemaDelegations(50004219);

            var httpContextAccessorMock = GetHttpContextAccessorMock("party", "50004219");
            _client = GetTestClient(new PepWithPDPAuthorizationMock(), httpContextAccessorMock);
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418192");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/organization/maskinportenschema/received");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<MaskinportenSchemaDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MaskinportenSchemaDelegationExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertMaskinportenSchemaDelegationExternalEqual);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns notfound when the query parameter is missing
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns notfound when the query parameter is missing
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_Missing_CoveredBy()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1//maskinportenschema/received");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns Forbidden when the query parameter is invalid
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns Forbidden
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_Invalid_CoveredBy()
        {
            // Arrange
            _client = GetTestClient(new PepWithPDPAuthorizationMock());
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/1234/maskinportenschema/received");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns 200 with empty array when there are no delegations received for the reportee
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns 200 with rempty array when there are no delegations received for the reportee
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_CoveredBy_NoDelegations()
        {
            // Arrange
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string expected = "[]";

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004225/maskinportenschema/received");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(expected, responseContent);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns list of resources that were delegated. The resource metadata is set to not available if the resource in a delegation for some reason is  not found in resource registry
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns list of resources that were delegated. The resource metadata is set to not available if the resource in a delegation for some reason is  not found in resource registry
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_ResourceMetadataNotFound()
        {
            // Arrange
            List<MaskinportenSchemaDelegationExternal> expectedDelegations = TestDataUtil.GetReceivedMaskinportenSchemaDelegations(50004216);

            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004216/maskinportenschema/received");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<MaskinportenSchemaDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MaskinportenSchemaDelegationExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertMaskinportenSchemaDelegationExternalEqual);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not set
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not set
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_MissingBearerToken()
        {
            _client.DefaultRequestHeaders.Remove("Authorization");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004223/maskinportenschema/received");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not valid
        /// Expected: GetReceivedMaskinportenSchemaDelegations returns unauthorized when the bearer token is not valid
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_InvalidBearerToken()
        {
            _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "This is an invalid token");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/50004223/maskinportenschema/received");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations returns a list of delegations between supplier and consumer for a given scope.
        ///            Token is authorized for admin scope and and can lookup delegations even when scope is not in the consumers owned scope-prefixes (consumer_prefix)
        /// Expected: GetMaskinportenDelegations returns a list of delegations offered by supplier to consumer for a given scope
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_Valid()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<string> resourceIds = new List<string>
            {
                "nav_aa_distribution",
                "appid-123"
            };
            List<MPDelegationExternal> expectedDelegations = GetExpectedMaskinportenSchemaDelegations("810418672", "810418192", resourceIds);

            // Act
            int supplierOrg = 810418672;
            int consumerOrg = 810418192;
            string scope = "altinn:test/theworld.write";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();
            List<MPDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MPDelegationExternal>>(responseContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertDelegationEqual);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations returns a list of delegations between supplier and consumer for a given scope.
        ///            Token is authorized for admin scope and and can lookup delegations even when scope is not in the consumers owned scope-prefixes (consumer_prefix)
        /// Expected: GetMaskinportenDelegations returns a list of delegations offered by supplier to consumer for a given scope
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_ServiceOwnerLookup_Valid()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<string> resourceIds = new List<string>
            {
                "nav_aa_distribution",
                "appid-123"
            };
            List<MPDelegationExternal> expectedDelegations = GetExpectedMaskinportenSchemaDelegations("810418672", "810418192", resourceIds);

            // Act
            int supplierOrg = 810418672;
            int consumerOrg = 810418192;
            string scope = "altinn:test/theworld.write";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();
            List<MPDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MPDelegationExternal>>(responseContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertDelegationEqual);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations returns a list of delegations for a given scope.
        ///            Token is authorized for admin scope and and can lookup delegations even when scope is not in the consumers owned scope-prefixes (consumer_prefix)
        /// Expected: GetMaskinportenDelegations returns a list of delegations for a given scope
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_ServiceOwnerLookup_NoSupplerConsumer_Valid()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<string> resourceIds = new List<string>
            {
                "nav_aa_distribution",
                "appid-123"
            };
            List<MPDelegationExternal> expectedDelegations = GetExpectedMaskinportenSchemaDelegations(null, null, resourceIds);

            // Act
            string scope = "altinn:test/theworld.write";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();
            List<MPDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MPDelegationExternal>>(responseContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertDelegationEqual);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations with a scope with altinn prefix, which the serviceowner skd is not authorized for
        /// Expected: GetMaskinportenDelegations returns forbidden
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_ServiceOwnerLookup_UnauthorizedScope()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("SKD", "974761076", "altinn:maskinporten/delegations", null, consumerPrefix: new[] { "skd" });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "Not authorized for lookup of delegations for the scope: altinn:instances.read";

            // Act
            int supplierOrg = 810418362;
            int consumerOrg = 810418532;
            string scope = "altinn:instances.read";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();
            ProblemDetails errorResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(expected, errorResponse.Title);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations with serviceowner scope, without specifying a scope query param
        /// Expected: GetMaskinportenDelegations returns forbidden
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_ServiceOwnerLookup_WithoutScope()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("SKD", "974761076", "altinn:maskinporten/delegations", null, new[] { "skd" });
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "Not authorized for lookup of delegations without specifying parameter: scope";

            // Act
            int supplierOrg = 810418362;
            int consumerOrg = 810418532;

            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope=");
            string responseContent = await response.Content.ReadAsStringAsync();
            ProblemDetails errorResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(expected, errorResponse.Title);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations for orgnummer that does not have any delegations
        /// Expected: GetMaskinportenDelegations returns ok, no delegations found
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_Valid_DelegationsEmpty()
        {
            // Arrange
            string token = string.Empty;

            // Trying to move this line
            token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin", null, null);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "[]";

            // Act
            int supplierOrg = 810418362;
            int consumerOrg = 810418532;
            string scope = "altinn:test/theworld.write";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(expected, responseContent);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations authorized with admin scope, without specifying filter scope param
        /// Expected: GetMaskinportenDelegations returns OK
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_WithoutScope_Valid()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<string> resourceIds = new List<string>
            {
                "nav_aa_distribution",
                "appid-123"
            };
            List<MPDelegationExternal> expectedDelegations = GetExpectedMaskinportenSchemaDelegations("810418672", "810418192", resourceIds);

            // Act
            int supplierOrg = 810418672;
            int consumerOrg = 810418192;

            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope=");
            string responseContent = await response.Content.ReadAsStringAsync();
            List<MPDelegationExternal> actualDelegations = JsonSerializer.Deserialize<List<MPDelegationExternal>>(responseContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertionUtil.AssertCollections(expectedDelegations, actualDelegations, AssertionUtil.AssertDelegationEqual);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations without providing any scope, supplierOrg or consumerOrg
        /// Expected: GetMaskinportenDelegations returns badrequest
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_WithoutScopeOrOrgs_BadRequest()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "Query without parameter; scope, must provide at least one of; supplierOrg, consumerOrg";

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg=&consumerorg=&scope=");
            string responseContent = await response.Content.ReadAsStringAsync();
            ProblemDetails errorResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expected, errorResponse.Title);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations for invalid supplier orgnummer
        /// Expected: GetMaskinportenDelegations returns badrequest
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_InvalidSupplier()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "Supplierorg is not an valid organization number";

            // Act
            string supplierOrg = "12345";
            string consumerOrg = "810418532";
            string scope = "altinn:test/theworld.write";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails errorResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expected, errorResponse.Errors["supplierOrg"][0]);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations for invalid consumer orgnummer
        /// Expected: GetMaskinportenDelegations returns badrequest
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_InvalidConsumer()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "Consumerorg is not an valid organization number";

            // Act
            string supplierOrg = "810418362";
            string consumerOrg = "12345";
            string scope = "altinn:test/theworld.write";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails errorResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expected, errorResponse.Errors["consumerOrg"][0]);
        }

        /// <summary>
        /// Test case: GetMaskinportenDelegations for a scope which is not a registered reference on any resources
        /// Expected: GetMaskinportenDelegations returns ok, no delegations found
        /// </summary>
        [Fact]
        public async Task GetMaskinportenDelegations_Admin_ScopesNotRegisteredOnResource()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string expected = "[]";

            // Act
            int supplierOrg = 810418672;
            int consumerOrg = 810418192;
            string scope = "altinn:test/test";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, responseContent.Replace('"', ' ').Trim());
        }

        /// <summary>
        /// Test case: GetMaskinportenSchemaDelegations for a URN scope format
        /// Expected: GetMaskinportenSchemaDelegations returns OK
        /// </summary>
        [Fact]
        public async Task GetMaskinportenSchemaDelegations_Admin_UrnScopeFormat()
        {
            // Arrange
            string token = PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            int supplierOrg = 810418672;
            int consumerOrg = 810418192;
            string scope = "altinn:maskinporten:urn:scope:test";
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/maskinporten/delegations/?supplierorg={supplierOrg}&consumerorg={consumerOrg}&scope={scope}");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("[]", responseContent);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations, user with necessary rights
        /// Expected: User is authorized
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_UserComplyingToPolicy()
        {
            // Arrange
            const HttpStatusCode expectedStatusCode = HttpStatusCode.OK;
            var httpContextAccessorMock = GetHttpContextAccessorMock("party", "50004219");
            _client = GetTestClient(new PepWithPDPAuthorizationMock(), httpContextAccessorMock);
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418192");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/organization/maskinportenschema/offered");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetOfferedMaskinportenSchemaDelegations, user without necessary rights
        /// Expected: Authorization is denied
        /// Testing if user without necessary rights is denied access to 
        /// </summary>
        [Fact]
        public async Task GetOfferedMaskinportenSchemaDelegations_UserNotComplyingToPolicy()
        {
            // Arrange 
            const HttpStatusCode expectedStatusCode = HttpStatusCode.Forbidden;
            var httpContextAccessorMock = GetHttpContextAccessorMock("party", "12345678");
            _client = GetTestClient(new PepWithPDPAuthorizationMock(), httpContextAccessorMock);
            var token = PrincipalUtil.GetToken(1234, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418192");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/organization/maskinportenschema/offered");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations, user with necessary rights
        /// Expected: User is authorized
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_UserComplyingToPolicy()
        {
            // Arrange
            const HttpStatusCode expectedStatusCode = HttpStatusCode.OK;
            var httpContextAccessorMock = GetHttpContextAccessorMock("party", "50004219");
            _client = GetTestClient(new PepWithPDPAuthorizationMock(), httpContextAccessorMock);
            var token = PrincipalUtil.GetToken(1234, 12344321, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418192");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/organization/maskinportenschema/received");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        /// <summary>
        /// Test case: GetReceivedMaskinportenSchemaDelegations, user without necessary rights
        /// Expected: Authorization is denied
        /// Testing if user without necessary rights is denied access to 
        /// </summary>
        [Fact]
        public async Task GetReceivedMaskinportenSchemaDelegations_UserNotComplyingToPolicy()
        {
            // Arrange 
            const HttpStatusCode expectedStatusCode = HttpStatusCode.Forbidden;
            var httpContextAccessorMock = GetHttpContextAccessorMock("party", "50004219");
            _client = GetTestClient(new PepWithPDPAuthorizationMock(), httpContextAccessorMock);
            var token = PrincipalUtil.GetToken(4321, 12345678, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418192");

            // Act
            HttpResponseMessage response = await _client.GetAsync($"accessmanagement/api/v1/organization/maskinportenschema/received");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry, to the organization 50004222
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        /// Expected: MaskinportenDelegation returns 201 Created with response body containing the expected delegated rights
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_DAGL_Success()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            RightsDelegationResponseExternal expectedResponse = GetExpectedResponse("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004222");
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004222");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            RightsDelegationResponseExternal actualResponse = JsonSerializer.Deserialize<RightsDelegationResponseExternal>(responseContent, options);
            AssertionUtil.AssertRightsDelegationResponseExternalEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 for the reportee organization 910459880 of the jks_audi_etron_gt maskinporten schema resource from the resource registry, to the organization 50004222
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 910459880
        /// Expected: MaskinportenDelegation returns 201 Created with response body containing the expected delegated rights
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_DAGL_ExternalIdentifier_OrgNoReportee_Success()
        {
            // Arrange
            string fromParty = "50005545";
            _client = GetTestClient(httpContextAccessor: GetHttpContextAccessorMock("party", fromParty));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "910459880");

            RightsDelegationResponseExternal expectedResponse = GetExpectedResponse("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004222");
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004222");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/organization/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            RightsDelegationResponseExternal actualResponse = JsonSerializer.Deserialize<RightsDelegationResponseExternal>(responseContent, options);
            AssertionUtil.AssertRightsDelegationResponseExternalEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry, to the organization 810418672
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The request 'To' property is using the urn:altinn:organizationnumber attribute in order to use the externally available organizationnumber to specify the recipient of the delegation
        /// Expected: MaskinportenDelegation returns 201 Created with response body containing the expected delegated rights
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_DAGL_ExternalIdentifier_OrgNoRecipient_Success()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            RightsDelegationResponseExternal expectedResponse = GetExpectedResponse("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "810418672");
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "810418672");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            RightsDelegationResponseExternal actualResponse = JsonSerializer.Deserialize<RightsDelegationResponseExternal>(responseContent, options);
            AssertionUtil.AssertRightsDelegationResponseExternalEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed without a user token
        /// Expected: 401 Unauthorized
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_MissingToken_Unauthorized()
        {
            // Arrange
            string fromParty = "50005545";
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004222");

            // Act
            HttpResponseMessage response = await NewClient().PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 with authentication level 2,
        ///            for the reportee party 50002598 to the recipient party 2
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The recipient (To) in the request is not a valid party id
        /// Expected: MaskinportenDelegation returns 400 Bad Request with a problem details respons body describing the error
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_InvalidTo()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "p2", "Input_Default");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "p2", "ExpectedOutput_InvalidTo");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 with authentication level 2,
        ///            for the reportee party 50002598 to the recipient userid 20001337
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The recipient (To) in the request is a userid which delegation of maskinporten schema to should not be possible
        /// Expected: MaskinportenDelegation returns 400 Bad Request with a problem details respons body describing the error
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_InvalidTo_UserId()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "u20001337", "Input_InvalidTo_UserId");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "u20001337", "ExpectedOutput_InvalidTo_UserId");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 with authentication level 2,
        ///            for the reportee party 50002598 to a recipient social security number
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The recipient (To) in the request is a social security number which delegation of maskinporten schema to should not be possible
        /// Expected: MaskinportenDelegation returns 400 Bad Request with a problem details respons body describing the error
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_InvalidTo_Ssn()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "u20001337", "Input_InvalidTo_Ssn");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "u20001337", "ExpectedOutput_InvalidTo_Ssn");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 with authentication level 2,
        ///            for the reportee party 50005545 of the non_delegable_maskinportenschema maskinporten schema resource from the resource registry to the organization 50004222
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The non_delegable_maskinportenschema resource has a role requirement (NOPE), which does not exist meaning the user will not have any delegable rights for the resource
        /// Expected: MaskinportenDelegation returns 400 Bad Request with a problem details respons body describing the error
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_NonDelegableResource()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "non_delegable_maskinportenschema", $"p{fromParty}", "p50004222");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "non_delegable_maskinportenschema", $"p{fromParty}", "p50004222");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 with authentication level 2,
        ///            for the reportee party 50005545 of the digdirs_company_car maskinporten schema resource from the resource registry to the organization 50004222
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The required minimum authentication level for digdirs_company_car
        /// Expected: MaskinportenDelegation returns 400 Bad Request with a problem details respons body describing the error
        /// </summary>
        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_TooLowAuthenticationLevelForResource()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "digdirs_company_car", $"p{fromParty}", "p50004222");
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "digdirs_company_car", $"p{fromParty}", "p50004222");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: RevokeOfferedMaskinportenScopeDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            delegated to the organization 50004221
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        /// Expected: RevokeOfferedMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeOfferedMaskinportenScopeDelegation_DAGL_Success()
        {
            // Arrange
            int fromParty = 50005545;
            DelegationMetadataRepositoryMock delegationMetadataRepositoryMock = new DelegationMetadataRepositoryMock { MetadataChanges = new Dictionary<string, List<DelegationChange>>() };
            HttpClient client = GetTestClient(delegationMetadataRepositoryMock: delegationMetadataRepositoryMock);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            Dictionary<string, List<DelegationChange>> expectedDbUpdates = new Dictionary<string, List<DelegationChange>>
            {
                { "resourceregistry/jks_audi_etron_gt/50005545/p50004221", new List<DelegationChange> { TestDataUtil.GetResourceRegistryDelegationChange("jks_audi_etron_gt", ResourceType.MaskinportenSchema, fromParty, Convert.ToDateTime("2022-09-27T13:02:23.786072Z"), coveredByPartyId: 50004221, performedByUserId: 20000490, changeType: DelegationChangeType.RevokeLast) } }
            };

            StreamContent requestContent = GetRequestContent("RevokeOfferedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004221");

            // Act
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            AssertionUtil.AssertEqual(expectedDbUpdates, delegationMetadataRepositoryMock.MetadataChanges);
        }

        /// <summary>
        /// Test case: RevokeOfferedMaskinportenScopeDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            delegated to the organization 50004221
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        /// Expected: RevokeOfferedMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeOfferedMaskinportenScopeDelegation_DAGL_ToOrgNo_Success()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("RevokeOfferedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "810418532");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeOfferedMaskinportenScopeDelegation performed by authenticated user 20000490 for the reportee organization 910459880 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            delegated to the organization 50004221
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 910459880
        /// Expected: RevokeOfferedMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeOfferedMaskinportenScopeDelegation_DAGL_FromOrgNo_Success()
        {
            // Arrange
            string fromParty = "50005545";
            _client = GetTestClient(httpContextAccessor: GetHttpContextAccessorMock("party", fromParty));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "910459880");

            StreamContent requestContent = GetRequestContent("RevokeOfferedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "810418532");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeOfferedMaskinportenScopeDelegation performed by authenticated user 20000095 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            delegated to the organization 50004221
        ///            In this case:
        ///            - The user 20000095 is ADMAI (Tilgangsstyrer) for the From unit 50005545
        /// Expected: RevokeOfferedMaskinportenScopeDelegation returns 403 Forbidden
        /// </summary>
        [Fact]
        public async Task RevokeOfferedMaskinportenScopeDelegation_ADMAI_Forbidden()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000095, 50002203));

            StreamContent requestContent = GetRequestContent("RevokeOfferedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50004221");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeOfferedMaskinportenScopeDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            delegated to party 2
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The To party does not exist
        /// Expected: RevokeOfferedMaskinportenScopeDelegation returns 400 Bad Request with ValidationProblemDetails body
        /// </summary>
        [Fact]
        public async Task RevokeOfferedMaskinportenScopeDelegation_ValidationProblemDetails_InvalidTo()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("RevokeOfferedMaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "p2");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("RevokeOfferedMaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "p2", "ExpectedOutput_InvalidTo");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: RevokeReceivedMaskinportenScopeDelegation performed by authenticated user 20001337 for the reportee party 50004221 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            which have received delegation from the party 50005545
        ///            In this case:
        ///            - The user 20001337 is DAGL for the To unit 50004221
        /// Expected: RevokeReceivedMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeReceivedMaskinportenScopeDelegation_DAGL_Success()
        {
            // Arrange
            string toParty = "50004221";
            DelegationMetadataRepositoryMock delegationMetadataRepositoryMock = new DelegationMetadataRepositoryMock { MetadataChanges = new Dictionary<string, List<DelegationChange>>() };
            HttpClient client = GetTestClient(delegationMetadataRepositoryMock: delegationMetadataRepositoryMock);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            Dictionary<string, List<DelegationChange>> expectedDbUpdates = new Dictionary<string, List<DelegationChange>>
            {
                { "resourceregistry/jks_audi_etron_gt/50005545/p50004221", new List<DelegationChange> { TestDataUtil.GetResourceRegistryDelegationChange("jks_audi_etron_gt", ResourceType.MaskinportenSchema, 50005545, created: null, coveredByPartyId: 50004221, performedByUserId: 20001337, changeType: DelegationChangeType.RevokeLast) } }
            };

            StreamContent requestContent = GetRequestContent("RevokeReceivedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p50005545", $"p{toParty}");

            // Act
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/{toParty}/maskinportenschema/received/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            AssertionUtil.AssertEqual(expectedDbUpdates, delegationMetadataRepositoryMock.MetadataChanges);
        }

        /// <summary>
        /// Test case: RevokeActiveMaskinportenScopeDelegation performed by authenticated user 20001337 for the reportee party 50004221 of the non-delegable altinn_access_management maskinporten schema resource from the resource registry,
        ///            which have received delegation from the party 50005545
        ///            In this case:
        ///            - The user 20001337 is DAGL for the To unit 50004221
        /// Expected: RevokeActiveMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeActiveMaskinportenScopeDelegation_ResourceDelegableFalse_Success()
        {
            // Arrange
            string toParty = "50004221";
            DelegationMetadataRepositoryMock delegationMetadataRepositoryMock = new DelegationMetadataRepositoryMock { MetadataChanges = new Dictionary<string, List<DelegationChange>>() };
            HttpClient client = GetTestClient(delegationMetadataRepositoryMock: delegationMetadataRepositoryMock);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("RevokeReceivedMaskinportenScopeDelegation", "jks_undelegable", $"p50005545", $"p{toParty}");

            // Act
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/{toParty}/maskinportenschema/received/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeReceivedMaskinportenScopeDelegation performed by authenticated user 20001337 for the reportee organization 810418532 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            which have received delegation from the party 50005545
        ///            In this case:
        ///            - The user 20001337 is DAGL for the To unit 810418532
        /// Expected: RevokeReceivedMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeReceivedMaskinportenScopeDelegation_DAGL_ToOrgNo_Success()
        {
            // Arrange
            string toParty = "50004221";
            _client = GetTestClient(httpContextAccessor: GetHttpContextAccessorMock("party", toParty));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418532");

            StreamContent requestContent = GetRequestContent("RevokeReceivedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p50005545", $"p{toParty}");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/organization/maskinportenschema/received/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeReceivedMaskinportenScopeDelegation performed by authenticated user 20001337 for the reportee organization 910459880 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            which have received delegation from the party 50005545
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 910459880
        /// Expected: RevokeReceivedMaskinportenScopeDelegation returns 204 No Content
        /// </summary>
        [Fact]
        public async Task RevokeReceivedMaskinportenScopeDelegation_DAGL_FromOrgNo_Success()
        {
            // Arrange
            string toParty = "50004221";
            _client = GetTestClient(httpContextAccessor: GetHttpContextAccessorMock("party", toParty));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));
            _client.DefaultRequestHeaders.Add("Altinn-Party-OrganizationNumber", "810418532");

            StreamContent requestContent = GetRequestContent("RevokeReceivedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"910459880", $"810418532");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/organization/maskinportenschema/received/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeReceivedMaskinportenScopeDelegation performed by authenticated user 20000095 for the reportee party 50004221 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            which have received delegation from the party 50005545
        ///            In this case:
        ///            - The user 20000095 is ADMAI (Tilgangsstyrer) for the To party 50004221
        /// Expected: RevokeReceivedMaskinportenScopeDelegation returns 403 Forbidden
        /// </summary>
        [Fact]
        public async Task RevokeReceivedMaskinportenScopeDelegation_ADMAI_Forbidden()
        {
            // Arrange
            string toParty = "50004221";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000095, 50002203));

            StreamContent requestContent = GetRequestContent("RevokeReceivedMaskinportenScopeDelegation", "jks_audi_etron_gt", $"p50005545", $"p{toParty}");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{toParty}/maskinportenschema/received/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: RevokeReceivedMaskinportenScopeDelegation performed by authenticated user 20001337 for the reportee party 50004221 of the jks_audi_etron_gt maskinporten schema resource from the resource registry,
        ///            which have received delegation from the party 2
        ///            In this case:
        ///            - The user 20001337 is DAGL for the To unit 50004221
        ///            - The From party 2 is not a valid party
        /// Expected: RevokeReceivedMaskinportenScopeDelegation returns 400 Bad Request with ValidationProblemDetails body
        /// </summary>
        [Fact]
        public async Task RevokeReceivedMaskinportenScopeDelegation_ValidationProblemDetails_InvalidFrom()
        {
            // Arrange
            string toParty = "50004221";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("RevokeReceivedMaskinportenScopeDelegation", "mp_validation_problem_details", $"p2", $"p{toParty}");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("RevokeReceivedMaskinportenScopeDelegation", "mp_validation_problem_details", $"p2", $"p{toParty}", "ExpectedOutput_InvalidFrom");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{toParty}/maskinportenschema/received/revoke", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry, to the organization with partyId 50005545 (orgNr 910459880)
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        /// Expected: MaskinportenDelegation returns 400 BadRequest with response body containing ValidationProblemDetails with error message that CoveredBy can not be the same as OfferedBy
        /// </summary>
        [Fact]
        public async Task MaskinportenDelegation_DAGL_FromAndToIdenticalPartyId()
        {
            // Arrange
            string fromParty = "50005545";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50005545");
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", "p50005545");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: MaskinportenDelegation performed by authenticated user 20000490 for the reportee party 50005545 of the jks_audi_etron_gt maskinporten schema resource from the resource registry, to the organization with orgNr 910459880 (partyId: 50005545)
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        /// Expected: MaskinportenDelegation returns 400 BadRequest with response body containing ValidationProblemDetails with error message that CoveredBy can not be the same as OfferedBy
        /// </summary>
        [Fact]
        public async Task MaskinportenDelegation_DAGL_FromAndToIdenticalOrgNr()
        {
            // Arrange
            string fromParty = "50005545";
            string toOrg = "910459880";

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", toOrg);
            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "jks_audi_etron_gt", $"p{fromParty}", toOrg);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{fromParty}/maskinportenschema/offered", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: DelegationCheck returns a list of rights the authenticated userid 20000490 is authorized to delegate the maskinportenchema on behalf of the reportee party 50005545.
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545
        ///            - The only right for the maskinportenschema scope-access-schema is delegable through having DAGL:
        ///                 - scope-access-schema:ScopeAccess
        /// Expected: DelegationCheck returns a list of RightDelegationStatus matching expected
        /// </summary>
        [Fact]
        public async Task DelegationCheck_DAGL_HasDelegableRights()
        {
            // Arrange
            int userId = 20000490;
            int reporteePartyId = 50005545;
            string resourceId = "scope-access-schema";

            var token = PrincipalUtil.GetToken(userId, 0, 3);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<RightDelegationCheckResultExternal> expectedResponse = GetExpectedDelegationStatus($"u{userId}", $"p{reporteePartyId}", resourceId);
            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<RightDelegationCheckResultExternal> actualResponse = JsonSerializer.Deserialize<List<RightDelegationCheckResultExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedResponse, actualResponse, AssertionUtil.AssertRightDelegationCheckExternalEqual);
        }

        /// <summary>
        /// Test case: DelegationCheck returns a list of rights the authenticated userid 20001337 is authorized to delegate on behalf of the reportee party 50005545 for the generic-access-resource from the resource registry.
        ///            In this case:
        ///            - The user 20001337 is HADM for the From unit 50005545
        ///            - The only right for the maskinportenschema scope-access-schema is delegable through having DAGL (and HADM inherits the same rights as DAGL):
        ///               - scope-access-schema:ScopeAccess
        /// Expected: DelegationCheck returns a list of RightDelegationStatus matching expected
        /// </summary>
        [Fact]
        public async Task DelegationCheck_HADM_HasDelegableRights()
        {
            // Arrange
            int userId = 20001337;
            int reporteePartyId = 50005545;
            string resourceId = "scope-access-schema";

            var token = PrincipalUtil.GetToken(userId, 0, 3);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<RightDelegationCheckResultExternal> expectedResponse = GetExpectedDelegationStatus($"u{userId}", $"p{reporteePartyId}", resourceId);
            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<RightDelegationCheckResultExternal> actualResponse = JsonSerializer.Deserialize<List<RightDelegationCheckResultExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedResponse, actualResponse, AssertionUtil.AssertRightDelegationCheckExternalEqual);
        }

        /// <summary>
        /// Test case: DelegationCheck bars use by private persons
        ///            In this case:
        ///            - The user 20000490 has PRIV role for itself (party 50002598)
        ///            - Only Organizations can delegate and get delegated maskinpostenschema
        /// Expected: DelegationCheck returns a 403 Forbidden
        /// </summary>
        [Fact]
        public async Task DelegationCheck_PRIV_HasDelegableRights()
        {
            // Arrange
            int userId = 20000490;
            int reporteePartyId = 50002598;
            string resourceId = "scope-access-schema";

            var token = PrincipalUtil.GetToken(userId, 0, 3);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: DelegationCheck is only made to be used for MaskinportenServices, not A3 apps
        ///            In this case:
        ///            - Since the resource is an AltinnApp, a BadRequest response with a ValidationProblemDetails model response should be returned
        /// Expected: Responce error model is matching expected
        /// </summary>
        [Fact]
        public async Task DelegationCheck_AppRight_BadRequest()
        {
            // Arrange
            int userId = 20001337;
            int reporteePartyId = 50005545;
            string resourceId = "org1_app1";

            var token = PrincipalUtil.GetToken(userId, 0, 3);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ValidationProblemDetails expectedResponse = GetExpectedValidationError($"DelegationCheck", $"u{userId}", $"p{reporteePartyId}", resourceId);
            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: DelegationCheck when the authenticated 20001337 is authorized to delegate on behalf of the reportee party 50001337 for the invalid resource non_existing_id 
        ///            In this case:
        ///            - Since the resource is invalid a BadRequest response with a ValidationProblemDetails model response should be returned
        /// Expected: Responce error model is matching expected
        /// </summary>
        [Fact]
        public async Task DelegationCheck_InvalidResource_BadRequest()
        {
            // Arrange
            int userId = 20001337;
            int reporteePartyId = 50005545;
            string resourceId = "non_existing_id";

            var token = PrincipalUtil.GetToken(userId, 0, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ValidationProblemDetails expectedResponse = GetExpectedValidationError("DelegationCheck", $"u{userId}", $"p{reporteePartyId}", resourceId);
            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: DelegationCheck is only made to be used for MaskinportenServices, not resource registry resources
        ///            In this case:
        ///            - Since the resource is an AltinnApp, a BadRequest response with a ValidationProblemDetails model response should be returned
        /// Expected: Responce error model is matching expected
        /// </summary>
        [Fact]
        public async Task DelegationCheck_RRResource_BadRequest()
        {
            // Arrange
            int userId = 20001337;
            int reporteePartyId = 50005545;
            string resourceId = "generic-access-resource";

            var token = PrincipalUtil.GetToken(userId, 0, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ValidationProblemDetails expectedResponse = GetExpectedValidationError("DelegationCheck", $"u{userId}", $"p{reporteePartyId}", resourceId);
            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        /// <summary>
        /// Test case: DelegationCheck checks that minimum required access level is fulfilled
        ///            In this case:
        ///            - The user 20000490 is DAGL for the From unit 50005545, meaning the resource scope-access-schema is delegable for the user
        ///            - The user is currently authorized with a level 2 while the resource requires a minum level of 3
        /// Expected: DelegationCheck returns a responce error matching expected
        /// </summary>
        [Fact]
        public async Task DelegationCheck_InsufficientAccessLevel()
        {
            // Arrange
            int userId = 20000490;
            int reporteePartyId = 50005545;
            string resourceId = "scope-access-schema";

            var token = PrincipalUtil.GetToken(userId, 0, 2);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<RightDelegationCheckResultExternal> expectedResponse = GetExpectedDelegationStatus($"u{userId}", $"p{reporteePartyId}", resourceId, 2);
            StreamContent requestContent = GetDelegationCheckContent(resourceId);

            // Act
            HttpResponseMessage response = await _client.PostAsync($"accessmanagement/api/v1/{reporteePartyId}/maskinportenschema/delegationcheck", requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<RightDelegationCheckResultExternal> actualResponse = JsonSerializer.Deserialize<List<RightDelegationCheckResultExternal>>(responseContent, options);
            AssertionUtil.AssertCollections(expectedResponse, actualResponse, AssertionUtil.AssertRightDelegationCheckExternalEqual);
        }

        private HttpClient GetTestClient(IPDP pdpMock = null, IHttpContextAccessor httpContextAccessor = null, IDelegationMetadataRepository delegationMetadataRepositoryMock = null)
        {
            // Migrated stub kept to minimize diff in test bodies. pdpMock is honored
            // only if it's a PepWithPDPAuthorizationMock (this class is already that
            // configuration); switch to MaskinportenSchemaPdpPermitControllerTest if
            // PdpPermitMock is required. httpContextAccessor is honored by reading
            // its HttpContext.Request.RouteValues and applying them to the shared
            // MutableHttpContextAccessor. delegationMetadataRepositoryMock is
            // ignored — the repository is now a singleton; use
            // ResetDelegationMetadataRepository() to access/reset its state.
            if (httpContextAccessor is not null)
            {
                var ctx = httpContextAccessor.HttpContext;
                if (ctx is not null)
                {
                    foreach (var kvp in ctx.Request.RouteValues)
                    {
                        _httpContextAccessor.SetOverride(kvp.Key, (string)kvp.Value);
                    }
                }
            }

            if (delegationMetadataRepositoryMock is DelegationMetadataRepositoryMock providedRepo)
            {
                // Alias: share one dictionary instance between the singleton and the
                // caller's local variable so test assertions that read providedRepo
                // observe mutations performed through the request pipeline.
                var repo = (DelegationMetadataRepositoryMock)_fixture.Services.GetRequiredService<IDelegationMetadataRepository>();
                repo.MetadataChanges = new Dictionary<string, List<DelegationChange>>();
                providedRepo.MetadataChanges = repo.MetadataChanges;
            }

            return NewClient();
        }

        private static IHttpContextAccessor GetHttpContextAccessorMock(string partytype, string id)
        {
            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues.Add(partytype, id);
            return new HttpContextAccessor { HttpContext = httpContext };
        }

        private static List<MPDelegationExternal> GetExpectedMaskinportenSchemaDelegations(string supplierOrg, string consumerOrg, List<string> resourceIds)
        {
            List<MPDelegationExternal> delegations = new List<MPDelegationExternal>();
            delegations = TestDataUtil.GetAdminDelegations(supplierOrg, consumerOrg, resourceIds);
            return delegations;
        }

        private static StreamContent GetRequestContent(string operation, string resourceId, string from, string to, string inputFileName = "Input_Default")
        {
            Stream dataStream = File.OpenRead($"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/to_{to}/{inputFileName}.json");
            StreamContent content = new StreamContent(dataStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private static RightsDelegationResponseExternal GetExpectedResponse(string operation, string resourceId, string from, string to, string responseFileName = "ExpectedOutput_Default")
        {
            string responsePath = $"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/to_{to}/{responseFileName}.json";
            string content = File.ReadAllText(responsePath);
            return (RightsDelegationResponseExternal)JsonSerializer.Deserialize(content, typeof(RightsDelegationResponseExternal), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static ValidationProblemDetails GetExpectedValidationProblemDetails(string operation, string resourceId, string from, string to, string responseFileName = "ExpectedOutput_Default")
        {
            string responsePath = $"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/to_{to}/{responseFileName}.json";
            string content = File.ReadAllText(responsePath);
            return (ValidationProblemDetails)JsonSerializer.Deserialize(content, typeof(ValidationProblemDetails), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static ValidationProblemDetails GetExpectedProblemDetails(string operation, string resourceId, string from, string to, string responseFileName = "ExpectedOutput_Default")
        {
            string responsePath = $"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/to_{to}/{responseFileName}.json";
            string content = File.ReadAllText(responsePath);
            return (ValidationProblemDetails)JsonSerializer.Deserialize(content, typeof(ValidationProblemDetails), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static List<RightDelegationCheckResultExternal> GetExpectedDelegationStatus(string user, string from, string resourceId, int? authLevel = null)
        {
            string content;
            if (authLevel == null)
            {
                content = File.ReadAllText($"Data/Json/MaskinportenSchema/DelegationCheck/{resourceId}/from_{from}/authn_{user}.json");
            }
            else
            {
                content = File.ReadAllText($"Data/Json/MaskinportenSchema/DelegationCheck/{resourceId}/from_{from}/authn_{user}_authLevel{authLevel}.json");
            }

            return (List<RightDelegationCheckResultExternal>)JsonSerializer.Deserialize(content, typeof(List<RightDelegationCheckResultExternal>), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static ValidationProblemDetails GetExpectedValidationError(string operation, string user, string from, string resourceId, int? authLevel = null)
        {
            string content;
            if (authLevel == null)
            {
                content = File.ReadAllText($"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/authn_{user}.json");
            }
            else
            {
                content = File.ReadAllText($"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/authn_{user}_authLevel{authLevel}.json");
            }

            return (ValidationProblemDetails)JsonSerializer.Deserialize(content, typeof(ValidationProblemDetails), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static StreamContent GetDelegationCheckContent(string resourceId)
        {
            Stream dataStream = File.OpenRead($"Data/Json/MaskinportenSchema/DelegationCheck/{resourceId}/request.json");
            StreamContent content = new StreamContent(dataStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }
    }

    /// <summary>
    /// Sibling to <see cref="MaskinportenSchemaControllerTest"/> hosting the
    /// <c>PostMaskinportenSchemaDelegation_ValidationProblemDetails_*</c> tests
    /// that require <see cref="PdpPermitMock"/> instead of the default
    /// <see cref="PepWithPDPAuthorizationMock"/>. Split per recipe rule 6 (one
    /// mutually-exclusive DI configuration per class).
    /// </summary>
    public class MaskinportenSchemaPdpPermitControllerTest : IClassFixture<ApiFixture>
    {
        private readonly ApiFixture _fixture;
        private static readonly MutableHttpContextAccessor _httpContextAccessor = new();
        private readonly JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public MaskinportenSchemaPdpPermitControllerTest(ApiFixture fixture)
        {
            _fixture = fixture;
            _httpContextAccessor.ClearOverride();
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
                services.AddSingleton<IDelegationChangeEventQueue, DelegationChangeEventQueueMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPartiesClient, PartiesClientMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                services.RemoveAll<IHttpContextAccessor>();
                services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);
                services.RemoveAll<IPDP>();
                services.AddSingleton<IPDP, PdpPermitMock>();
            });
        }

        private HttpClient NewClient()
        {
            var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_SingleRightOnly()
        {
            _httpContextAccessor.SetOverride("party", "1");
            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "Input_SingleRightOnly");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "ExpectedOutput_SingleRightOnly");

            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/1/maskinportenschema/offered/", requestContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_OrgAppResource()
        {
            _httpContextAccessor.SetOverride("party", "1");
            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "Input_OrgAppResource");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "ExpectedOutput_OrgAppResource");

            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/1/maskinportenschema/offered/", requestContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_InvalidResourceRegistryId()
        {
            _httpContextAccessor.SetOverride("party", "1");
            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "Input_InvalidResourceRegistryId");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "ExpectedOutput_InvalidResourceRegistryId");

            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/1/maskinportenschema/offered/", requestContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_InvalidResourceType()
        {
            _httpContextAccessor.SetOverride("party", "1");
            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20001337, 50001337, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "Input_InvalidResourceType");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p1", "p2", "ExpectedOutput_InvalidResourceType");

            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/1/maskinportenschema/offered/", requestContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        [Fact]
        public async Task PostMaskinportenSchemaDelegation_ValidationProblemDetails_InvalidFrom_Ssn()
        {
            string fromParty = "50002598";
            _httpContextAccessor.SetOverride("party", "50002598");
            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PrincipalUtil.GetToken(20000490, 50002598, userPartyUuid: new Guid("f200a9cb-31ce-4ed6-aad3-ed08b3cbbeee")));
            client.DefaultRequestHeaders.Add("Altinn-Party-SocialSecurityNumber", "07124912037");

            StreamContent requestContent = GetRequestContent("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "p2", "Input_Default");
            ValidationProblemDetails expectedResponse = GetExpectedValidationProblemDetails("MaskinportenScopeDelegation", "mp_validation_problem_details", $"p{fromParty}", "p2", "ExpectedOutput_InvalidFrom_Ssn");

            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/person/maskinportenschema/offered/", requestContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            ValidationProblemDetails actualResponse = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, options);
            AssertionUtil.AssertValidationProblemDetailsEqual(expectedResponse, actualResponse);
        }

        private static StreamContent GetRequestContent(string operation, string resourceId, string from, string to, string inputFileName = "Input_Default")
        {
            Stream dataStream = File.OpenRead($"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/to_{to}/{inputFileName}.json");
            StreamContent content = new StreamContent(dataStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private static ValidationProblemDetails GetExpectedValidationProblemDetails(string operation, string resourceId, string from, string to, string responseFileName = "ExpectedOutput_Default")
        {
            string responsePath = $"Data/Json/MaskinportenSchema/{operation}/{resourceId}/from_{from}/to_{to}/{responseFileName}.json";
            string content = File.ReadAllText(responsePath);
            return (ValidationProblemDetails)JsonSerializer.Deserialize(content, typeof(ValidationProblemDetails), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
