using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the RemoveResource (DELETE resources) endpoint
/// which removes all resource rights delegations for a given resource from one party to another. The tests cover
/// successful removal by Malin (MD of Dumbo Adventures) and Thea (MD of Mille Hundefrisør), scope enforcement,
/// and error scenarios.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveResource(Guid, Guid, Guid, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Dumbo Adventures -> Mille Hundefrisør (Rightholder)
    /// - Two dedicated client delegation chains on their own parties, one used for the revocation guard and one used
    ///   for the cascade check, each shaped client -> facilitator (Accountant) -> agent (Agent) with the Mattilsynet
    ///   bakery resource delegated to the agent
    /// </para>
    /// <para>
    /// Pre-seeded via <see cref="TestDataSeeds"/>:
    /// - Resource "Sykmelding til arbeidsgiver" (nav_sykepenger_sykmelding)
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for resource registry policy lookups
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policy evaluation
    /// - <see cref="PolicyFactoryMock"/> captures written XACML policies
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (can act as to-others delegator)
    /// - Thea: managing director of Mille Hundefrisør (can act as from-others receiver)
    /// </para>
    /// <para>
    /// The tests verify that Malin can remove resource rights on behalf of Dumbo Adventures for existing rightholders,
    /// that Thea can remove resource rights on behalf of Mille Hundefrisør from an existing connection,
    /// that correct scopes are enforced, and that invalid resources produce appropriate errors.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class RemoveResource : IClassFixture<ApiFixture>
    {
        private static readonly Guid GuardClient = Guid.Parse("0196b120-0000-7000-8000-000000000001");
        private static readonly Guid GuardFacilitator = Guid.Parse("0196b120-0000-7000-8000-000000000002");
        private static readonly Guid GuardAgent = Guid.Parse("0196b120-0000-7000-8000-000000000003");
        private static readonly Guid CascadeClient = Guid.Parse("0196b120-0000-7000-8000-000000000011");
        private static readonly Guid CascadeFacilitator = Guid.Parse("0196b120-0000-7000-8000-000000000012");
        private static readonly Guid CascadeAgent = Guid.Parse("0196b120-0000-7000-8000-000000000013");
        private static readonly Guid CascadeClientAssignment = Guid.Parse("0196b120-0000-7000-8000-000000000021");
        private static readonly Guid CascadeDelegationResource = Guid.Parse("0196b120-0000-7000-8000-000000000022");

        public RemoveResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
            });
            Fixture.EnsureSeedOnce<RemoveResource>(db =>
            {
                var rightholderFromDumboToMille = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.MilleHundefrisor.Id,
                    RoleId = RoleConstants.Rightholder
                };

                db.Assignments.Add(rightholderFromDumboToMille);
                db.SaveChanges();

                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = GuardClient,
                        Name = "Lysaker Blomster AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950031",
                        RefId = "399950031",
                        PartyId = 50950031,
                    },
                    new Entity()
                    {
                        Id = GuardFacilitator,
                        Name = "Storli Regnskap AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950032",
                        RefId = "399950032",
                        PartyId = 50950032,
                    },
                    new Entity()
                    {
                        Id = GuardAgent,
                        Name = "Tuva Storli",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "20019099934",
                        RefId = "20019099934",
                        PartyId = 50950033,
                        UserId = 50950033,
                        DateOfBirth = new DateOnly(1990, 1, 20),
                    },
                    new Entity()
                    {
                        Id = CascadeClient,
                        Name = "Rusten Sykkelverksted AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950041",
                        RefId = "399950041",
                        PartyId = 50950041,
                    },
                    new Entity()
                    {
                        Id = CascadeFacilitator,
                        Name = "Vollen Regnskap AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950042",
                        RefId = "399950042",
                        PartyId = 50950042,
                    },
                    new Entity()
                    {
                        Id = CascadeAgent,
                        Name = "Even Vollen",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "21019099935",
                        RefId = "21019099935",
                        PartyId = 50950043,
                        UserId = 50950043,
                        DateOfBirth = new DateOnly(1990, 1, 21),
                    });

                db.SaveChanges();

                SeedClientDelegation(db, GuardClient, GuardFacilitator, GuardAgent, clientAssignmentId: null, delegationResourceId: null, policyPath: "mattilsynet-baker-konditorvare/50950031/p50950033/delegationpolicy.xml");
                SeedClientDelegation(db, CascadeClient, CascadeFacilitator, CascadeAgent, clientAssignmentId: CascadeClientAssignment, delegationResourceId: CascadeDelegationResource, policyPath: "mattilsynet-baker-konditorvare/50950041/p50950043/delegationpolicy.xml");

                db.SaveChanges();
            });
        }

        private static void SeedClientDelegation(AppDbContext db, Guid clientId, Guid facilitatorId, Guid agentId, Guid? clientAssignmentId, Guid? delegationResourceId, string policyPath)
        {
            var accountantFromClientToFacilitator = new Assignment()
            {
                FromId = clientId,
                ToId = facilitatorId,
                RoleId = RoleConstants.Accountant,
            };

            if (clientAssignmentId.HasValue)
            {
                accountantFromClientToFacilitator.Id = clientAssignmentId.Value;
            }

            var agentFromFacilitatorToAgent = new Assignment()
            {
                FromId = facilitatorId,
                ToId = agentId,
                RoleId = RoleConstants.Agent,
            };

            var assignmentResourceForClient = new AssignmentResource()
            {
                AssignmentId = accountantFromClientToFacilitator.Id,
                ResourceId = TestData.MattilsynetBakeryService.Id,
                PolicyPath = policyPath,
                PolicyVersion = "1.0",
            };

            var delegation = new AccessMgmt.PersistenceEF.Models.Delegation()
            {
                FromId = accountantFromClientToFacilitator.Id,
                ToId = agentFromFacilitatorToAgent.Id,
                FacilitatorId = facilitatorId,
            };

            var delegationResource = new DelegationResource()
            {
                DelegationId = delegation.Id,
                ResourceId = TestData.MattilsynetBakeryService.Id,
                AssignmentResourceId = assignmentResourceForClient.Id,
            };

            if (delegationResourceId.HasValue)
            {
                delegationResource.Id = delegationResourceId.Value;
            }

            db.Assignments.Add(accountantFromClientToFacilitator);
            db.Assignments.Add(agentFromFacilitatorToAgent);
            db.AssignmentResources.Add(assignmentResourceForClient);
            db.Delegations.Add(delegation);
            db.DelegationResources.Add(delegationResource);
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
        /// Helper to get valid right keys via the delegation check endpoint.
        /// Malin (MD of Dumbo) performs a delegation check for the resource to discover delegatable right keys.
        /// </summary>
        private async Task<List<string>> GetDelegatableRightKeys(string resource)
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={resource}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result.Rights
                .Where(r => r.Result)
                .Select(r => r.Right.Key)
                .ToList();
        }

        /// <summary>
        /// Helper to add resource rights delegation before testing removal.
        /// </summary>
        private async Task AddResourceRights(string resource, List<string> rightKeys)
        {
            var body = new RightKeyListDto { DirectRightKeys = rightKeys };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource={resource}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        /// <summary>
        /// Malin (MD of Dumbo Adventures) removes all resource rights for nav_sykepenger_sykmelding delegated to Mille Hundefrisør.
        /// First adds the delegation, then removes it. Expects 204 NoContent.
        /// </summary>
        [Fact]
        public async Task RemoveResource_AsManagingDirectorToOrganization_Returns204NoContent()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("nav_sykepenger_sykmelding");
            Assert.NotEmpty(rightKeys);

            await AddResourceRights("nav_sykepenger_sykmelding", rightKeys);

            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Thea (MD of Mille Hundefrisør) removes all resource rights for nav_sykepenger_sykmelding from the Dumbo -> Mille connection,
        /// acting as receiver (from-others direction). Expects 204 NoContent.
        /// </summary>
        [Fact]
        public async Task RemoveResource_AsManagingDirectorFromOtherOrganization_Returns204NoContent()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("nav_sykepenger_sykmelding");
            Assert.NotEmpty(rightKeys);

            await AddResourceRights("nav_sykepenger_sykmelding", rightKeys);

            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Lysaker Blomster tries to revoke a resource its agent Tuva holds only through a client delegation.
        /// The endpoint does not revoke client delegated access, so the attempt is rejected.
        /// </summary>
        [Fact]
        public async Task RemoveResource_ForClientDelegatedResourceOnly_Returns400WithClientDelegationNotRevocableError()
        {
            HttpClient client = CreateClient(GuardClient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={GuardClient}&from={GuardClient}&to={GuardAgent}&resource=app_mat_mattilsynet-baker-konditorvare",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {responseContent}");

            AltinnProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnProblemDetails>(responseContent, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(problemDetails);
            Assert.True(problemDetails.Extensions.TryGetValue("validationErrors", out object errorsValue), $"Expected validationErrors in the problem details. Response body: {responseContent}");
            JsonElement errors = Assert.IsType<JsonElement>(errorsValue);

            Assert.Equal("AM.VLD-00035", errors[0].GetProperty("code").GetString());

            await Fixture.QueryDb(async db =>
            {
                bool delegationResourceStillExists = await db.DelegationResources
                    .AnyAsync(dr => dr.Delegation.From.FromId == GuardClient && dr.Delegation.To.ToId == GuardAgent, TestContext.Current.CancellationToken);

                Assert.True(delegationResourceStillExists);
            });
        }

        /// <summary>
        /// Removing the client-side assignment that carries the delegated resource cascades all the way down:
        /// the delegation and its delegation resource go with it.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WhenClientAssignmentIsDeleted_RemovesDelegationResourceByCascade()
        {
            await Fixture.QueryDb(async db =>
            {
                Assignment clientAssignment = await db.Assignments.FirstAsync(a => a.Id == CascadeClientAssignment, TestContext.Current.CancellationToken);
                db.Assignments.Remove(clientAssignment);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            await Fixture.QueryDb(async db =>
            {
                bool delegationResourceExists = await db.DelegationResources
                    .AnyAsync(dr => dr.Id == CascadeDelegationResource, TestContext.Current.CancellationToken);

                bool delegationExists = await db.Delegations
                    .AnyAsync(d => d.FacilitatorId == CascadeFacilitator, TestContext.Current.CancellationToken);

                Assert.False(delegationResourceExists);
                Assert.False(delegationExists);
            });
        }

        /// <summary>
        /// Malin (MD of Dumbo) tries to remove resource rights for a resource that does not exist in the database.
        /// Expects 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WithInvalidResource_Returns400ForInvalidResource()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nonexistent_resource",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires bidirectional write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WithFromOthersReadScope_Returns403ForFromOthersReadScope()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on an endpoint that requires bidirectional write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WithToOthersReadScope_Returns403ForToOthersReadScope()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
