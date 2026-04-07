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

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetConnections(AccessManagement.Api.Enduser.Models.ConnectionInput, AccessManagement.Api.Enduser.Models.PagingInput, bool, bool, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Nordis AS → Verdiq AS: Rightholder and Accountant assignments
    /// - Verdiq AS → Paula and Ørjan: Agent assignments
    /// - Nordis AS → Paula: Agent assignment
    /// - AssignmentPackage linking Nordis→Verdiq Rightholder to DocumentBasedSupervision
    /// </para>
    /// <para>
    /// The tests verify bidirectional scope enforcement: from-others read scope is required when
    /// the party appears as the "to" parameter (receiving), and to-others read scope is required
    /// when the party appears as the "from" parameter (giving). Mismatched scopes or party values
    /// that don't match from/to result in 403 Forbidden.
    /// </para>
    /// </remarks>
    public class GetConnections : IClassFixture<ApiFixture>
    {
        public GetConnections(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.EnsureSeedOnce(db =>
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
        
        /// <summary>
        /// Verdiq queries connections where it is the receiver (to=Verdiq) using from-others read scope.
        /// Expects OK.
        /// </summary>
        [Fact]
        public async Task ListConnections_WithDirectionFromOthersUsingFromOthersScope_ReturnsOk()
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
        public async Task ListConnections_WithDirectionFromOthersUsingToOthersScope_ReturnsForbidden()
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
        public async Task ListConnections_WithDirectionToOthersUsingToOthersScope_ReturnsOk()
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
        public async Task ListConnections_WithDirectionToOthersUsingFromOthersScope_ReturnsForbidden()
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
        public async Task ListConnections_WithDirectionToOthersUsingToOtherScopeWithPartyNotEqualFromOrTo_ReturnsForbidden()
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
        public async Task ListConnections_WithDirectionToOthersUsingToOtherScopeWithSystemUserAsTo_ReturnsOK()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationVerdiqAS}&to={TestEntities.SystemUserStandard.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
