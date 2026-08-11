using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the GetResourceRights endpoint which returns direct and indirect rights for a specific resource between two parties. The tests cover both to-others and from-others query directions, verifying correct scope requirements and response content based on seeded data and actor perspectives.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetResourceRights(Guid, Guid, Guid, string, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - ResourceType "Test"
    /// - Resource "Skattemelding" (app_skd_skattemelding)
    /// - Assignment: Dumbo Adventures -> Mille Hundefrisør (Rightholder)
    /// - AssignmentResource linking Skattemelding to the assignment above
    /// - A dedicated client delegation chain on its own parties: Bryggen Bokhandel AS (client) -> Havly Regnskap AS
    ///   (facilitator, Accountant) -> Iver Havly (agent, Agent), where Skattemelding is delegated to the agent and
    ///   the rights are held in the policy of the client-side AssignmentResource
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (views from Dumbo's perspective)
    /// - Thea: managing director of Mille Hundefrisør (views from Mille's perspective)
    /// </para>
    /// <para>
    /// The tests verify that the endpoint returns direct and indirect rights for a specific resource
    /// between two parties, and that the correct bidirectional read scope is required depending on
    /// the direction of the query (to-others vs from-others). Mismatched scopes result in HTTP 403 Forbidden.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class GetResourceRights : IClassFixture<ApiFixture>
    {
        private static readonly Guid ClientDelegationClient = Guid.Parse("0196b110-0000-7000-8000-000000000001");
        private static readonly Guid ClientDelegationFacilitator = Guid.Parse("0196b110-0000-7000-8000-000000000002");
        private static readonly Guid ClientDelegationAgent = Guid.Parse("0196b110-0000-7000-8000-000000000003");
        private static readonly Guid MixedClient = Guid.Parse("0196b111-0000-7000-8000-000000000001");
        private static readonly Guid MixedFacilitator = Guid.Parse("0196b111-0000-7000-8000-000000000002");
        private static readonly Guid MixedAgent = Guid.Parse("0196b111-0000-7000-8000-000000000003");

        /// <summary>
        /// Delegation policy holding the nine rights the direct grants in this class are built from.
        /// </summary>
        private const string DirectPolicyPath = "sirius-skattemelding-v1/50083510/p50155461/delegationpolicy.xml";

        /// <summary>
        /// Delegation policy used by the client delegated grants: read (also held directly in the mixed case)
        /// and delete (held nowhere else), so a grant resolved from the wrong policy is visible in the response.
        /// </summary>
        private const string ClientDelegationPolicyPath = "sirius-skattemelding-v1/50950021/p50950023/delegationpolicy.xml";

        public GetResourceRights(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce<GetResourceRights>(db =>
            {
                var rightholderFromDumboToMille = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.MilleHundefrisor.Id,
                    RoleId = RoleConstants.Rightholder
                };

                db.Assignments.Add(rightholderFromDumboToMille);
                db.SaveChanges();

                db.AssignmentResources.Add(new AssignmentResource()
                {
                    AssignmentId = rightholderFromDumboToMille.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = DirectPolicyPath
                });

                db.SaveChanges();

                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = ClientDelegationClient,
                        Name = "Bryggen Bokhandel AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950021",
                        RefId = "399950021",
                        PartyId = 50950021,
                    },
                    new Entity()
                    {
                        Id = ClientDelegationFacilitator,
                        Name = "Havly Regnskap AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950022",
                        RefId = "399950022",
                        PartyId = 50950022,
                    },
                    new Entity()
                    {
                        Id = ClientDelegationAgent,
                        Name = "Iver Havly",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "19019099933",
                        RefId = "19019099933",
                        PartyId = 50950023,
                        UserId = 50950023,
                        DateOfBirth = new DateOnly(1990, 1, 19),
                    });

                db.SaveChanges();

                var accountantFromClientToFacilitator = new Assignment()
                {
                    FromId = ClientDelegationClient,
                    ToId = ClientDelegationFacilitator,
                    RoleId = RoleConstants.Accountant,
                };

                var agentFromFacilitatorToIver = new Assignment()
                {
                    FromId = ClientDelegationFacilitator,
                    ToId = ClientDelegationAgent,
                    RoleId = RoleConstants.Agent,
                };

                var assignmentResourceForClient = new AssignmentResource()
                {
                    AssignmentId = accountantFromClientToFacilitator.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = ClientDelegationPolicyPath,
                    PolicyVersion = "1.0",
                };

                var delegationToIver = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromClientToFacilitator.Id,
                    ToId = agentFromFacilitatorToIver.Id,
                    FacilitatorId = ClientDelegationFacilitator,
                };

                db.Assignments.Add(accountantFromClientToFacilitator);
                db.Assignments.Add(agentFromFacilitatorToIver);
                db.AssignmentResources.Add(assignmentResourceForClient);
                db.Delegations.Add(delegationToIver);
                db.DelegationResources.Add(new DelegationResource()
                {
                    DelegationId = delegationToIver.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    AssignmentResourceId = assignmentResourceForClient.Id,
                });

                db.SaveChanges();

                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = MixedClient,
                        Name = "Torvet Delikatesse AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950051",
                        RefId = "399950051",
                        PartyId = 50950051,
                    },
                    new Entity()
                    {
                        Id = MixedFacilitator,
                        Name = "Bakken Regnskap AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950052",
                        RefId = "399950052",
                        PartyId = 50950052,
                    },
                    new Entity()
                    {
                        Id = MixedAgent,
                        Name = "Sigrid Bakken",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "22019099936",
                        RefId = "22019099936",
                        PartyId = 50950053,
                        UserId = 50950053,
                        DateOfBirth = new DateOnly(1990, 1, 22),
                    });

                db.SaveChanges();

                // Sigrid holds the resource twice over: directly as a rightholder of Torvet Delikatesse, and
                // through the client delegation Bakken Regnskap made to her on the same client's behalf.
                var rightholderFromMixedClientToAgent = new Assignment()
                {
                    FromId = MixedClient,
                    ToId = MixedAgent,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromMixedClientToFacilitator = new Assignment()
                {
                    FromId = MixedClient,
                    ToId = MixedFacilitator,
                    RoleId = RoleConstants.Accountant,
                };

                var agentFromMixedFacilitatorToSigrid = new Assignment()
                {
                    FromId = MixedFacilitator,
                    ToId = MixedAgent,
                    RoleId = RoleConstants.Agent,
                };

                var directResourceForMixedAgent = new AssignmentResource()
                {
                    AssignmentId = rightholderFromMixedClientToAgent.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = DirectPolicyPath,
                    PolicyVersion = "1.0",
                };

                var assignmentResourceForMixedClient = new AssignmentResource()
                {
                    AssignmentId = accountantFromMixedClientToFacilitator.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = ClientDelegationPolicyPath,
                    PolicyVersion = "1.0",
                };

                var delegationToSigrid = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromMixedClientToFacilitator.Id,
                    ToId = agentFromMixedFacilitatorToSigrid.Id,
                    FacilitatorId = MixedFacilitator,
                };

                db.Assignments.Add(rightholderFromMixedClientToAgent);
                db.Assignments.Add(accountantFromMixedClientToFacilitator);
                db.Assignments.Add(agentFromMixedFacilitatorToSigrid);
                db.AssignmentResources.Add(directResourceForMixedAgent);
                db.AssignmentResources.Add(assignmentResourceForMixedClient);
                db.Delegations.Add(delegationToSigrid);
                db.DelegationResources.Add(new DelegationResource()
                {
                    DelegationId = delegationToSigrid.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    AssignmentResourceId = assignmentResourceForMixedClient.Id,
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
        /// Malin (MD of Dumbo) queries resource rights for Skattemelding delegated to Mille in the to-others direction.
        /// Expects OK with a valid response containing the resource.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsManagingDirectorToOrganization_WithToOthersScope_Returns200WithDirectRights()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.IndirectRights);
            Assert.NotEmpty(resourceRightsDto.DirectRights);
            Assert.Equal(9, resourceRightsDto.DirectRights.Count); // 9 inherited rights from Dumbo to Mille via the Rightholder role
            foreach (var right in resourceRightsDto.DirectRights)
            {
                // All rights to Mille should be direct via Dumbo's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.MilleHundefrisor.Entity.Name);
                Assert.True(permission.To.Id == TestData.MilleHundefrisor.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Thea (MD of Mille) queries resource rights for Skattemelding received from Dumbo in the from-others direction.
        /// Expects OK with a valid response containing the resource.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsManagingDirectorFromOtherOrganization_WithFromOthersScope_Returns200WithDirectRights()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.IndirectRights);
            Assert.NotEmpty(resourceRightsDto.DirectRights);
            Assert.Equal(9, resourceRightsDto.DirectRights.Count); // 9 inherited rights from Dumbo to Mille via the Rightholder role
            foreach (var right in resourceRightsDto.DirectRights)
            {
                // All rights to Mille should be direct via Dumbo's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.MilleHundefrisor.Entity.Name);
                Assert.True(permission.To.Id == TestData.MilleHundefrisor.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Malin (MD of Dumbo) queries resource rights for Skattemelding for Thea in the to-others direction.
        /// Thea is not a direct rightholder for Skattemelding — instead she inherits access through her
        /// ManagingDirector role at Mille Hundefrisor, which IS a rightholder of Dumbo (key-role inheritance / nøkkelrollearv).
        /// Expects OK with 9 IndirectRights (KeyRole reason), no DirectRights.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsManagingDirectorToRightholder_WithToOthersScope_Returns200WithKeyRoleIndirectRights()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);
            Assert.Equal(9, resourceRightsDto.IndirectRights.Count); // 9 inherited rights from Mille's Rightholder role
            foreach (var right in resourceRightsDto.IndirectRights)
            {
                // All rights to Thea should be indirect via Mille's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.Thea.Entity.Name);
                Assert.True(permission.To.Id == TestData.Thea.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Malin (MD of Dumbo) queries resource rights for Skattemelding for Milena in the to-others direction.
        /// Milena is Chair of the Board at Mille Hundefrisor, which is a rightholder of Dumbo. She inherits
        /// access indirectly through her key-role at Mille (nøkkelrollearv).
        /// Expects OK with 9 IndirectRights (KeyRole reason), no DirectRights.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsManagingDirectorToKeyRolePerson_WithToOthersScope_Returns200WithKeyRoleIndirectRights()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Milena.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);
            Assert.Equal(9, resourceRightsDto.IndirectRights.Count); // 9 inherited rights from Mille's Rightholder role
            foreach (var right in resourceRightsDto.IndirectRights)
            {
                // All rights to Milena should be indirect via Mille's Rightholder role Chair of board, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.Milena.Entity.Name);
                Assert.True(permission.To.Id == TestData.Milena.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Thea queries her own resource rights for Skattemelding from Dumbo in the from-others direction.
        /// Thea inherits access indirectly through her ManagingDirector role at Mille Hundefrisor,
        /// which is a rightholder of Dumbo (key-role inheritance).
        /// Expects OK with 9 IndirectRights (KeyRole reason), no DirectRights.
        /// Note: method name says "ToOthers" but the query is actually from-others (party=Thea, from=Dumbo, to=Thea).
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsRightholderViaKeyRoleToSelf_WithToOthersScope_Returns200WithKeyRoleIndirectRights()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.Thea.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);
            Assert.Equal(9, resourceRightsDto.IndirectRights.Count); // 9 inherited rights from Mille's Rightholder role
            foreach (var right in resourceRightsDto.IndirectRights)
            {
                // All rights to Thea should be indirect via Mille's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.Thea.Entity.Name);
                Assert.True(permission.To.Id == TestData.Thea.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Bryggen Bokhandel queries the rights its agent Iver holds on Skattemelding. Iver holds them only through
        /// the client delegation, so the rights are resolved from the policy of the client-side AssignmentResource
        /// and reported as indirect with the ClientDelegation reason.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_ForClientDelegatedResource_Returns200WithIndirectRightsWithClientDelegationReason()
        {
            HttpClient client = CreateClient(ClientDelegationClient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={ClientDelegationClient}&from={ClientDelegationClient}&to={ClientDelegationAgent}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);

            foreach (var right in resourceRightsDto.IndirectRights)
            {
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.ClientDelegation), $"Expected ClientDelegation but got {right.Reason.Flag}.");
                PermissionDto permission = Assert.Single(right.Permissions);
                Assert.Equal(ClientDelegationClient, permission.From.Id);
                Assert.Equal(ClientDelegationAgent, permission.To.Id);
                Assert.Equal(RoleConstants.Accountant.Id, permission.Role.Id);
                Assert.Equal(ClientDelegationFacilitator, permission.Via.Id);
                Assert.Equal(RoleConstants.Agent.Id, permission.ViaRole.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.ClientDelegation), $"Expected ClientDelegation but got {permission.Reason.Flag}.");
            }

            // The rights come from the policy of the client side assignment resource, which grants read and
            // delete only. The delegation policies used for direct grants in this class also grant instantiate,
            // write and confirm, so resolving through the wrong assignment resource would widen the set.
            var actions = resourceRightsDto.IndirectRights.Select(r => r.Right.Action?.Value).ToList();
            Assert.Equal(2, resourceRightsDto.IndirectRights.Count);
            Assert.Contains("read", actions);
            Assert.DoesNotContain("instantiate", actions);
            Assert.DoesNotContain("write", actions);
            Assert.DoesNotContain("confirm", actions);
        }

        /// <summary>
        /// Sigrid holds Skattemelding both directly from Torvet Delikatesse and through the client delegation
        /// Bakken Regnskap made on the same client's behalf. Both holdings must be visible: the direct one under
        /// DirectRights, the client delegated one under IndirectRights, including for the read right they share.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_ForResourceHeldDirectlyAndByClientDelegation_Returns200WithBothHoldingsReported()
        {
            HttpClient client = CreateClient(MixedClient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={MixedClient}&from={MixedClient}&to={MixedAgent}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotEmpty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);

            List<string> directKeys = resourceRightsDto.DirectRights.Select(r => r.Right.Key).ToList();
            List<string> indirectKeys = resourceRightsDto.IndirectRights.Select(r => r.Right.Key).ToList();

            // The client delegation policy grants read and delete; the direct policy grants nine other-and-overlapping
            // rights. Read is the overlap, delete is held only through the client delegation.
            Assert.Equal(2, indirectKeys.Count);
            string sharedKey = Assert.Single(indirectKeys.Intersect(directKeys));
            string clientDelegationOnlyKey = Assert.Single(indirectKeys.Except(directKeys));
            Assert.NotEqual(sharedKey, clientDelegationOnlyKey);

            // The right held both ways is reported under both headings, each carrying only its own permission.
            RightPermission directShared = Assert.Single(resourceRightsDto.DirectRights, r => r.Right.Key == sharedKey);
            Assert.True(directShared.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {directShared.Reason.Flag}.");
            PermissionDto directPermission = Assert.Single(directShared.Permissions);
            Assert.Equal(RoleConstants.Rightholder.Id, directPermission.Role.Id);
            Assert.Null(directPermission.Via);
            Assert.True(directPermission.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {directPermission.Reason.Flag}.");

            RightPermission indirectShared = Assert.Single(resourceRightsDto.IndirectRights, r => r.Right.Key == sharedKey);
            Assert.True(indirectShared.Reason.Flag.Equals(AccessReasonFlag.ClientDelegation), $"Expected ClientDelegation but got {indirectShared.Reason.Flag}.");
            PermissionDto clientDelegatedPermission = Assert.Single(indirectShared.Permissions);
            Assert.Equal(MixedClient, clientDelegatedPermission.From.Id);
            Assert.Equal(MixedAgent, clientDelegatedPermission.To.Id);
            Assert.Equal(MixedFacilitator, clientDelegatedPermission.Via.Id);
            Assert.Equal(RoleConstants.Accountant.Id, clientDelegatedPermission.Role.Id);
            Assert.True(clientDelegatedPermission.Reason.Flag.Equals(AccessReasonFlag.ClientDelegation), $"Expected ClientDelegation but got {clientDelegatedPermission.Reason.Flag}.");

            // The right granted only by the client delegation policy stays out of the direct rights.
            RightPermission clientDelegationOnly = Assert.Single(resourceRightsDto.IndirectRights, r => r.Right.Key == clientDelegationOnlyKey);
            Assert.True(clientDelegationOnly.Reason.Flag.Equals(AccessReasonFlag.ClientDelegation), $"Expected ClientDelegation but got {clientDelegationOnly.Reason.Flag}.");
            Assert.DoesNotContain(resourceRightsDto.DirectRights, r => r.Right.Key == clientDelegationOnlyKey);
        }

        /// <summary>
        /// Malin uses from-others read scope for a to-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_ToOthersDirection_WithFromOthersScope_Returns403ForFromOthersScopeOnToOthersDirection()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Thea uses to-others read scope for a from-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_FromOthersDirection_WithToOthersScope_Returns403ForToOthersScopeOnFromOthersDirection()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses a write scope on the read-only GetResourceRights endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_WithWriteScope_Returns403ForWriteScope()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
