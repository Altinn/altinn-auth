using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.Authorization.Integration.Platform.Samarbeidsportalen;
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
    /// Integration tests for the Maskinporten delegations lookup with
    /// <see cref="AccessMgmtFeatureFlags.MaskinportenValidateScopePrefixWithApi"/> on, so the scope is checked
    /// against the prefixes the caller owns in Samarbeidsportalen instead of the <c>consumer_prefix</c> claims.
    /// The prefix check and the delegation lookup are faked, so no database is needed;
    /// <see cref="MaskinportenDelegationsControllerTest"/> covers the flag off.
    /// </summary>
    [IntegrationTest]
    public class MaskinportenDelegationsControllerPrefixApiTest : IClassFixture<NoDbApiFixture>
    {
        private const string Endpoint = "accessmanagement/api/v1/maskinporten/delegations/";
        private const string ConsumerOrgNumber = "810418192";
        private const string ServiceOwnerOrgNumber = "310202398";
        private const string LookupScope = "altinn:maskinporten/delegations";

        private readonly NoDbApiFixture _fixture;
        private readonly HttpClient _client;
        private readonly FakeScopePrefixValidator _prefixes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskinportenDelegationsControllerPrefixApiTest"/> class.
        /// </summary>
        public MaskinportenDelegationsControllerPrefixApiTest(NoDbApiFixture fixture)
        {
            _fixture = fixture;
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.MaskinportenValidateScopePrefixWithApi);
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
                services.RemoveAll<IMaskinportenDelegationLookupService>();
                services.AddSingleton<IMaskinportenDelegationLookupService>(new EmptyLookupService());
                services.RemoveAll<IScopePrefixValidator>();
                services.AddSingleton<IScopePrefixValidator, FakeScopePrefixValidator>();
            });

            _client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // The host, and so the fake, is shared by every test in the class: start each test from a clean fake.
            _prefixes = (FakeScopePrefixValidator)_fixture.Services.GetRequiredService<IScopePrefixValidator>();
            _prefixes.Reset();
            _prefixes.Own(ServiceOwnerOrgNumber, "mp");
        }

        [Fact]
        public async Task OwnedScope_Returns200()
        {
            _client.DefaultRequestHeaders.Authorization = Bearer(PrincipalUtil.GetOrgToken("SERVICEOWNER", ServiceOwnerOrgNumber, LookupScope));

            HttpResponseMessage response = await GetDelegations("mp:test/theworld.read");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            (string? org, string scope) = Assert.Single(_prefixes.Calls);
            Assert.Equal(ServiceOwnerOrgNumber, org);
            Assert.Equal("mp:test/theworld.read", scope);
        }

        [Fact]
        public async Task UnownedScope_ReturnsForbidden()
        {
            _client.DefaultRequestHeaders.Authorization = Bearer(PrincipalUtil.GetOrgToken("SERVICEOWNER", ServiceOwnerOrgNumber, LookupScope));

            HttpResponseMessage response = await GetDelegations("altinn:instances.read");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("Not authorized for lookup of delegations for the scope: altinn:instances.read", content);
        }

        [Fact]
        public async Task ConsumerPrefixClaim_IsIgnored()
        {
            // The token claims the altinn prefix, but Samarbeidsportalen says the caller only owns mp.
            _client.DefaultRequestHeaders.Authorization = Bearer(PrincipalUtil.GetOrgToken("SERVICEOWNER", ServiceOwnerOrgNumber, LookupScope, consumerPrefix: ["altinn"]));

            HttpResponseMessage response = await GetDelegations("altinn:instances.read");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PrefixDataUnavailable_Returns500()
        {
            _prefixes.Unavailable = true;
            _client.DefaultRequestHeaders.Authorization = Bearer(PrincipalUtil.GetOrgToken("SERVICEOWNER", ServiceOwnerOrgNumber, LookupScope));

            HttpResponseMessage response = await GetDelegations("mp:test/theworld.read");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task AdminScope_DoesNotCheckPrefixes()
        {
            _prefixes.Unavailable = true;
            _client.DefaultRequestHeaders.Authorization = Bearer(PrincipalUtil.GetOrgToken("DIGDIR", "991825827", "altinn:maskinporten/delegations.admin"));

            HttpResponseMessage response = await GetDelegations("skd:test");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_prefixes.Calls);
        }

        [Fact]
        public async Task NoScopeParameter_ReturnsForbidden_WithoutCheckingPrefixes()
        {
            _client.DefaultRequestHeaders.Authorization = Bearer(PrincipalUtil.GetOrgToken("SERVICEOWNER", ServiceOwnerOrgNumber, LookupScope));

            HttpResponseMessage response = await _client.GetAsync($"{Endpoint}?consumerorg={ConsumerOrgNumber}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(_prefixes.Calls);
        }

        [Theory]
        [InlineData(null)] // no consumer claim
        [InlineData("not json")]
        [InlineData("""{ "authority":"iso6523-actorid-upis", "ID":"0192:12345"}""")] // not a valid organization number
        [InlineData("null")] // valid JSON, but not an object
        [InlineData("[]")]
        [InlineData("""{ "authority":"iso6523-actorid-upis", "ID":991825827}""")] // ID is not a string
        public async Task MissingOrMalformedConsumerClaim_ReturnsForbidden(string? consumerClaim)
        {
            ClaimsPrincipal principal = PrincipalUtil.GetClaimsPrincipal("SERVICEOWNER", ServiceOwnerOrgNumber, LookupScope);
            ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;
            identity.RemoveClaim(identity.FindFirst("consumer"));
            if (consumerClaim is not null)
            {
                identity.AddClaim(new Claim("consumer", consumerClaim));
            }

            _client.DefaultRequestHeaders.Authorization = Bearer(JwtTokenMock.GenerateToken(principal, TimeSpan.FromMinutes(30)));

            HttpResponseMessage response = await GetDelegations("mp:test/theworld.read");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            (string? org, _) = Assert.Single(_prefixes.Calls);
            Assert.Null(org);
        }

        private Task<HttpResponseMessage> GetDelegations(string scope)
            => _client.GetAsync($"{Endpoint}?consumerorg={ConsumerOrgNumber}&scope={scope}", TestContext.Current.CancellationToken);

        private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

        /// <summary>
        /// Stands in for the Samarbeidsportalen prefix check: owns what <see cref="Own"/> says,
        /// fails when <see cref="Unavailable"/> is set, and records each organization number and scope it is asked about.
        /// </summary>
        private sealed class FakeScopePrefixValidator : IScopePrefixValidator
        {
            private readonly Dictionary<string, List<string>> _owned = [];

            public bool Unavailable { get; set; }

            public List<(string? OrganizationNumber, string Scope)> Calls { get; } = [];

            public void Reset()
            {
                _owned.Clear();
                Calls.Clear();
                Unavailable = false;
            }

            public void Own(string organizationNumber, string prefix)
            {
                if (!_owned.TryGetValue(organizationNumber, out List<string>? prefixes))
                {
                    _owned[organizationNumber] = prefixes = [];
                }

                prefixes.Add(prefix);
            }

            public ValueTask<IReadOnlyList<string>> GetUnownedScopes(string? organizationNumber, IEnumerable<string> scopes, CancellationToken cancellationToken = default)
            {
                List<string> requested = scopes.ToList();
                Calls.AddRange(requested.Select(scope => (organizationNumber, scope)));

                if (Unavailable)
                {
                    throw new ScopePrefixDataUnavailableException(new HttpRequestException("API is down"));
                }

                List<string> owned = organizationNumber is not null && _owned.TryGetValue(organizationNumber, out List<string>? prefixes) ? prefixes : [];
                IReadOnlyList<string> unowned = requested.Where(scope => !owned.Any(prefix => scope.StartsWith(prefix + ":", StringComparison.Ordinal))).ToList();
                return ValueTask.FromResult(unowned);
            }
        }

        private sealed class EmptyLookupService : IMaskinportenDelegationLookupService
        {
            public Task<List<Delegation>> GetMaskinportenDelegations(string? supplierOrg, string? consumerOrg, string? scope, CancellationToken cancellationToken = default)
                => Task.FromResult(new List<Delegation>());
        }
    }
}
