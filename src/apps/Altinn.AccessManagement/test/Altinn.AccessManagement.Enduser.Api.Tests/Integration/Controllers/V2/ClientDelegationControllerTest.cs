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
    /// <see cref="ClientDelegationController.GetClients(Guid, List{string}?, List{string}?, List{string}?, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
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

                // A second client without assignment resources, used to assert the resources filter.
                var rightholderFromOkernToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationOkernBorettslag.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
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
                db.Assignments.Add(rightholderFromOkernToVerdiq);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                // The accountant assignment holds both a package and a resource, so a filtered
                // client lists full access for the matching assignment.
                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderFromOkernToVerdiq.Id,
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

        [Fact]
        public async Task ListClient_WithResourcesFilter_Returns200WithOnlyClientHoldingAssignmentResource()
        {
            var client = CreateClient();

            // Both clients are listed without the filter.
            var unfilteredResponse = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, unfilteredResponse.StatusCode);
            var unfilteredData = await unfilteredResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var unfilteredResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(unfilteredData);

            Assert.Contains(unfilteredResult.Items, p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.Contains(unfilteredResult.Items, p => p.Client.Id == TestEntities.OrganizationOkernBorettslag.Id);

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&resources={TestData.MattilsynetBakeryService.RefId}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            var nordisClient = Assert.Single(result.Items);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, nordisClient.Client.Id);

            // The whole client relationship is listed: the filter selects the client and trims the
            // resource lists, while packages and the other role entries stay intact.
            var accountantAccess = nordisClient.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(accountantAccess);

            var resource = Assert.Single(accountantAccess.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, resource.Id);

            Assert.Contains(accountantAccess.Packages, p => p.Id == PackageConstants.Customs.Id);

            var rightholderAccess = nordisClient.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Rightholder);
            Assert.NotNull(rightholderAccess);
            Assert.Empty(rightholderAccess.Resources);
        }

        [Fact]
        public async Task ListClient_WithUnknownResourcesFilter_Returns200WithEmptyResult()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&resources=unknown-resource-ref", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task ListClient_WithResourcesAndRolesFilter_Returns200WithClientsMatchingBothFilters()
        {
            var client = CreateClient();

            // The accountant assignment holds the resource, so the combined filter matches.
            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&roles=regnskapsforer&resources={TestData.MattilsynetBakeryService.RefId}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            var nordisClient = Assert.Single(result.Items);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, nordisClient.Client.Id);

            var accountantAccess = Assert.Single(nordisClient.Access);
            Assert.Equal(RoleConstants.Accountant.Id, accountantAccess.Role.Id);

            var resource = Assert.Single(accountantAccess.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, resource.Id);

            // No rightholder assignment holds the resource, so the combined filter matches nothing.
            response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&roles=rettighetshaver&resources={TestData.MattilsynetBakeryService.RefId}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task ListClient_WithPackagesFilter_Returns200WithOnlyClientHoldingPackage()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&packages={PackageConstants.AccountantSalary.Entity.Urn}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientDto>>(data);

            // Only the client holding the package through its accountant role is selected. The
            // package list is trimmed to the match while the resource list stays intact.
            var nordisClient = Assert.Single(result.Items);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, nordisClient.Client.Id);

            var accountantAccess = Assert.Single(nordisClient.Access);
            Assert.Equal(RoleConstants.Accountant.Id, accountantAccess.Role.Id);

            var package = Assert.Single(accountantAccess.Packages);
            Assert.Equal(PackageConstants.AccountantSalary.Id, package.Id);

            var resource = Assert.Single(accountantAccess.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, resource.Id);
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
                var agentFromNordisToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                var accountantFromVerdiqToNordis = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.OrganizationNordisAS.Id,
                    RoleId = RoleConstants.Accountant,
                };

                var assignmentResourceAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromVerdiqToNordis.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var delegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromVerdiqToNordis.Id,
                    ToId = agentFromNordisToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationNordisAS.Id,
                };

                var delegationResourceToPaula = new DelegationResource()
                {
                    DelegationId = delegationToPaula.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceAccountant.Id,
                };

                db.Assignments.Add(agentFromNordisToPaula);
                db.Assignments.Add(accountantFromVerdiqToNordis);
                db.AssignmentResources.Add(assignmentResourceAccountant);
                db.Delegations.Add(delegationToPaula);
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

        [Fact]
        public async Task ListAgent_ForAgentWithDelegatedResource_Returns200WithResourcesPerRole()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.NotNull(connection);

            var accountantAccess = connection.Access.FirstOrDefault(r => r.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(accountantAccess);
            Assert.Empty(accountantAccess.Packages);

            var resource = Assert.Single(accountantAccess.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, resource.Id);
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

    #region POST accessmanagement/api/v2/enduser/clientdelegations/agents/accesspackages/delete

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
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/agents/accesspackages/delete?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}")
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

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/agents/accesspackages/delete?party={Guid.NewGuid()}&client={Guid.NewGuid()}&agent={Guid.NewGuid()}")
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

    #region POST accessmanagement/api/v2/enduser/clientdelegations/my/clients/accesspackages/delete

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

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/my/clients/accesspackages/delete?provider={Guid.NewGuid()}&client={Guid.NewGuid()}")
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

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteAgentAccessPackage(Guid, Guid, Guid, DelegationBatchInputDto, CancellationToken)"/>
    /// Batch removal spanning multiple roles, where each role empties its own delegation.
    /// </summary>
    [IntegrationTest]
    public class DeleteAgentAccessPackagesAcrossRoles : IClassFixture<ApiFixture>
    {
        public DeleteAgentAccessPackagesAcrossRoles(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DeleteAgentAccessPackagesAcrossRoles>(db =>
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

                var assignmentPackageCustoms = new AssignmentPackage()
                {
                    AssignmentId = rightholderFromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
                };

                // Each role has its own delegation to Paula carrying a single package.
                var rightholderDelegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = rightholderFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var accountantDelegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var rolePackage = db.RolePackages.FirstOrDefault(r => r.RoleId == RoleConstants.Accountant && r.PackageId == PackageConstants.AccountantWithSigningRights);
                var rightholderDelegationPackage = new DelegationPackage()
                {
                    DelegationId = rightholderDelegationToPaula.Id,
                    AssignmentPackageId = assignmentPackageCustoms.Id,
                    PackageId = PackageConstants.Customs.Id,
                };

                var accountantDelegationPackage = new DelegationPackage()
                {
                    DelegationId = accountantDelegationToPaula.Id,
                    RolePackageId = rolePackage.Id,
                    PackageId = PackageConstants.AccountantWithSigningRights.Id,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.AssignmentPackages.Add(assignmentPackageCustoms);
                db.Delegations.Add(rightholderDelegationToPaula);
                db.Delegations.Add(accountantDelegationToPaula);
                db.DelegationPackages.Add(rightholderDelegationPackage);
                db.DelegationPackages.Add(accountantDelegationPackage);

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
        public async Task DeleteAccessPackages_WithBatchEmptyingTwoDelegationsAcrossRoles_Returns200WithBothDelegationsRemoved()
        {
            var client = CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/agents/accesspackages/delete?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}")
            {
                Content = JsonContent.Create(new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn],
                        },
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Packages = [PackageConstants.AccountantWithSigningRights.Entity.Urn],
                        }
                    ]
                })
            };

            var deleteResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            var deleteResult = JsonSerializer.Deserialize<List<DelegationDto>>(deleteResponsePayload);
            Assert.Equal(2, deleteResult.Count);
            Assert.True(deleteResult.All(d => d.Changed));

            // Both delegations were emptied by their own role in the batch, so both must be removed.
            await Fixture.QueryDb(static async db =>
            {
                var delegations = await db.Delegations
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id && d.To.ToId == TestEntities.PersonPaula.Id)
                    .ToListAsync(TestContext.Current.CancellationToken);

                Assert.Empty(delegations);
            });
        }
    }
    #endregion

    #region GET accessmanagement/api/v2/enduser/clientdelegations/agents/resources

    /// <summary>
    /// <see cref="ClientDelegationController.GetDelegatedResourcesToAgentsViaParty(Guid, Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetDelegatedResourcesToAgents : IClassFixture<ApiFixture>
    {
        public GetDelegatedResourcesToAgents(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetDelegatedResourcesToAgents>(db =>
            {
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

                var assignmentResourceAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var delegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationResourceToPaula = new DelegationResource()
                {
                    DelegationId = delegationToPaula.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceAccountant.Id,
                };

                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.AssignmentResources.Add(assignmentResourceAccountant);
                db.Delegations.Add(delegationToPaula);
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
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task ListDelegatedResourcesToAgent_ForAgentWithDelegatedResource_Returns200WithResourcesGroupedByClient()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS.Id}&agent={TestEntities.PersonPaula.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientResourcesDto>>(data);

            var nordisClient = Assert.Single(result.Items);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, nordisClient.Client.Id);

            var access = Assert.Single(nordisClient.Access);
            Assert.Equal(RoleConstants.Accountant.Id, access.Role.Id);

            var resource = Assert.Single(access.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, resource.Id);
        }

        [Fact]
        public async Task ListDelegatedResourcesToAgent_ForAgentWithoutDelegatedResources_Returns200WithEmptyResult()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS.Id}&agent={TestEntities.PersonOrjan.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientResourcesDto>>(data);

            Assert.Empty(result.Items);
        }
    }
    #endregion

    #region GET accessmanagement/api/v2/enduser/clientdelegations/clients/resources

    /// <summary>
    /// <see cref="ClientDelegationController.GetDelegatedResourcesFromClientsViaParty(Guid, Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetDelegatedResourcesFromClients : IClassFixture<ApiFixture>
    {
        public GetDelegatedResourcesFromClients(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetDelegatedResourcesFromClients>(db =>
            {
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

                var assignmentResourceAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var delegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationResourceToPaula = new DelegationResource()
                {
                    DelegationId = delegationToPaula.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceAccountant.Id,
                };

                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.AssignmentResources.Add(assignmentResourceAccountant);
                db.Delegations.Add(delegationToPaula);
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
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task ListDelegatedResourcesFromClient_ForClientWithDelegatedResource_Returns200WithResourcesGroupedByAgent()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients/resources?party={TestEntities.OrganizationVerdiqAS.Id}&client={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentResourcesDto>>(data);

            var paulaAgent = Assert.Single(result.Items);
            Assert.Equal(TestEntities.PersonPaula.Id, paulaAgent.Agent.Id);

            var access = Assert.Single(paulaAgent.Access);
            Assert.Equal(RoleConstants.Accountant.Id, access.Role.Id);

            var resource = Assert.Single(access.Resources);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, resource.Id);
        }
    }
    #endregion

    #region POST accessmanagement/api/v2/enduser/clientdelegations/agents/resources

    /// <summary>
    /// <see cref="ClientDelegationController.DelegateResourceToAgent(Guid, Guid, Guid, ContractsV2.ResourceDelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DelegateResourceToAgent : IClassFixture<ApiFixture>
    {
        public DelegateResourceToAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DelegateResourceToAgent>(db =>
            {
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

                // The client Nordis delegated the bakery service to the facilitator Verdiq as a single resource.
                var assignmentResourceAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);
                db.AssignmentResources.Add(assignmentResourceAccountant);

                // Verdiq also holds an accountant package from Nordis. Resources available through the
                // package have no AssignmentResource and are not delegable as single resources.
                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    PackageId = PackageConstants.AccountantWithSigningRights,
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
        public async Task DelegateResource_WithSingleResourceAssignment_Returns200WithLinkedAssignmentResource()
        {
            var client = CreateClient();

            var delegationResult = await Delegate(client);

            var delegated = Assert.Single(delegationResult);
            Assert.True(delegated.Changed);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, delegated.ResourceId);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, delegated.FromId);
            Assert.Equal(TestEntities.PersonPaula.Id, delegated.ToId);
            Assert.Equal(TestEntities.OrganizationVerdiqAS.Id, delegated.ViaId);

            // Second delegation is a no-op.
            delegationResult = await Delegate(client);
            delegated = Assert.Single(delegationResult);
            Assert.False(delegated.Changed);

            // The delegated resource points at the originating assignment resource.
            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Include(d => d.DelegationResources)
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id
                        && d.To.ToId == TestEntities.PersonPaula.Id
                        && d.From.FromId == TestEntities.OrganizationNordisAS.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(delegation);
                var delegationResource = Assert.Single(delegation.DelegationResources);
                Assert.Equal(TestData.MattilsynetBakeryService.Id, delegationResource.ResourceId);

                var assignmentResource = await db.AssignmentResources
                    .FirstOrDefaultAsync(ar => ar.Id == delegationResource.AssignmentResourceId, TestContext.Current.CancellationToken);
                Assert.NotNull(assignmentResource);
                Assert.Equal(TestData.MattilsynetBakeryService.Id, assignmentResource.ResourceId);
            });

            // Read back from the client side.
            var getFromClient = await client.GetAsync($"{Route}/clients/resources?party={TestEntities.OrganizationVerdiqAS.Id}&client={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);
            var fromClientPayload = await getFromClient.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var fromClientResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.AgentResourcesDto>>(fromClientPayload);

            var paulaAccess = fromClientResult.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.NotNull(paulaAccess);
            var paulaResource = paulaAccess.Access.SelectMany(a => a.Resources).FirstOrDefault(r => r.Id == TestData.MattilsynetBakeryService.Id);
            Assert.NotNull(paulaResource);

            // Read back from the agent side.
            var getToAgent = await client.GetAsync($"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS.Id}&agent={TestEntities.PersonPaula.Id}", TestContext.Current.CancellationToken);
            var toAgentPayload = await getToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var toAgentResult = JsonSerializer.Deserialize<PaginatedResult<ContractsV2.ClientResourcesDto>>(toAgentPayload);

            var nordisAccess = toAgentResult.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS);
            Assert.NotNull(nordisAccess);
            var nordisResource = nordisAccess.Access.SelectMany(a => a.Resources).FirstOrDefault(r => r.Id == TestData.MattilsynetBakeryService.Id);
            Assert.NotNull(nordisResource);

            static async Task<List<ContractsV2.ResourceDelegationDto>> Delegate(HttpClient client)
            {
                var response = await client.PostAsJsonAsync(
                    $"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}",
                    new ContractsV2.ResourceDelegationBatchInputDto()
                    {
                        Values = [
                            new()
                            {
                                Role = RoleConstants.Accountant.Entity.Code,
                                Resources = [TestData.MattilsynetBakeryService.RefId]
                            }
                        ]
                    },
                    TestContext.Current.CancellationToken
                );

                var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                return JsonSerializer.Deserialize<List<ContractsV2.ResourceDelegationDto>>(data);
            }
        }

        [Fact]
        public async Task DelegateResource_WithResourceHeldThroughAccessPackageOnly_Returns400WithUserNotAuthorized()
        {
            var client = CreateClient();

            // Verdiq holds an accountant package from Nordis, but Sirius is not delegated to Verdiq as a
            // single resource. Package held access does not make a resource delegable onward.
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}",
                new ContractsV2.ResourceDelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = [TestData.SiriusSkattemelding.RefId]
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
                Assert.Equal(ValidationErrors.UserNotAuthorized.ErrorCode, error.ErrorCode);
            });

            // Nothing was persisted.
            await Fixture.QueryDb(static async db =>
            {
                var delegationResources = await db.DelegationResources
                    .Where(r => r.ResourceId == TestData.SiriusSkattemelding.Id)
                    .ToListAsync(TestContext.Current.CancellationToken);

                Assert.Empty(delegationResources);
            });
        }

        [Fact]
        public async Task DelegateResource_WithDuplicateResourceInPayload_Returns200WithSingleDelegationResource()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonOrjan}",
                new ContractsV2.ResourceDelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = [TestData.MattilsynetBakeryService.RefId, TestData.MattilsynetBakeryService.RefId]
                        },
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = [TestData.MattilsynetBakeryService.RefId]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = JsonSerializer.Deserialize<List<ContractsV2.ResourceDelegationDto>>(data);
            Assert.Equal(3, result.Count);
            Assert.Single(result, r => r.Changed);

            // One delegation and one delegation resource row despite the repeated entries.
            await Fixture.QueryDb(static async db =>
            {
                var delegations = await db.Delegations
                    .Include(d => d.DelegationResources)
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id && d.To.ToId == TestEntities.PersonOrjan.Id)
                    .ToListAsync(TestContext.Current.CancellationToken);

                var delegation = Assert.Single(delegations);
                var delegationResource = Assert.Single(delegation.DelegationResources);
                Assert.Equal(TestData.MattilsynetBakeryService.Id, delegationResource.ResourceId);
            });
        }

        [Fact]
        public async Task DelegateResource_WithNonExistentResource_Returns400WithResourceNotExists()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/resources?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={TestEntities.PersonPaula}",
                new ContractsV2.ResourceDelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = ["unknown-resource-ref"]
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
                Assert.Equal(ValidationErrors.ResourceNotExists.ErrorCode, error.ErrorCode);
            });
        }
    }

    #endregion

    #region POST accessmanagement/api/v2/enduser/clientdelegations/agents/resources/delete

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteAgentResource(Guid, Guid, Guid, ContractsV2.ResourceDelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DeleteAgentResource : IClassFixture<ApiFixture>
    {
        public DeleteAgentResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DeleteAgentResource>(db =>
            {
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

                var assignmentResourceAccountant = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                // Paula has only the resource delegated, so removing it must delete the delegation.
                var delegationToPaula = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationResourceToPaula = new DelegationResource()
                {
                    DelegationId = delegationToPaula.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceAccountant.Id,
                };

                // Orjan has both a package and the resource, so removing the resource keeps the delegation.
                var rolePackage = db.RolePackages.FirstOrDefault(r => r.RoleId == RoleConstants.Accountant && r.PackageId == PackageConstants.AccountantWithSigningRights);
                var delegationToOrjan = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToOrjan.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationPackageToOrjan = new DelegationPackage()
                {
                    DelegationId = delegationToOrjan.Id,
                    RolePackageId = rolePackage.Id,
                    PackageId = PackageConstants.AccountantWithSigningRights,
                };

                var delegationResourceToOrjan = new DelegationResource()
                {
                    DelegationId = delegationToOrjan.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceAccountant.Id,
                };

                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);
                db.AssignmentResources.Add(assignmentResourceAccountant);
                db.Delegations.Add(delegationToPaula);
                db.Delegations.Add(delegationToOrjan);
                db.DelegationResources.Add(delegationResourceToPaula);
                db.DelegationPackages.Add(delegationPackageToOrjan);
                db.DelegationResources.Add(delegationResourceToOrjan);

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
        public async Task DeleteResource_WhenLastAccessOnDelegation_Returns200WithRemovedDelegation()
        {
            var client = CreateClient();

            var deleteResult = await DeleteResource(client, TestEntities.PersonPaula.Id);

            var deleted = Assert.Single(deleteResult);
            Assert.True(deleted.Changed);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, deleted.ResourceId);

            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id && d.To.ToId == TestEntities.PersonPaula.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.Null(delegation);

                var delegationResource = await db.DelegationResources
                    .Where(r => r.ResourceId == TestData.MattilsynetBakeryService.Id && r.Delegation.To.ToId == TestEntities.PersonPaula.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.Null(delegationResource);
            });

            // Second removal is a no-op.
            deleteResult = await DeleteResource(client, TestEntities.PersonPaula.Id);
            deleted = Assert.Single(deleteResult);
            Assert.False(deleted.Changed);
        }

        [Fact]
        public async Task DeleteResource_WithRemainingPackage_Returns200WithKeptDelegation()
        {
            var client = CreateClient();

            var deleteResult = await DeleteResource(client, TestEntities.PersonOrjan.Id);

            var deleted = Assert.Single(deleteResult);
            Assert.True(deleted.Changed);

            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Include(d => d.DelegationPackages)
                    .Include(d => d.DelegationResources)
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id && d.To.ToId == TestEntities.PersonOrjan.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(delegation);
                Assert.Empty(delegation.DelegationResources);
                Assert.NotEmpty(delegation.DelegationPackages);
            });
        }

        private static async Task<List<ContractsV2.ResourceDelegationDto>> DeleteResource(HttpClient client, Guid agent)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/agents/resources/delete?party={TestEntities.OrganizationVerdiqAS}&client={TestEntities.OrganizationNordisAS}&agent={agent}")
            {
                Content = JsonContent.Create(new ContractsV2.ResourceDelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = [TestData.MattilsynetBakeryService.RefId],
                        }
                    ]
                })
            };

            var deleteResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            return JsonSerializer.Deserialize<List<ContractsV2.ResourceDelegationDto>>(deleteResponsePayload);
        }
    }
    #endregion

    #region POST accessmanagement/api/v2/enduser/clientdelegations/my/clients/resources/delete

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteMyResourcesToClientViaProvider(Guid, Guid, ContractsV2.ResourceDelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class DeleteMyClientResources : IClassFixture<ApiFixture>
    {
        public DeleteMyClientResources(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<DeleteMyClientResources>(db =>
            {
                // Paula is the agent on both delegations, since the endpoint always acts
                // on behalf of the authenticated user.
                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };

                var accountantFromOkernToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationOkernBorettslag.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };

                var assignmentResourceNordis = new AssignmentResource()
                {
                    AssignmentId = accountantFromNordisToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                var assignmentResourceOkern = new AssignmentResource()
                {
                    AssignmentId = accountantFromOkernToVerdiq.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    PolicyPath = "mattilsynet-baker-konditorvare/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                };

                // From Nordis, Paula holds only the resource, so removing it deletes the delegation.
                var delegationFromNordis = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromNordisToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationResourceFromNordis = new DelegationResource()
                {
                    DelegationId = delegationFromNordis.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceNordis.Id,
                };

                // From Okern, Paula also holds a package, so removing the resource keeps the delegation.
                var rolePackage = db.RolePackages.FirstOrDefault(r => r.RoleId == RoleConstants.Accountant && r.PackageId == PackageConstants.AccountantWithSigningRights);
                var delegationFromOkern = new AccessMgmt.PersistenceEF.Models.Delegation()
                {
                    FromId = accountantFromOkernToVerdiq.Id,
                    ToId = agentFromVerdiqToPaula.Id,
                    FacilitatorId = TestEntities.OrganizationVerdiqAS.Id,
                };

                var delegationPackageFromOkern = new DelegationPackage()
                {
                    DelegationId = delegationFromOkern.Id,
                    RolePackageId = rolePackage.Id,
                    PackageId = PackageConstants.AccountantWithSigningRights,
                };

                var delegationResourceFromOkern = new DelegationResource()
                {
                    DelegationId = delegationFromOkern.Id,
                    ResourceId = TestData.MattilsynetBakeryService.Id,
                    AssignmentResourceId = assignmentResourceOkern.Id,
                };

                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(accountantFromOkernToVerdiq);
                db.AssignmentResources.Add(assignmentResourceNordis);
                db.AssignmentResources.Add(assignmentResourceOkern);
                db.Delegations.Add(delegationFromNordis);
                db.Delegations.Add(delegationFromOkern);
                db.DelegationResources.Add(delegationResourceFromNordis);
                db.DelegationPackages.Add(delegationPackageFromOkern);
                db.DelegationResources.Add(delegationResourceFromOkern);

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
        public async Task DeleteMyClientResources_WhenLastAccessOnDelegation_Returns200WithRemovedDelegation()
        {
            var client = CreateClient();

            var deleteResult = await DeleteMyResource(client, TestEntities.OrganizationNordisAS.Id);

            var deleted = Assert.Single(deleteResult);
            Assert.True(deleted.Changed);
            Assert.Equal(TestData.MattilsynetBakeryService.Id, deleted.ResourceId);

            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id
                        && d.To.ToId == TestEntities.PersonPaula.Id
                        && d.From.FromId == TestEntities.OrganizationNordisAS.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.Null(delegation);
            });

            // Second removal is a no-op.
            deleteResult = await DeleteMyResource(client, TestEntities.OrganizationNordisAS.Id);
            deleted = Assert.Single(deleteResult);
            Assert.False(deleted.Changed);
        }

        [Fact]
        public async Task DeleteMyClientResources_WithRemainingPackage_Returns200WithKeptDelegation()
        {
            var client = CreateClient();

            var deleteResult = await DeleteMyResource(client, TestEntities.OrganizationOkernBorettslag.Id);

            var deleted = Assert.Single(deleteResult);
            Assert.True(deleted.Changed);

            await Fixture.QueryDb(static async db =>
            {
                var delegation = await db.Delegations
                    .Include(d => d.DelegationPackages)
                    .Include(d => d.DelegationResources)
                    .Where(d => d.FacilitatorId == TestEntities.OrganizationVerdiqAS.Id
                        && d.To.ToId == TestEntities.PersonPaula.Id
                        && d.From.FromId == TestEntities.OrganizationOkernBorettslag.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(delegation);
                Assert.Empty(delegation.DelegationResources);
                Assert.NotEmpty(delegation.DelegationPackages);
            });
        }

        [Fact]
        public async Task DeleteMyClientResources_WithUnknownProviderAndClient_Returns400WithProviderAndClientQueryPointers()
        {
            var client = CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/my/clients/resources/delete?provider={Guid.NewGuid()}&client={Guid.NewGuid()}")
            {
                Content = JsonContent.Create(new ContractsV2.ResourceDelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = [TestData.MattilsynetBakeryService.RefId],
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

        private static async Task<List<ContractsV2.ResourceDelegationDto>> DeleteMyResource(HttpClient client, Guid clientId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Route}/my/clients/resources/delete?provider={TestEntities.OrganizationVerdiqAS}&client={clientId}")
            {
                Content = JsonContent.Create(new ContractsV2.ResourceDelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Resources = [TestData.MattilsynetBakeryService.RefId],
                        }
                    ]
                })
            };

            var deleteResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            return JsonSerializer.Deserialize<List<ContractsV2.ResourceDelegationDto>>(deleteResponsePayload);
        }
    }
    #endregion
}
