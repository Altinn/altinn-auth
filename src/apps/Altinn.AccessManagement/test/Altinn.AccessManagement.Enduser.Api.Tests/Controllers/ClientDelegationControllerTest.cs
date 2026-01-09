using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public class ClientDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/clientdelegations";

    public class FeatureFlagClientDelegationEnabledCollection : IClassFixture<ApiFixture>
    {
        public FeatureFlagClientDelegationEnabledCollection(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
        }

        [Fact]
        public async Task ListClient_WithDisabledFeatureFlag_ReturnsNotFound()
        {
            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var result = await client.GetAsync($"{Route}/clients?party={Guid.NewGuid()}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        public ApiFixture Fixture { get; }
    }

    public class GetClientDelegations : IClassFixture<ApiFixture>
    {
        public GetClientDelegations(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
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
                var agentFromPaulaToNordis = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromPaulaToNordis);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        #region GET accessmanagement/api/v1/enduser/clientdelegations/clients

        [Fact]
        public async Task ListClient_ForOrganization_MissingQueryParamPartyBadRequest()
        {
            var client = CreateClient();

            var result = await client.GetAsync($"{Route}/clients", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithCCR_ReturnsOk()
        {
            var rolepkgs = new List<RolePackage>();
            await Fixture.QueryDb(async db =>
            {
                rolepkgs = await db.RolePackages
                    .Where(rp => rp.RoleId == RoleConstants.Accountant.Id)
                    .ToListAsync();

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(data);

            var connection = result.Items.SelectMany(p => p.Access).FirstOrDefault(a => a.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(connection);
            Assert.Equal(RoleConstants.Accountant.Id, connection.Role.Id);
            foreach (var rolePkg in rolepkgs)
            {
                Assert.Contains(rolePkg.PackageId, connection.Packages.Select(p => p.Id));
            }
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithRightholderAssignment_ReturnsOk()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.NotNull(connection);
            Assert.Equal(connection.Client.Id, TestEntities.OrganizationNordisAS.Id);

            var access = connection.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Rightholder);
            Assert.NotNull(access);
            Assert.Equal(RoleConstants.Rightholder.Id, access.Role.Id);
            Assert.Equal(PackageConstants.Customs.Id, access.Packages.First().Id);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, access.Packages.First().Urn);
            Assert.Equal(PackageConstants.Customs.Entity.AreaId, access.Packages.First().AreaId);
        }

        #endregion

        #region GET accessmanagement/api/v1/enduser/clientdelegations/agents

        [Fact]
        public async Task ListAgents_ForOrganization_MissingQueryParamPartyBadRequest()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ListAgent_ForPersonWithAgentAssignment_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(data);
            
            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.NotNull(connection);
            Assert.Equal(TestEntities.PersonPaula.Id, connection.Agent.Id);

            var access = connection.Access.FirstOrDefault(r => r.Role.Id == RoleConstants.Agent);
            Assert.NotNull(access);
            Assert.Empty(access.Packages);
        }

        [Fact]
        public async Task ListAgent_ForPersonWithAgentAssignmentToAnotherOrganization_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(data);
            
            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.Null(connection);
        }

        [Fact]
        public async Task ListAgent_ForNoAgentAssignment_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonOrjan);
            Assert.Null(connection);
        }

        #endregion
    }
}
