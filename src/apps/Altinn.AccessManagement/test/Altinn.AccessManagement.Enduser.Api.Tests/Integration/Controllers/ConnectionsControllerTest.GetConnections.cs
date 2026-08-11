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

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetConnections(AccessManagement.Api.Enduser.Models.ConnectionInput, AccessManagement.Api.Enduser.Models.PagingInput, bool, bool, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Nordis AS -> Verdiq AS: Rightholder and Accountant assignments
    /// - Verdiq AS -> Paula and Ørjan: Agent assignments
    /// - Nordis AS -> Paula: Agent assignment
    /// - AssignmentPackage linking Nordis->Verdiq Rightholder to DocumentBasedSupervision
    /// - A dedicated client delegation chain on its own parties: Bekkelia Regnskapsklient AS (client) -> Fjordheim
    ///   Regnskap AS (facilitator, Accountant) -> Kasper Bekk (agent, Agent), with the Mattilsynet bakery resource
    ///   delegated to the agent through an AssignmentResource on the client-side assignment
    /// </para>
    /// <para>
    /// The tests verify bidirectional scope enforcement: from-others read scope is required when
    /// the party appears as the "to" parameter (receiving), and to-others read scope is required
    /// when the party appears as the "from" parameter (giving). Mismatched scopes or party values
    /// that don't match from/to result in 403 Forbidden.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    [Collection(ConnectionsReadOnlyCollection.Name)]
    public class GetConnections
    {
        private static readonly Guid ClientDelegationClient = Guid.Parse("0196b100-0000-7000-8000-000000000001");
        private static readonly Guid ClientDelegationFacilitator = Guid.Parse("0196b100-0000-7000-8000-000000000002");
        private static readonly Guid ClientDelegationAgent = Guid.Parse("0196b100-0000-7000-8000-000000000003");

        public GetConnections(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetConnections>(db =>
            {
                var rightholderFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };

                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };
                var agentFromVerdiqToOrjan = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonOrjan,
                    RoleId = RoleConstants.Agent,
                };
                var agentFromNordisToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                var assignmentPackageFromNordisToVerdiq = new AssignmentPackage()
                {
                    AssignmentId = rightholderFromNordisToVerdiq.Id,
                    PackageId = PackageConstants.DocumentBasedSupervision,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);
                db.Assignments.Add(agentFromNordisToPaula);

                db.SaveChanges();

                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = ClientDelegationClient,
                        Name = "Bekkelia Regnskapsklient AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950001",
                        RefId = "399950001",
                        PartyId = 50950001,
                    },
                    new Entity()
                    {
                        Id = ClientDelegationFacilitator,
                        Name = "Fjordheim Regnskap AS",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.AS,
                        OrganizationIdentifier = "399950002",
                        RefId = "399950002",
                        PartyId = 50950002,
                    },
                    new Entity()
                    {
                        Id = ClientDelegationAgent,
                        Name = "Kasper Bekk",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "17019099931",
                        RefId = "17019099931",
                        PartyId = 50950003,
                        UserId = 50950003,
                        DateOfBirth = new DateOnly(1990, 1, 17),
                    });

                db.SaveChanges();

                var accountantFromClientToFacilitator = new Assignment()
                {
                    FromId = ClientDelegationClient,
                    ToId = ClientDelegationFacilitator,
                    RoleId = RoleConstants.Accountant,
                };

                var agentFromFacilitatorToKasper = new Assignment()
                {
                    FromId = ClientDelegationFacilitator,
                    ToId = ClientDelegationAgent,
                    RoleId = RoleConstants.Agent,
                };

                var assignmentResourceForClient = new AssignmentResource()
                {
                    AssignmentId = accountantFromClientToFacilitator.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/50950001/p50950003/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var delegationToKasper = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromClientToFacilitator.Id,
                    ToId = agentFromFacilitatorToKasper.Id,
                    FacilitatorId = ClientDelegationFacilitator,
                };

                db.Assignments.Add(accountantFromClientToFacilitator);
                db.Assignments.Add(agentFromFacilitatorToKasper);
                db.AssignmentResources.Add(assignmentResourceForClient);
                db.Delegations.Add(delegationToKasper);
                db.DelegationResources.Add(new DelegationResource()
                {
                    DelegationId = delegationToKasper.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceForClient.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient(params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
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
        /// Verdiq queries connections where it is the receiver (to=Verdiq) using from-others read scope.
        /// Expects OK.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionFromOthersUsingFromOthersScope_Returns200Ok()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ, "some:other/scope.read");

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&to={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verdiq queries connections where it is the receiver (to=Verdiq) using the wrong to-others read scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionFromOthersUsingToOthersScope_Returns403ForToOthersScopeOnFromOthersDirection()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&to={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Verdiq queries connections where it is the giver (from=Verdiq) using to-others read scope.
        /// Expects OK.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingToOthersScope_Returns200Ok()
        {
            var client = CreateClient("some:other/scope.read", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verdiq queries connections where it is the giver (from=Verdiq) using the wrong from-others read scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingFromOthersScope_Returns403ForFromOthersScopeOnToOthersDirection()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Party (Verdiq) does not match either from (Paula) or to (Ørjan), so the request is rejected.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingToOtherScopeWithPartyNotEqualFromOrTo_Returns403ForPartyNotMatchingFromOrTo()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.PersonPaula.Id}&to={TestEntities.PersonOrjan.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Verdiq queries connections where from=Verdiq and to=SystemUserStandard using to-others read scope.
        /// SystemUser is an allowed entity type for the "to" parameter. Expects 200 OK.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingToOtherScopeWithSystemUserAsTo_Returns200Ok()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationVerdiqAS}&to={TestEntities.SystemUserStandard.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Kasper, agent of the facilitator Fjordheim Regnskap, lists his incoming connections with client delegations included.
        /// The client Bekkelia Regnskapsklient is reached only through the client delegation, so it must be present.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithClientDelegationsIncluded_Returns200WithClientDelegatedConnection()
        {
            var client = CreateClient(ClientDelegationAgent, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={ClientDelegationAgent}&to={ClientDelegationAgent}&includeClientDelegations=true", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {data}");

            var result = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Contains(result.Items, c => c.Party.Id == ClientDelegationClient);
        }

        /// <summary>
        /// Same query with resources requested: the resource delegated through the client delegation is included on the connection.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithClientDelegationsAndResourcesIncluded_Returns200WithClientDelegatedResource()
        {
            var client = CreateClient(ClientDelegationAgent, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={ClientDelegationAgent}&to={ClientDelegationAgent}&includeClientDelegations=true&includeResources=true", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {data}");

            var result = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var connection = Assert.Single(result.Items, c => c.Party.Id == ClientDelegationClient);
            Assert.Contains(connection.Resources, r => r.Id == TestData.MattilsynetBakeryService.Id);
        }

        /// <summary>
        /// Without resources requested the client delegated connection is still returned, but carries no resources.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithClientDelegationsIncludedWithoutResources_Returns200WithConnectionWithoutResources()
        {
            var client = CreateClient(ClientDelegationAgent, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={ClientDelegationAgent}&to={ClientDelegationAgent}&includeClientDelegations=true&includeResources=false", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {data}");

            var result = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var connection = Assert.Single(result.Items, c => c.Party.Id == ClientDelegationClient);
            Assert.Empty(connection.Resources);
        }

        /// <summary>
        /// With client delegations excluded the client is not reachable for the agent at all.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithClientDelegationsExcluded_Returns200WithoutClientDelegatedConnection()
        {
            var client = CreateClient(ClientDelegationAgent, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={ClientDelegationAgent}&to={ClientDelegationAgent}&includeClientDelegations=false&includeResources=true", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {data}");

            var result = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.DoesNotContain(result.Items, c => c.Party.Id == ClientDelegationClient);
        }
    }
}
