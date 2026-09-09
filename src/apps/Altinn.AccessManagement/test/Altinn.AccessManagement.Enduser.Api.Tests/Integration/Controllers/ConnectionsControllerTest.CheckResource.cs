using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Nordis AS -> Verdiq AS (Rightholder)
    /// </para>
    /// <para>
    /// Pre-seeded via <see cref="TestDataSeeds"/>:
    /// - Resource "Dialogs for sickness benefits" (nav_sykepenger_dialog)
    /// - Resource "Omsetningsoppgave for alkohol" (app_dihe_omsetningsoppgave-for-alkohol)
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for resource registry policy lookups
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policy evaluation
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (has both role and package access)
    /// - Thea: rightholder of Dumbo Adventures (has package access only, no role access)
    /// </para>
    /// <para>
    /// The tests verify delegation check results including full access (roles + packages),
    /// package-only access, partial access (some rights granted, some denied), and scope enforcement.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class CheckResource : IClassFixture<ApiFixture>
    {
        private static readonly Guid ClientDelegationClient = Guid.Parse("0196b130-0000-7000-8000-000000000001");
        private static readonly Guid ClientDelegationFacilitator = Guid.Parse("0196b130-0000-7000-8000-000000000002");
        private static readonly Guid ClientDelegationAgent = Guid.Parse("0196b130-0000-7000-8000-000000000003");

        public CheckResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce<CheckResource>(db =>
            {
                var rightholderFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.SaveChanges();

                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = ClientDelegationClient,
                        Name = "Havna Fiskemat AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950061",
                        RefId = "399950061",
                        PartyId = 50950061,
                    },
                    new Entity()
                    {
                        Id = ClientDelegationFacilitator,
                        Name = "Sundet Regnskap AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950062",
                        RefId = "399950062",
                        PartyId = 50950062,
                    },
                    new Entity()
                    {
                        Id = ClientDelegationAgent,
                        Name = "Jonas Sundet",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "23019099937",
                        RefId = "23019099937",
                        PartyId = 50950063,
                        UserId = 50950063,
                        DateOfBirth = new DateOnly(1990, 1, 23),
                    });

                db.SaveChanges();

                var accountantFromClientToFacilitator = new Assignment()
                {
                    FromId = ClientDelegationClient,
                    ToId = ClientDelegationFacilitator,
                    RoleId = RoleConstants.Accountant,
                };

                var agentFromFacilitatorToJonas = new Assignment()
                {
                    FromId = ClientDelegationFacilitator,
                    ToId = ClientDelegationAgent,
                    RoleId = RoleConstants.Agent,
                };

                var assignmentResourceForClient = new AssignmentResource()
                {
                    AssignmentId = accountantFromClientToFacilitator.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = "sirius-skattemelding-v1/50083510/p50155461/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var delegationToJonas = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromClientToFacilitator.Id,
                    ToId = agentFromFacilitatorToJonas.Id,
                    FacilitatorId = ClientDelegationFacilitator,
                };

                db.Assignments.Add(accountantFromClientToFacilitator);
                db.Assignments.Add(agentFromFacilitatorToJonas);
                db.AssignmentResources.Add(assignmentResourceForClient);
                db.Delegations.Add(delegationToJonas);
                db.DelegationResources.Add(new DelegationResource()
                {
                    DelegationId = delegationToJonas.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    AssignmentResourceId = assignmentResourceForClient.Id,
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
        /// Malin (MD of Dumbo) checks nav_sykepenger_dialog. Has full access via both roles and packages.
        /// Expects OK with 3 rights (read, access, subscribe) all granted with RoleAccess and PackageAccess.
        /// </summary>
        [Fact]
        public async Task CheckResource_NavSykemeldingDialog_FullAccess_RolesAndPackages_Returns200WithRightsGrantedByRolesAndPackages()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=nav_sykepenger_dialog", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == false);
            Assert.Equal(3, result.Rights.Count());

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto accessRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("access", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(accessRight);
            Assert.True(accessRight.Result, "The 'access' right should have Result = true");
            Assert.NotEmpty(accessRight.ReasonCodes);
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto subscribeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("subscribe", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(subscribeRight);
            Assert.True(subscribeRight.Result, "The 'subscribe' right should have Result = true");
            Assert.NotEmpty(subscribeRight.ReasonCodes);
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));
        }

        /// <summary>
        /// Thea (rightholder of Dumbo) checks nav_sykepenger_dialog
        /// Expects OK with 3 rights all granted with PackageAccess but not RoleAccess.
        /// </summary>
        [Fact]
        public async Task CheckResource_NavSykemeldingDialog_FullAccess_Packages_Returns200WithRightsGrantedByPackages()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=nav_sykepenger_dialog", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == false);
            Assert.Equal(3, result.Rights.Count());

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto accessRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("access", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(accessRight);
            Assert.True(accessRight.Result, "The 'access' right should have Result = true");
            Assert.NotEmpty(accessRight.ReasonCodes);
            Assert.DoesNotContain(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto subscribeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("subscribe", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(subscribeRight);
            Assert.True(subscribeRight.Result, "The 'subscribe' right should have Result = true");
            Assert.NotEmpty(subscribeRight.ReasonCodes);
            Assert.DoesNotContain(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));
        }

        /// <summary>
        /// Malin (MD of Dumbo) checks app_dihe_omsetningsoppgave-for-alkohol. Has partial access: read and write
        /// granted via roles, but sign denied with MissingRoleAccess and MissingDelegationAccess.
        /// Expects OK with 17 rights (mix of granted and denied).
        /// </summary>
        [Fact]
        public async Task CheckResource_DiheOmsetningsoppgave_PartialAccess_RolesAndPackages_Returns200WithPartiallyGrantedRights()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_dihe_omsetningsoppgave-for-alkohol", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.Contains(result.Rights, r => r.Result == false);
            Assert.Contains(result.Rights, r => r.Result == true);
            Assert.Equal(17, result.Rights.Count());

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto writeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("Write (Task_1)", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(writeRight);
            Assert.True(writeRight.Result, "The 'Write' right should have Result = true");
            Assert.NotEmpty(writeRight.ReasonCodes);
            Assert.Contains(writeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.DoesNotContain(writeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto signRight = result.Rights.FirstOrDefault(r => r.Right.Name.Contains("sign", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(signRight);
            Assert.False(signRight.Result, "The 'sign' right should have Result = false");
            Assert.NotEmpty(signRight.ReasonCodes);
            Assert.Contains(signRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingRoleAccess));
            Assert.Contains(signRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingDelegationAccess));
        }

        /// <summary>
        /// Jonas is an agent who reaches Havna Fiskemat's Skattemelding only through a client delegation.
        /// Holding a resource that way lets him use it, but never delegate it onward, so the delegation check
        /// must deny every right. This pins the delegation check against the client delegated holdings the
        /// read endpoints report.
        /// </summary>
        [Fact]
        public async Task CheckResource_AsAgentHoldingResourceOnlyByClientDelegation_Returns200WithAllRightsDeniedForMissingDelegationAccess()
        {
            HttpClient client = CreateClient(ClientDelegationAgent, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={ClientDelegationClient}&resource=app_skd_sirius-skattemelding-v1", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == true);

            foreach (RightCheckDto right in result.Rights)
            {
                Assert.Contains(right.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingDelegationAccess));
                Assert.DoesNotContain(right.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.DelegationAccess));
            }
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithReadScope_Returns403ForReadScope()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            var response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=test-delegation-check-resource", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithFromOthersWriteScope_Returns403ForFromOthersWriteScope()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            var response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=test-delegation-check-resource", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
