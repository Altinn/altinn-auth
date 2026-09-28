using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BankruptcyDelegationController"/>, covering the two read
/// endpoints that report what has been delegated: <c>GET users/accesspackages</c> (estates and
/// packages per agent) and <c>GET estates/accesspackages</c> (agents and packages per estate).
/// </summary>
public partial class BankruptcyDelegationControllerTest
{
    /// <summary>
    /// Seeds a delegation of an estate to an agent, with one DelegationPackage per package. The
    /// facilitator is the party both assignments have in common: the estate administrator.
    /// </summary>
    /// <param name="db">Database context supplied by the fixture seed.</param>
    /// <param name="estate">An EstateAdministrator assignment, estate -> party.</param>
    /// <param name="agent">An Agent assignment, party -> user.</param>
    /// <param name="packageIds">Packages to delegate. The EstateAdministrator role package is used for each.</param>
    internal static void SeedEstateDelegation(AppDbContext db, Assignment estate, Assignment agent, params Guid[] packageIds)
    {
        var delegation = new AccessMgmt.PersistenceEF.Models.Delegation
        {
            FromId = estate.Id,
            ToId = agent.Id,
            FacilitatorId = estate.ToId,
        };
        db.Delegations.Add(delegation);

        foreach (var packageId in packageIds)
        {
            var rolePackage = db.RolePackages
                .AsNoTracking()
                .First(rp => rp.RoleId == RoleConstants.EstateAdministrator.Id && rp.PackageId == packageId);

            db.DelegationPackages.Add(new DelegationPackage
            {
                DelegationId = delegation.Id,
                PackageId = packageId,
                RolePackageId = rolePackage.Id,
            });
        }
    }

    /// <summary>
    /// Tests for <see cref="BankruptcyDelegationController.GetBankruptcyEstatesWithPackagesForUser"/> and
    /// <see cref="BankruptcyDelegationController.GetUsersWithPackagesForBankruptcyEstate"/>.
    /// </summary>
    /// <remarks>
    /// Reads the shared seed described on <see cref="BankruptcyReadOnlyFixture"/>: Paula has Solsiden
    /// (read + write) and Økern (read), Kasper has Nordis (read), Ørjan has nothing delegated, and
    /// <see cref="TestEntities.PersonHenrik"/> is not an Agent at all.
    /// </remarks>
    [IntegrationTest]
    [Collection(BankruptcyReadOnlyCollection.Name)]
    public class GetBankruptcyEstatesAndUsersWithPackages
    {
        public GetBankruptcyEstatesAndUsersWithPackages(BankruptcyReadOnlyFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeeded();
        }

        public BankruptcyReadOnlyFixture Fixture { get; }

        private HttpClient CreateAdministratorClient() =>
            CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

        private static string UserUrl(Guid user) =>
            $"{Route}/users/accesspackages?party={TestEntities.PersonMatilde.Id}&user={user}";

        private static string EstateUrl(Guid estate) =>
            $"{Route}/estates/accesspackages?party={TestEntities.PersonMatilde.Id}&estate={estate}";

        private async Task<PaginatedResult<T>> GetPaginated<T>(string url)
        {
            var client = CreateAdministratorClient();

            var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            var result = JsonSerializer.Deserialize<PaginatedResult<T>>(content, JsonOptions);
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            return result;
        }

        private static List<Guid> PackageIds(IEnumerable<ClientDto.RoleAccessPackages> access) =>
            access.SelectMany(a => a.Packages).Select(p => p.Id).ToList();

        private static List<Guid> PackageIds(IEnumerable<AgentDto.AgentRoleAccessPackages> access) =>
            access.SelectMany(a => a.Packages).Select(p => p.Id).ToList();

