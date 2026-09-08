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
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests for <see cref="BanckruptcyDelegationController"/>.
/// </summary>
public class BankruptcyDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/bankruptcyestate";

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/users

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.GetAgentAdminInformation(Guid, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Seed data (estate party = <see cref="TestEntities.OrganizationVerdiqAS"/>):
    /// <list type="bullet">
    ///   <item><description><see cref="TestEntities.PersonPaula"/> has only an Agent assignment (AgentForhold) -> <see cref="BankruptcyEstatePermissions.User"/>.</description></item>
    ///   <item><description><see cref="TestEntities.PersonOrjan"/> has only an administrator delegation (Rightholder + KonkursboAdministrator package) -> <see cref="BankruptcyEstatePermissions.Admin"/>.</description></item>
    ///   <item><description><see cref="TestEntities.PersonKasper"/> has both -> <see cref="BankruptcyEstatePermissions.UserAndAdmin"/>.</description></item>
    /// </list>
    /// </remarks>
    [IntegrationTest]
    public class GetAgentAdminInformation : IClassFixture<ApiFixture>
    {
        public GetAgentAdminInformation(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetAgentAdminInformation>(db =>
            {
                // Paula: only AgentForhold (Agent assignment)
                var agentToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula.Id,
                    RoleId = RoleConstants.Agent,
                };

                // Orjan: only admin access delegated (Rightholder + KonkursboAdministrator package)
                var rightholderToOrjan = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonOrjan.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                var adminPackageToOrjan = new AssignmentPackage()
                {
                    AssignmentId = rightholderToOrjan.Id,
                    PackageId = PackageConstants.KonkursboAdministrator.Id,
                };

                // Kasper: both AgentForhold and admin access delegated
                var agentToKasper = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonKasper.Id,
                    RoleId = RoleConstants.Agent,
                };
                var rightholderToKasper = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonKasper.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                var adminPackageToKasper = new AssignmentPackage()
                {
                    AssignmentId = rightholderToKasper.Id,
                    PackageId = PackageConstants.KonkursboAdministrator.Id,
                };

                db.Assignments.Add(agentToPaula);
                db.Assignments.Add(rightholderToOrjan);
                db.Assignments.Add(agentToKasper);
                db.Assignments.Add(rightholderToKasper);

                db.AssignmentPackages.Add(adminPackageToOrjan);
                db.AssignmentPackages.Add(adminPackageToKasper);

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
        /// A request without any authentication token cannot access the service.
        /// Expects 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task GetAgentAdminInformation_WithNoToken_Returns401Unauthorized()
        {
            var client = Fixture.Server.CreateClient();

            var response = await client.GetAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Authorized request returns the agents and administrators for the estate,
        /// including a user that only has AgentForhold, a user that only has admin
        /// access delegated, and a user that has both.
        /// </summary>
        [Fact]
        public async Task GetAgentAdminInformation_Authorized_Returns200WithUsersAndAdmins()
        {
            var client = CreateClient(TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.GetAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<BankruptcyEntityDto>>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            var paula = result.Items.FirstOrDefault(x => x.Id == TestEntities.PersonPaula.Id);
            var orjan = result.Items.FirstOrDefault(x => x.Id == TestEntities.PersonOrjan.Id);
            var kasper = result.Items.FirstOrDefault(x => x.Id == TestEntities.PersonKasper.Id);

            Assert.NotNull(paula);
            Assert.NotNull(orjan);
            Assert.NotNull(kasper);

            Assert.Equal(BankruptcyEstatePermissions.User, paula.Permissions);
            Assert.Equal(BankruptcyEstatePermissions.Admin, orjan.Permissions);
            Assert.Equal(BankruptcyEstatePermissions.UserAndAdmin, kasper.Permissions);
        }
    }

    #endregion
}
