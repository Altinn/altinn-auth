using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers.V2;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using ContractsV2 = Altinn.Authorization.Api.Contracts.AccessManagement.V2;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers.V2;

public class ClientDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v2/enduser/clientdelegations";

    public static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region GET accessmanagement/api/v2/enduser/clientdelegations/my/clients

    /// <summary>
    /// <see cref="ClientDelegationController.GetMyClients(List{Guid}?, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetMyClients : IClassFixture<ApiFixture>
    {
        public GetMyClients(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetMyClients>(db =>
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

                var delegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationToOrjan = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToOrjan.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var rppaula = db.RolePackages.FirstOrDefault(r => r.RoleId == RoleConstants.Accountant && r.PackageId == PackageConstants.AccountantWithSigningRights);
                var rporjan = db.RolePackages.FirstOrDefault(r => r.RoleId == RoleConstants.Accountant && r.PackageId == PackageConstants.AccountantWithoutSigningRights);
                var delegationPackageAccountantWithSigningRightsToPaula = new DelegationPackage()
                {
                    DelegationId = delegationToPaula.Id,
                    RolePackageId = rppaula.Id,
                    PackageId = PackageConstants.AccountantWithSigningRights,
                };
                var delegationPackageAccountantSalaryToPaula = new DelegationPackage()
                {
                    DelegationId = delegationToPaula.Id,
                    RolePackageId = rppaula.Id,
                    PackageId = PackageConstants.AccountantSalary,
                };

                var delegationPackageAccountantWithSigningRightsToOrjan = new DelegationPackage()
                {
                    DelegationId = delegationToOrjan.Id,
                    RolePackageId = rporjan.Id,
                    PackageId = PackageConstants.AccountantWithoutSigningRights,
                };

                var assignmentResourceAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var delegationResourceToPaula = new DelegationResource()
                {
                    DelegationId = delegationToPaula.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceAccountant.Id,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);
                db.Assignments.Add(agentFromNordisToPaula);

                db.Delegations.Add(delegationToPaula);
                db.Delegations.Add(delegationToOrjan);

                db.DelegationPackages.Add(delegationPackageAccountantWithSigningRightsToPaula);
                db.DelegationPackages.Add(delegationPackageAccountantWithSigningRightsToOrjan);
                db.DelegationPackages.Add(delegationPackageAccountantSalaryToPaula);
                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderFromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                db.AssignmentResources.Add(assignmentResourceAccountant);
                db.DelegationResources.Add(delegationResourceToPaula);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task ListMyClients_WithFilter_Returns200WithFilteredClients()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/my/clients?provider={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.MyClientDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(result.Items);
            Assert.NotEmpty(result.Items.FirstOrDefault().Clients);

            response = await client.GetAsync($"{Route}/my/clients?provider={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.MyClientDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(result.Items);
            Assert.Empty(result.Items.FirstOrDefault().Clients);
        }

        [Fact]
        public async Task ListMyClients_WithProviderFilter_Returns200IncludingDelegationResources()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/my/clients?provider={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.MyClientDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(result.Items);

            var verdiq = result.Items.FirstOrDefault(p => p?.Provider?.Id == TestEntities.OrganizationVerdiqAS);
            Assert.NotNull(verdiq);

            var verdiqClient = verdiq.Clients.FirstOrDefault();
            Assert.NotNull(verdiqClient);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, verdiqClient.Client.Id);

            var access = verdiqClient.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(access);

            Assert.Single(access.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, access.Resources.FirstOrDefault()?.Id);
        }

        [Fact]
        public async Task ListMyClients_WithProviderFilterForProviderWithoutDelegationResources_Returns200WithEmptyResources()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/my/clients?provider={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.MyClientDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(result.Items);

            var nordis = result.Items.FirstOrDefault(p => p?.Provider?.Id == TestEntities.OrganizationNordisAS);
            Assert.NotNull(nordis);
            Assert.Empty(nordis.Clients);
        }

        [Fact]
        public async Task ListMyClients_WithoutFilter_Returns200WithAllProvidersAndClients()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/my/clients", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.MyClientDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(2, result.Items.Count());
            var verdiq = result.Items.FirstOrDefault(p => p?.Provider?.Id == TestEntities.OrganizationVerdiqAS);
            var nordis = result.Items.FirstOrDefault(p => p?.Provider?.Id == TestEntities.OrganizationNordisAS);
            Assert.Empty(nordis.Clients);

            Assert.Single(verdiq.Clients);
            var verdiqClients = verdiq.Clients.FirstOrDefault();

            Assert.Single(verdiqClients.Access);
            var verdiqAccess = verdiqClients.Access.FirstOrDefault();

            Assert.Equal(2, verdiqAccess.Packages.Count);
            var verdiqPackageAccountantWithSigningRights = verdiqAccess.Packages.FirstOrDefault(p => p.Id == PackageConstants.AccountantWithSigningRights);
            var verdiqPackageAccountantSalary = verdiqAccess.Packages.FirstOrDefault(p => p.Id == PackageConstants.AccountantSalary);

            Assert.NotNull(verdiqPackageAccountantWithSigningRights);
            Assert.NotNull(verdiqPackageAccountantSalary);

            Assert.Equal(RoleConstants.Accountant.Id, verdiqAccess.Role.Id);
            Assert.Equal(PackageConstants.AccountantWithSigningRights, verdiqPackageAccountantWithSigningRights.Id);
            Assert.Equal(PackageConstants.AccountantSalary, verdiqPackageAccountantSalary.Id);

            Assert.Equal(TestEntities.OrganizationVerdiqAS.Id, verdiq.Provider.Id);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, verdiqClients.Client.Id);

            Assert.Single(verdiqAccess.Resources);
            var verdiqResource = verdiqAccess.Resources.FirstOrDefault();
            Assert.NotNull(verdiqResource);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, verdiqResource.Id);
        }
    }
    #endregion

    #region GET accessmanagement/api/v2/enduser/clientdelegations/my/clientproviders

    /// <summary>
    /// <see cref="ClientDelegationController.GetMyClientProviders(CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetMyClientProviders : IClassFixture<ApiFixture>
    {
        public GetMyClientProviders(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetMyClientProviders>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                });

                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
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
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task ListMyClientProviders_ForPersonWithAgentAssignments_Returns200WithProviders()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/my/clientproviders", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.MyClientProviderDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, result.Items.Count());

            var verdiq = result.Items.FirstOrDefault(p => p.Provider.Id == TestEntities.OrganizationVerdiqAS);
            Assert.NotNull(verdiq);
            Assert.NotNull(verdiq.Provider);

            var access = Assert.Single(verdiq.Access);
            Assert.Empty(access.Packages);
            Assert.Empty(access.Resources);
        }
    }
    #endregion

    #region DELETE accessmanagement/api/v2/enduser/clientdelegations/my/clientproviders

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteMyAgentViaParty(Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DeleteMyClientProvider : IClassFixture<ApiFixture>
    {
        public DeleteMyClientProvider(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DeleteMyClientProvider>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
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
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DeleteMyClientProvider_ForProviderWithoutDelegations_Returns204AndRemovesAgentAssignment()
        {
            var client = CreateClient();

            var response = await client.DeleteAsync($"{Route}/my/clientproviders?provider={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await Fixture.QueryDb(static async db =>
            {
                var assignment = await db.Assignments
                    .Where(a => a.FromId == TestEntities.OrganizationNordisAS.Id && a.ToId == TestEntities.PersonPaula.Id && a.RoleId == RoleConstants.Agent.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.Null(assignment);
            });
        }
    }
    #endregion

    #region GET accessmanagement/api/v2/enduser/clientdelegations/clients

    /// <summary>
    /// <see cref="ClientDelegationController.GetClients(Guid, List{string}?, List{string}?, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetClients : IClassFixture<ApiFixture>
    {
        public GetClients(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetClients>(db =>
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

                var assignmentResourceForAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromPaulaToNordis);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                db.AssignmentResources.Add(assignmentResourceForAccountant);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithRightholderAssignment_Returns200WithRightholderClientAndCustomsPackage()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.NotNull(connection);
            Assert.Equal(connection.Client.Id, TestEntities.OrganizationNordisAS.Id);

            var access = connection.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Rightholder);
            Assert.NotNull(access);
            Assert.Equal(RoleConstants.Rightholder.Id, access.Role.Id);

            var customsPackage = access.Packages.FirstOrDefault(p => p.Id == PackageConstants.Customs.Id);
            Assert.NotNull(customsPackage);
            Assert.Equal(PackageConstants.Customs.Id, customsPackage.Id);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, customsPackage.Urn);
            Assert.Equal(PackageConstants.Customs.Entity.AreaId, customsPackage.AreaId);
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithAccountantAssignmentResource_Returns200WithResourceInAccess()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            var nordisClient = result.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.NotNull(nordisClient);

            var accountantAccess = nordisClient.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(accountantAccess);

            Assert.Single(accountantAccess.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, accountantAccess.Resources.FirstOrDefault()?.Id);
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithRightholderAssignment_Returns200WithEmptyResourcesOnRightholder()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&roles=rettighetshaver", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            var nordisClient = result.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.NotNull(nordisClient);

            var rightholderAccess = nordisClient.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Rightholder);
            Assert.NotNull(rightholderAccess);

            Assert.Empty(rightholderAccess.Resources);
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithAccountantRoleFilter_Returns200WithResourceInFilteredAccess()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&roles=regnskapsforer", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            var nordisClient = result.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.NotNull(nordisClient);

            Assert.DoesNotContain(nordisClient.Access, a => a.Role.Id == RoleConstants.Rightholder);

            var accountantAccess = nordisClient.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(accountantAccess);

            Assert.Single(accountantAccess.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, accountantAccess.Resources.FirstOrDefault()?.Id);
        }
    }
    #endregion

    #region GET accessmanagement/api/v2/enduser/clientdelegations/agents

    /// <summary>
    /// <see cref="ClientDelegationController.GetAgents(Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetAgents : IClassFixture<ApiFixture>
    {
        public GetAgents(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetAgents>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
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
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task ListAgent_ForPersonWithAgentAssignment_Returns200WithAgentConnection()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.NotNull(connection);
            Assert.Equal(TestEntities.PersonPaula.Id, connection.Agent.Id);

            var access = connection.Access.FirstOrDefault(r => r.Role.Id == RoleConstants.Agent);
            Assert.NotNull(access);
            Assert.Empty(access.Packages);
            Assert.Empty(access.Resources);
        }
    }
    #endregion

    #region POST accessmanagement/api/v2/enduser/clientdelegations/agents

    /// <summary>
    /// <see cref="ClientDelegationController.AddAgent(Guid, Guid?, AccessManagement.Api.Enduser.Models.PersonInput?, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class AddAgent : IClassFixture<ApiFixture>
    {
        public AddAgent(ApiFixture fixture)
        {
            Fixture = fixture;
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ}"));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task AddAgent_NotPermittedEntityType_Returns400WhenEntityTypeDisallowed()
        {
            // Try to add organization as agent
            var client = CreateClient();
            var response = await client.PostAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.OrganizationNordisAS}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);

            // Ensure proper error returned
            Assert.Single(problem.Errors);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.DisallowedEntityType.ErrorCode, error.ErrorCode);
                Assert.Contains("$QUERY/agent", error.Paths);
            });
        }

        [Fact]
        public async Task AddAgent_WithUnknownAgent_Returns400WithAgentQueryPointer()
        {
            var client = CreateClient();
            var response = await client.PostAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&agent={Guid.NewGuid()}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);

            Assert.Single(problem.Errors);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.EntityNotExists.ErrorCode, error.ErrorCode);
                Assert.Contains("QUERY/agent", error.Paths);
                Assert.DoesNotContain("QUERY/to", error.Paths);
            });
        }

        [Fact]
        public async Task AddAgent_PermittedEntityTypeAgentSystemUser_Returns200AndAddsAgentSystemUser()
        {
            var client = CreateClient();
            var response = await client.PostAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.SystemUserClient}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var getAgents = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getAgents.StatusCode);
            var getAgentsData = await getAgents.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentDto>>(getAgentsData);

            var agent = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.SystemUserClient);
            Assert.NotNull(agent);
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v2/enduser/clientdelegations/agents

    /// <summary>
    /// <see cref="ClientDelegationController.RemoveAgent(Guid, Guid, bool, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DeleteAgent : IClassFixture<ApiFixture>
    {
        public DeleteAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DeleteAgent>(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
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
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task RemoveAgentWithExistingDelegations_WithCascadeFalse_Returns400WhenDelegationHasActiveConnections()
        {
            // Create Delegation
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify Delegation exists
            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientPackagesDto>>(delegationsToAgentPayload);

            Assert.NotEmpty(delegationToAgentResult.Items);

            // Delete Delegation without cascade
            var deleteResponse = await client.DeleteAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.PersonPaula}&cascade=false", TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

            // Ensure proper error returned
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(deleteResponsePayload, SerializerOptions);
            Assert.Single(problem.Errors);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.DelegationHasActiveConnections.ErrorCode, error.ErrorCode);
            });

            // Delete with cascade true
            deleteResponse = await client.DeleteAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.PersonPaula}&cascade=true", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Ensure Agent is deleted
            var getAgents = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}", TestContext.Current.CancellationToken);
            var getAgentsPayload = await getAgents.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var agentResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentDto>>(getAgentsPayload);

            Assert.Equal(HttpStatusCode.OK, getAgents.StatusCode);
            Assert.Empty(agentResult.Items);
        }
    }
    #endregion

    #region DELETE accessmanagement/api/v2/enduser/clientdelegations/agents/clients

    /// <summary>
    /// <see cref="ClientDelegationController.RemoveAgentsClient(Guid, Guid, Guid, bool, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class RemoveAnAgentsClient : IClassFixture<ApiFixture>
    {
        public RemoveAnAgentsClient(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RemoveAnAgentsClient>(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
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

                var assignmentPackageCustomsFromNordisToVerdiq = new AssignmentPackage()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
                };

                var assignmentPackageExplicitServiceDelegationFromNordisToVerdiq = new AssignmentPackage()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.ExplicitServiceDelegation.Id,
                };

                var delegationFromNordisToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = rightholderfromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationFromNordisToOrjan = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = rightholderfromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToOrjan.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationPackageFromNordisToPaula = new DelegationPackage()
                {
                    AssignmentPackageId = assignmentPackageCustomsFromNordisToVerdiq.Id,
                    DelegationId = delegationFromNordisToPaula.Id,
                    PackageId = PackageConstants.Customs.Id,
                };

                var delegationPackageFromNordisToOrjan = new DelegationPackage()
                {
                    AssignmentPackageId = assignmentPackageExplicitServiceDelegationFromNordisToVerdiq.Id,
                    DelegationId = delegationFromNordisToOrjan.Id,
                    PackageId = PackageConstants.ExplicitServiceDelegation.Id,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);

                db.AssignmentPackages.Add(assignmentPackageCustomsFromNordisToVerdiq);
                db.AssignmentPackages.Add(assignmentPackageExplicitServiceDelegationFromNordisToVerdiq);

                db.Delegations.Add(delegationFromNordisToPaula);
                db.Delegations.Add(delegationFromNordisToOrjan);

                db.DelegationPackages.Add(delegationPackageFromNordisToPaula);
                db.DelegationPackages.Add(delegationPackageFromNordisToOrjan);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task RemoveAgentsClientWithExistingDelegations_WithCascadeFalse_Returns400WhenDelegationHasActiveConnections()
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"{Route}/agents/clients?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Ensure proper error returned
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);
            Assert.Single(problem.Errors);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.DelegationHasActiveConnections.ErrorCode, error.ErrorCode);
            });
        }

        [Fact]
        public async Task RemoveAgentsClientWithExistingDelegations_WithCascadeTrue_Returns204AndRemovesDelegation()
        {
            var client = CreateClient();

            // Ensure delegation exists
            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Where(a => a.To.ToId == TestEntities.PersonOrjan)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(delegation);
            });

            var response = await client.DeleteAsync($"{Route}/agents/clients?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonOrjan}&cascade=true", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Ensure delegation has been deleted.
            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Where(a => a.To.ToId == TestEntities.PersonOrjan)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.Null(delegation);
            });
        }
    }
    #endregion

    #region POST accessmanagement/api/v2/enduser/clientdelegations/agents/accesspackages

    /// <summary>
    /// <see cref="ClientDelegationController.DelegateAccessPackageToAgent(Guid, Guid, Guid, DelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DelegateAccessPackageToAgent : IClassFixture<ApiFixture>
    {
        public DelegateAccessPackageToAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DelegateAccessPackageToAgent>(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
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
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithValidInput_Returns200WithDelegationResult()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var delegationResult = JsonSerializer.Deserialize<List<DelegationDto>>(data);
            Assert.NotEmpty(delegationResult);
            Assert.True(delegationResult.All(d => d.Changed));

            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientPackagesDto>>(delegationsToAgentPayload);

            Assert.NotEmpty(delegationToAgentResult.Items);

            var agentAccess = delegationToAgentResult.Items.FirstOrDefault();
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, agentAccess.Client.Id);
            Assert.Equal(RoleConstants.Rightholder.Entity.Code, agentAccess.Access.FirstOrDefault()?.Role?.Code);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, agentAccess.Access.FirstOrDefault()?.Packages?.FirstOrDefault().Urn);

            var getDelegationFromClient = await client.GetAsync($"{Route}/clients/accesspackages?party={TestEntities.OrganizationVerdiqAS.Id}&client={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);
            var delegationsFromClientPayload = await getDelegationFromClient.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationsFromClientResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentPackagesDto>>(delegationsFromClientPayload);

            Assert.NotEmpty(delegationsFromClientResult.Items);

            var accessToClient = delegationsFromClientResult.Items.FirstOrDefault();
            Assert.Equal(TestEntities.PersonPaula.Id, accessToClient.Agent.Id);
            Assert.Equal(RoleConstants.Rightholder.Entity.Code, accessToClient.Access.FirstOrDefault()?.Role?.Code);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, accessToClient.Access.FirstOrDefault()?.Packages?.FirstOrDefault().Urn);
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithUnknownClientAndAgent_Returns400WithClientAndAgentQueryPointers()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&client={Guid.NewGuid()}&agent={Guid.NewGuid()}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);

            Assert.Equal(2, problem.Errors.Count);
            Assert.Single(problem.Errors, e => e.Paths.Contains("$QUERY/client"));
            Assert.Single(problem.Errors, e => e.Paths.Contains("$QUERY/agent"));
            Assert.DoesNotContain(problem.Errors, e => e.Paths.Contains("$QUERY/from") || e.Paths.Contains("$QUERY/to"));
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithPersonWithoutAgentAssignment_Returns400WithAgentQueryPointer()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonOrjan}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);

            Assert.Single(problem.Errors);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.MissingAssignment.ErrorCode, error.ErrorCode);
                Assert.Contains("$QUERY/agent", error.Paths);
            });
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v2/enduser/clientdelegations/agents/accesspackages

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteAgentAccessPackage(Guid, Guid, Guid, DelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DeleteAgentAccessPackage : IClassFixture<ApiFixture>
    {
        public DeleteAgentAccessPackage(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DeleteAgentAccessPackage>(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
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
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DeleteAccessPackageToAgent_WithValidInput_Returns200WithDeleteResult()
        {
            // Create Delegation
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Delete Delegation
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}")
            {
                Content = JsonContent.Create(new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                })
            };

            var deleteResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            var deleteResult = JsonSerializer.Deserialize<List<DelegationDto>>(deleteResponsePayload);
            Assert.NotEmpty(deleteResult);
            Assert.True(deleteResult.All(d => d.Changed));

            // Verify Delegation deleted
            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&agent={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientPackagesDto>>(delegationsToAgentPayload);

            Assert.Empty(delegationToAgentResult.Items);
        }

        [Fact]
        public async Task DeleteAccessPackageToAgent_WithUnknownPartyClientAndAgent_Returns400WithV2QueryPointers()
        {
            var client = CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Route}/agents/accesspackages?party={Guid.NewGuid()}&client={Guid.NewGuid()}&agent={Guid.NewGuid()}")
            {
                Content = JsonContent.Create(new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                })
            };

            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);

            Assert.Single(problem.Errors, e => e.Paths.Contains("$QUERY/party"));
            Assert.Single(problem.Errors, e => e.Paths.Contains("$QUERY/client"));
            Assert.Contains(problem.Errors, e => e.Paths.Contains("$QUERY/agent"));
            Assert.DoesNotContain(problem.Errors, e => e.Paths.Contains("$QUERY/from") || e.Paths.Contains("$QUERY/to"));
        }
    }
    #endregion

    #region DELETE accessmanagement/api/v2/enduser/clientdelegations/my/clients/accesspackages

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteMyPackagesToClientViaProvider(Guid, Guid, DelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DeleteMyClientAccessPackages : IClassFixture<ApiFixture>
    {
        public DeleteMyClientAccessPackages(ApiFixture fixture)
        {
            Fixture = fixture;
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DeleteMyClientAccessPackages_WithUnknownProviderAndClient_Returns400WithProviderAndClientQueryPointers()
        {
            var client = CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Route}/my/clients/accesspackages?provider={Guid.NewGuid()}&client={Guid.NewGuid()}")
            {
                Content = JsonContent.Create(new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                })
            };

            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data, SerializerOptions);

            Assert.Contains(problem.Errors, e => e.Paths.Contains("$QUERY/provider"));
            Assert.Single(problem.Errors, e => e.Paths.Contains("$QUERY/client"));
            Assert.DoesNotContain(problem.Errors, e => e.Paths.Contains("$QUERY/party") || e.Paths.Contains("$QUERY/from") || e.Paths.Contains("$QUERY/to") || e.Paths.Contains("$QUERY/agent"));
        }
    }
    #endregion
}
