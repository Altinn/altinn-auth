using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests the <c>hovedadministrator</c> (main-administrator) fallback branch of
/// <c>PackageDelegationCheckQuery</c>: an actor that holds the main-administrator access package on
/// behalf of a party can delegate the full main-administrator package set. Every other delegation-check
/// test uses a managing-director or rightholder actor, so this CROSS-JOIN fallback was otherwise unexercised.
/// </summary>
public partial class ConnectionsControllerTest
{
    [IntegrationTest]
    public class CheckPackageMainAdmin : IClassFixture<ApiFixture>
    {
        public CheckPackageMainAdmin(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<CheckPackageMainAdmin>(db =>
            {
                // Leia holds the main-administrator access package on behalf of Han Solo Enterprise.
                var mainAdminAssignment = new Assignment
                {
                    FromId = TestData.HanSoloEnterprise.Id,
                    ToId = TestData.LeiaOrgana.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(mainAdminAssignment);
                db.SaveChanges();

                db.AssignmentPackages.Add(new AssignmentPackage
                {
                    AssignmentId = mainAdminAssignment.Id,
                    PackageId = PackageConstants.MainAdministrator.Id,
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
        /// Leia (main-administrator for Han Solo Enterprise) runs a package delegation check on the
        /// enterprise's behalf. The check resolves her delegable packages through the main-administrator
        /// fallback, so the result is non-empty and at least one package is delegable for the
        /// "HovedAdmin" reason that only that branch produces.
        /// </summary>
        [Fact]
        public async Task CheckPackage_AsMainAdministrator_ReturnsPackagesDelegableViaHovedAdminFallback()
        {
            HttpClient client = CreateClient(TestData.LeiaOrgana.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.HanSoloEnterprise.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            // The main-administrator fallback tags the packages it adds with a "...-HovedAdmin" reason.
            Assert.Contains(
                result.Items,
                p => p.Result && p.Reasons.Any(r => (r.Description ?? string.Empty).Contains("HovedAdmin", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
