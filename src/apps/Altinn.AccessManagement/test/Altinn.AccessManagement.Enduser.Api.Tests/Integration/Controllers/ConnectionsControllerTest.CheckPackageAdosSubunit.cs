using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Regression coverage for the ADOS subunit package delegation bug (issue 4029): the Daglig-leder
/// (ManagingDirector) of an ADOS mainunit must be able to delegate the access packages he holds through
/// the ManagingDirector role on the mainunit, from the ADOS subunit to a third party.
/// <para>
/// The <c>mainUnit</c> CTE in <c>PackageDelegationCheckQuery</c> resolves the mainunit through an assignment
/// with the <c>administrativ-enhet-offentlig-sektor</c> (ADOS) role, so the delegable packages surface with a
/// "MainUnit" reason even though the delegation check is performed on behalf of the ADOS subunit.
/// </para>
/// </summary>
public partial class ConnectionsControllerTest
{
    [IntegrationTest]
    public class CheckPackageAdosSubunit : IClassFixture<ApiFixture>
    {
        private static readonly Guid AdosMainUnitId = Guid.Parse("0196b160-0000-7000-8000-000000000001");
        private static readonly Guid AdosSubUnitId = Guid.Parse("0196b160-0000-7000-8000-000000000002");
        private static readonly Guid DagligLederId = Guid.Parse("0196b160-0000-7000-8000-000000000003");

        public CheckPackageAdosSubunit(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<CheckPackageAdosSubunit>(db =>
            {
                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = AdosMainUnitId,
                        Name = "ADOS Mainunit DelegationCheck",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.ORGL,
                        OrganizationIdentifier = "399960001",
                        RefId = "399960001",
                        PartyId = 50960001,
                    },
                    new Entity()
                    {
                        Id = AdosSubUnitId,
                        Name = "ADOS Subunit DelegationCheck",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.ADOS,
                        OrganizationIdentifier = "399960002",
                        RefId = "399960002",
                        ParentId = AdosMainUnitId,
                        PartyId = 50960002,
                    },
                    new Entity()
                    {
                        Id = DagligLederId,
                        Name = "Dagny DagligLeder",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "25019099951",
                        RefId = "25019099951",
                        PartyId = 50960003,
                        UserId = 50960003,
                        DateOfBirth = new DateOnly(1990, 1, 25),
                    });
                db.SaveChanges();

                // The ADOS subunit points to its mainunit via the ADOS (administrativ-enhet-offentlig-sektor) role.
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosSubUnitId,
                    ToId = AdosMainUnitId,
                    RoleId = RoleConstants.AdministrativeUnitPublicSector,
                });

                // Dagny is Managing Director (DAGL) of the ADOS mainunit and therefore inherits its access packages.
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosMainUnitId,
                    ToId = DagligLederId,
                    RoleId = RoleConstants.ManagingDirector,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

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
        /// Dagny (Daglig-leder of the ADOS mainunit) runs a package delegation check on behalf of the ADOS subunit.
        /// The packages she holds through the ManagingDirector role on the mainunit must be delegable from the
        /// subunit, surfaced with a "MainUnit" reason.
        /// </summary>
        [Fact]
        public async Task CheckPackage_AsDagligLederOfAdosMainUnit_ReturnsPackagesDelegableFromAdosSubunit()
        {
            HttpClient client = CreateClient(DagligLederId, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={AdosSubUnitId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            // At least one package must be delegable, resolved through the mainunit inheritance branch of the query.
            Assert.Contains(
                result.Items,
                p => p.Result && p.Reasons.Any(r => (r.Description ?? string.Empty).Contains("MainUnit", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
