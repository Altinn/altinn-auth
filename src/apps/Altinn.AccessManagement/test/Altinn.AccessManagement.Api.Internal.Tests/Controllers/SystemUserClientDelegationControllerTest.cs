using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Internal.Controllers.Internal;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Api.Internal.Tests.Controllers;

public class SystemUserClientDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v1/internal/systemuserclientdelegation";

    #region GET accessmanagement/api/v1/internal/systemuserclientdelegation/clients

    /// <summary>
    /// <see cref="SystemUserClientDelegationController.GetClients(Guid, string[], string[], CancellationToken)"/>
    ///
    /// Authentication reads this endpoint as a fallback until it moves onto
    /// enduser/clientdelegations/clients, so these tests pin the filter semantics it has today
    /// against the real service rather than a mocked one.
    /// </summary>
    [IntegrationTest]
    public class GetClients : IClassFixture<ApiFixture>
    {
        public GetClients(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetClients>(db =>
            {
                // Nordis reaches the facilitator through two roles. Rettighetshaver has no role
                // packages, so its only package is the directly delegated one, while regnskapsforer
                // carries its packages through the role. That split is what makes Nordis the client
                // that proves the packages filter counts both sources as one set.
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

                // Okern holds the regnskapsforer role packages but not the delegated toll package,
                // so it drops out as soon as toll joins the packages filter.
                var accountantFromOkernToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationOkernBorettslag.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };

                // Solsiden is a rettighetshaver without assignment packages. Rettighetshaver has no
                // role packages either, so this connection provides no access at all and the client
                // is never listed.
                var rightholderFromSolsidenToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(accountantFromOkernToVerdiq);
                db.Assignments.Add(rightholderFromSolsidenToVerdiq);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderFromNordisToVerdiq.Id,
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
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        private async Task<List<SystemuserClientDto>> GetClientsWithFilter(string filter)
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}{filter}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            return JsonSerializer.Deserialize<List<SystemuserClientDto>>(data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        [Fact]
        public async Task ListClients_WithoutRolesFilter_Returns200WithClientsHoldingAnyValidClientRole()
        {
            var result = await GetClientsWithFilter(string.Empty);

            // The endpoint fills in every valid client role when none is given. That fill narrows the
            // query, it is not something a client is matched against: Nordis holds two of the six
            // roles and Okern one, and both are listed.
            var nordis = Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationOkernBorettslag.Id);

            Assert.Contains(nordis.Access, a => a.Role == RoleConstants.Rightholder.Entity.Code);
            Assert.Contains(nordis.Access, a => a.Role == RoleConstants.Accountant.Entity.Code);
        }

        [Fact]
        public async Task ListClients_WithoutRolesFilter_Returns200WithoutClientWhoseConnectionProvidesNoPackages()
        {
            var result = await GetClientsWithFilter(string.Empty);

            // Solsiden is a rettighetshaver with neither assignment packages nor role packages.
            Assert.DoesNotContain(result, c => c.Party.Id == TestEntities.OrganizationSolsidenSameie.Id);
        }

        [Fact]
        public async Task ListClients_WithRolesFilter_Returns200WithClientsHoldingAtLeastOneOfTheRoles()
        {
            var result = await GetClientsWithFilter($"&roles={RoleConstants.Rightholder.Entity.Code}&roles={RoleConstants.Auditor.Entity.Code}");

            // The roles filter is a union. Nordis is a rettighetshaver but not a revisor, and is
            // still returned. Okern is neither, and is not.
            Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.DoesNotContain(result, c => c.Party.Id == TestEntities.OrganizationOkernBorettslag.Id);
        }

        [Fact]
        public async Task ListClients_WithRolesFilter_Returns200WithAccessNarrowedToTheFilteredRoles()
        {
            var result = await GetClientsWithFilter($"&roles={RoleConstants.Rightholder.Entity.Code}");

            // Filtering on a role narrows which assignments are read, so Nordis comes back with its
            // rettighetshaver access only, even though it also reaches the facilitator as regnskapsforer.
            var nordis = Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationNordisAS.Id);

            var access = Assert.Single(nordis.Access);
            Assert.Equal(RoleConstants.Rightholder.Entity.Code, access.Role);
            Assert.Equal([PackageConstants.Customs.Entity.Code], access.Packages);
        }

        [Fact]
        public async Task ListClients_WithSinglePackageFilter_Returns200WithEveryClientHoldingIt()
        {
            var result = await GetClientsWithFilter($"&packages={PackageConstants.AccountantSalary.Entity.Code}");

            // Both clients hold the package through the regnskapsforer role.
            Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationOkernBorettslag.Id);
        }

        [Fact]
        public async Task ListClients_WithSeveralPackagesFilter_Returns200WithOnlyClientsHoldingEveryPackage()
        {
            var result = await GetClientsWithFilter($"&packages={PackageConstants.AccountantSalary.Entity.Code}&packages={PackageConstants.Customs.Entity.Code}");

            // The packages filter is an intersection, and role packages and directly delegated
            // packages count as one set: Nordis holds regnskapsforer-lonn through the role and toll
            // through the assignment. Okern holds only the first and drops out.
            Assert.Single(result, c => c.Party.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.DoesNotContain(result, c => c.Party.Id == TestEntities.OrganizationOkernBorettslag.Id);
        }

        [Fact]
        public async Task ListClients_WithUnknownRoleFilter_Returns400()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&roles=notarole", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    #endregion

    #region POST accessmanagement/api/v1/internal/systemuserclientdelegation

    /// <summary>
    /// <see cref="SystemUserClientDelegationController.PostClientDelegation(Guid, CreateSystemDelegationRequestDto, CancellationToken)"/>
    ///
    /// The Bruno negative tests in SystemUserClientDelegation/NegativeTests send these same
    /// requests in AT22 and TT02, so the error codes asserted here are the ones they assert.
    /// </summary>
    [IntegrationTest]
    public class PostClientDelegation : IClassFixture<ApiFixture>
    {
        public PostClientDelegation(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<PostClientDelegation>(db =>
            {
                // Okern is a regnskapsforer client of Verdiq and nothing else, so it holds the
                // regnskapsforer role packages but no forretningsforer assignment.
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationOkernBorettslag.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private async Task<HttpResponseMessage> Post(Guid clientId, Guid agentId, string roleIdentifier, string packageUrn)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE));
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var request = new CreateSystemDelegationRequestDto
            {
                ClientId = clientId,
                AgentId = agentId,
                AgentName = "Test agent",
                AgentRole = RoleConstants.Agent.Entity.Code,
                RolePackages = [new() { RoleIdentifier = roleIdentifier, PackageUrn = packageUrn }],
            };

            return await client.PostAsJsonAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}", request, TestContext.Current.CancellationToken);
        }

        private static async Task<List<string>> ValidationErrorCodes(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return [.. body.RootElement.GetProperty("validationErrors").EnumerateArray().Select(e => e.GetProperty("code").GetString())];
        }

        [Fact]
        public async Task PostClientDelegation_UnknownPackage_Returns400InvalidPackage()
        {
            var response = await Post(TestEntities.OrganizationOkernBorettslag.Id, TestEntities.SystemUserClient.Id, RoleConstants.Accountant.Entity.Code, "xyz12345");

            Assert.Equal(["AM.VLD-00030"], await ValidationErrorCodes(response));
        }

        [Fact]
        public async Task PostClientDelegation_AgentIsPerson_Returns400DisallowedEntityType()
        {
            var response = await Post(TestEntities.OrganizationOkernBorettslag.Id, TestEntities.PersonPaula.Id, RoleConstants.Accountant.Entity.Code, PackageConstants.AccountantSalary.Entity.Urn);

            Assert.Equal(["AM.VLD-00008"], await ValidationErrorCodes(response));
        }

        [Fact]
        public async Task PostClientDelegation_ClientNotConnectedToFacilitator_Returns400MissingAssignment()
        {
            var response = await Post(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.SystemUserClient.Id, RoleConstants.Accountant.Entity.Code, PackageConstants.AccountantSalary.Entity.Urn);

            Assert.Equal(["AM.VLD-00032"], await ValidationErrorCodes(response));
        }

        [Fact]
        public async Task PostClientDelegation_ForretningsforerPackageForAccountantClient_Returns400MissingAssignment()
        {
            var response = await Post(TestEntities.OrganizationOkernBorettslag.Id, TestEntities.SystemUserClient.Id, RoleConstants.BusinessManager.Entity.Code, PackageConstants.BusinessManagerRealEstate.Entity.Urn);

            Assert.Equal(["AM.VLD-00032"], await ValidationErrorCodes(response));
        }

        [Fact]
        public async Task PostClientDelegation_PackageTheClientHolds_Returns200WithDelegation()
        {
            var response = await Post(TestEntities.OrganizationOkernBorettslag.Id, TestEntities.SystemUserClient.Id, RoleConstants.Accountant.Entity.Code, PackageConstants.AccountantSalary.Entity.Urn);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<List<CreateDelegationResponseDto>>(TestContext.Current.CancellationToken);
            Assert.Single(result);
        }
    }

    #endregion
}