        /// <summary>
        /// Returns exactly the estates delegated to the given agent, each with the packages delegated
        /// on that estate, and nothing delegated to another agent of the same party.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesWithPackagesForUser_ForAgentWithDelegations_Returns200WithEstatesAndPackages()
        {
            var result = await GetPaginated<ClientDto>(UserUrl(TestEntities.PersonPaula.Id));

            Assert.Equal(2, result.Items.Count());
            Assert.DoesNotContain(result.Items, x => x.Client.Id == TestEntities.OrganizationNordisAS.Id);

            var solsiden = Assert.Single(result.Items, x => x.Client.Id == TestEntities.OrganizationSolsidenSameie.Id);
            var solsidenAccess = Assert.Single(solsiden.Access);
            Assert.Equal(RoleConstants.EstateAdministrator.Id, solsidenAccess.Role.Id);
            var solsidenPackages = PackageIds(solsiden.Access);
            Assert.Equal(2, solsidenPackages.Count);
            Assert.Contains(PackageConstants.BankruptcyEstateReadAccess.Id, solsidenPackages);
            Assert.Contains(PackageConstants.BankruptcyEstateWriteAccess.Id, solsidenPackages);

            var okern = Assert.Single(result.Items, x => x.Client.Id == TestEntities.OrganizationOkernBorettslag.Id);
            Assert.Equal(PackageConstants.BankruptcyEstateReadAccess.Id, Assert.Single(PackageIds(okern.Access)));
        }

        /// <summary>
        /// A different agent of the same party sees only their own estate.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesWithPackagesForUser_ForOtherAgent_Returns200WithThatAgentsEstateOnly()
        {
            var result = await GetPaginated<ClientDto>(UserUrl(TestEntities.PersonKasper.Id));

            var estate = Assert.Single(result.Items);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, estate.Client.Id);
            Assert.Equal(PackageConstants.BankruptcyEstateReadAccess.Id, Assert.Single(PackageIds(estate.Access)));
        }

        /// <summary>
        /// An agent without any delegated estates gets an empty list rather than an error.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesWithPackagesForUser_ForAgentWithoutDelegations_Returns200WithEmptyList()
        {
            var result = await GetPaginated<ClientDto>(UserUrl(TestEntities.PersonOrjan.Id));

            Assert.Empty(result.Items);
        }

        /// <summary>
        /// A user that is not an Agent of the party gets an empty list.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesWithPackagesForUser_ForUserThatIsNotAgent_Returns200WithEmptyList()
        {
            var result = await GetPaginated<ClientDto>(UserUrl(TestEntities.PersonHenrik.Id));

            Assert.Empty(result.Items);
        }

        /// <summary>
        /// Returns the agent the estate is delegated to, with the packages delegated on that estate
        /// only: the agent's packages on their other estate do not leak in.
        /// </summary>
        [Fact]
        public async Task GetUsersWithPackagesForBankruptcyEstate_ForDelegatedEstate_Returns200WithOnlyThatEstatesPackages()
        {
            var result = await GetPaginated<AgentDto>(EstateUrl(TestEntities.OrganizationOkernBorettslag.Id));

            var agent = Assert.Single(result.Items);
            Assert.Equal(TestEntities.PersonPaula.Id, agent.Agent.Id);
            var access = Assert.Single(agent.Access);
            Assert.Equal(RoleConstants.EstateAdministrator.Id, access.Role.Id);
            Assert.Equal(PackageConstants.BankruptcyEstateReadAccess.Id, Assert.Single(PackageIds(agent.Access)));
        }

        /// <summary>
        /// Returns every package the agent has on the estate.
        /// </summary>
        [Fact]
        public async Task GetUsersWithPackagesForBankruptcyEstate_ForEstateWithSeveralPackages_Returns200WithAllPackages()
        {
            var result = await GetPaginated<AgentDto>(EstateUrl(TestEntities.OrganizationSolsidenSameie.Id));

            var agent = Assert.Single(result.Items);
            Assert.Equal(TestEntities.PersonPaula.Id, agent.Agent.Id);
            var packages = PackageIds(agent.Access);
            Assert.Equal(2, packages.Count);
            Assert.Contains(PackageConstants.BankruptcyEstateReadAccess.Id, packages);
            Assert.Contains(PackageConstants.BankruptcyEstateWriteAccess.Id, packages);
        }

        /// <summary>
        /// An estate the party does not administrate is rejected rather than returning an empty list.
        /// </summary>
        [Fact]
        public async Task GetUsersWithPackagesForBankruptcyEstate_ForEstateNotConnectedToParty_Returns400()
        {
            var client = CreateAdministratorClient();

            var response = await client.GetAsync(EstateUrl(TestEntities.PersonHenrik.Id), TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {content}");

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, JsonOptions);
            Assert.NotNull(problem);
            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.InvalidBankruptcyEstate.ErrorCode);
        }
    }
}
