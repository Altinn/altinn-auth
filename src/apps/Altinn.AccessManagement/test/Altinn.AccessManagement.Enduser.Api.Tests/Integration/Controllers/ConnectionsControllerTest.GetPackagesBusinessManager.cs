using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on the entity-variant scoping of the
/// forretningsfoerer (<see cref="RoleConstants.BusinessManager"/>) role packages: inside a securities
/// fund (<see cref="EntityVariantConstants.VPFO"/>) a forretningsfoerer gets the same access packages
/// as the fund's daglig leder (<see cref="RoleConstants.ManagingDirector"/>), while inside a limited
/// company (<see cref="EntityVariantConstants.AS"/>) the very same role gets none of them.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetPackages(Guid, Guid?, Guid?, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// covering the role packages the static ingest scopes to an entity variant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ingest gives the forretningsfoerer role its packages with an entity variant set, while the
    /// daglig leder gets them unscoped. The connection query resolves a role package either when it
    /// carries no variant or when it matches the variant of the party the assignment is from, so the
    /// endpoint is what proves the rule end to end rather than the seed rows on their own.
    /// </para>
    /// <para>
    /// Seeded by this class (all rows are new; nothing existing is mutated):
    /// - Nordstjernen Verdipapirfond (VPFO): Geir Pedersen as daglig leder, Marit Eriksen as forretningsfoerer.
    /// - Nordstjernen Forvaltning AS (AS): Trond Larsen as forretningsfoerer.
    /// </para>
    /// <para>
    /// Each actor queries the from-others direction for their own connection, which is the
    /// "what does this organisation give me" view.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    [Collection(ConnectionsReadOnlyCollection.Name)]
    public class GetPackagesBusinessManager
    {
        private static readonly Guid SecuritiesFundId = Guid.Parse("0196a0b1-0003-7001-8001-000000000001");
        private static readonly Guid LimitedCompanyId = Guid.Parse("0196a0b1-0003-7001-8001-000000000002");

        public GetPackagesBusinessManager(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetPackagesBusinessManager>(db =>
            {
                db.Entities.AddRange(
                    new Entity
                    {
                        Id = SecuritiesFundId,
                        Name = "Nordstjernen Verdipapirfond",
                        RefId = "310000011",
                        OrganizationIdentifier = "310000011",
                        PartyId = 50400001,
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.VPFO,
                    },
                    new Entity
                    {
                        Id = LimitedCompanyId,
                        Name = "Nordstjernen Forvaltning AS",
                        RefId = "310000012",
                        OrganizationIdentifier = "310000012",
                        PartyId = 50400002,
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                    });
                db.SaveChanges();

                db.Assignments.AddRange(
                    new Assignment
                    {
                        FromId = SecuritiesFundId,
                        ToId = TestData.GeirPedersen.Id,
                        RoleId = RoleConstants.ManagingDirector,
                    },
                    new Assignment
                    {
                        FromId = SecuritiesFundId,
                        ToId = TestData.MaritEriksen.Id,
                        RoleId = RoleConstants.BusinessManager,
                    },
                    new Assignment
                    {
                        FromId = LimitedCompanyId,
                        ToId = TestData.TrondLarsen.Id,
                        RoleId = RoleConstants.BusinessManager,
                    });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// The forretningsfoerer of the securities fund holds exactly the packages the fund's daglig
        /// leder holds - the rule the VPFO-scoped role packages exist to produce.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsBusinessManagerOfSecuritiesFund_ReturnsSamePackagesAsManagingDirector()
        {
            List<string> managingDirectorPackages = await GetPackageUrns(TestData.GeirPedersen.Id, SecuritiesFundId);
            List<string> businessManagerPackages = await GetPackageUrns(TestData.MaritEriksen.Id, SecuritiesFundId);

            Assert.NotEmpty(managingDirectorPackages);
            Assert.Equal(managingDirectorPackages, businessManagerPackages);
        }

        /// <summary>
        /// The forretningsfoerer of a limited company holds no packages at all: every role package for
        /// the role is scoped to some other entity variant, so none of them resolve for an AS.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsBusinessManagerOfLimitedCompany_ReturnsNoPackages()
        {
            List<string> businessManagerPackages = await GetPackageUrns(TestData.TrondLarsen.Id, LimitedCompanyId);

            Assert.Empty(businessManagerPackages);
        }

        private HttpClient CreateClient(Guid partyUuid, params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        /// <summary>
        /// Asks the endpoint, as <paramref name="personId"/> themselves, which packages
        /// <paramref name="organizationId"/> gives them, and returns the package URNs sorted so that
        /// two results can be compared directly.
        /// </summary>
        private async Task<List<string>> GetPackageUrns(Guid personId, Guid organizationId)
        {
            HttpClient client = CreateClient(personId, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={personId}&from={organizationId}&to={personId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<PackagePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);

            return result.Items.Select(p => p.Package.Urn).Order(StringComparer.Ordinal).ToList();
        }
    }
}
