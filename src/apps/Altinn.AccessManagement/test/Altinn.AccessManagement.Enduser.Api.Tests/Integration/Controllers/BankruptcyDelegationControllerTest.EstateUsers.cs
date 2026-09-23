using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BanckruptcyDelegationController"/>, covering the two read
/// endpoints that report what a single agent has been given: <c>GET estates/users</c> and
/// <c>GET estates/users/packages</c>.
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
    /// Tests for <see cref="BanckruptcyDelegationController.GetBankruptcyEstatesForUser"/> and
    /// <see cref="BanckruptcyDelegationController.GetBankruptcyEstatePackagesForUser"/>.
    /// </summary>
    /// <remarks>
    /// Reads the shared seed described on <see cref="BankruptcyReadOnlyFixture"/>: Paula has Solsiden
    /// and Økern, Kasper has Nordis, Ørjan has nothing delegated, and
    /// <see cref="TestEntities.PersonHenrik"/> is not an Agent at all.
    /// </remarks>
    [IntegrationTest]
    [Collection(BankruptcyReadOnlyCollection.Name)]
    public class GetBankruptcyEstatesAndPackagesForUser
    {
        public GetBankruptcyEstatesAndPackagesForUser(BankruptcyReadOnlyFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeeded();
        }

        public BankruptcyReadOnlyFixture Fixture { get; }

        private HttpClient CreateAdministratorClient() =>
            CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

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

        /// <summary>
        /// Returns exactly the estates delegated to the given agent, and nothing delegated to another
        /// agent of the same party.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesForUser_ForAgentWithDelegations_Returns200WithOnlyThatAgentsEstates()
        {
            var result = await GetPaginated<CompactEntityDto>(
                $"{Route}/estates/users?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonPaula.Id}");

            Assert.Equal(2, result.Items.Count());
            Assert.Contains(result.Items, x => x.Id == TestEntities.OrganizationSolsidenSameie.Id);
            Assert.Contains(result.Items, x => x.Id == TestEntities.OrganizationOkernBorettslag.Id);
            Assert.DoesNotContain(result.Items, x => x.Id == TestEntities.OrganizationNordisAS.Id);
        }

        /// <summary>
        /// A different agent of the same party sees only their own estate.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesForUser_ForOtherAgent_Returns200WithThatAgentsEstateOnly()
        {
            var result = await GetPaginated<CompactEntityDto>(
                $"{Route}/estates/users?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonKasper.Id}");

            var estate = Assert.Single(result.Items);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, estate.Id);
        }

        /// <summary>
        /// An agent without any delegated estates gets an empty list rather than an error.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesForUser_ForAgentWithoutDelegations_Returns200WithEmptyList()
        {
            var result = await GetPaginated<CompactEntityDto>(
                $"{Route}/estates/users?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonOrjan.Id}");

            Assert.Empty(result.Items);
        }

        /// <summary>
        /// A user that is not an Agent of the party gets an empty list.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatesForUser_ForUserThatIsNotAgent_Returns200WithEmptyList()
        {
            var result = await GetPaginated<CompactEntityDto>(
                $"{Route}/estates/users?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonHenrik.Id}");

            Assert.Empty(result.Items);
        }

        /// <summary>
        /// Returns the packages delegated on the given estate to the given agent.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatePackagesForUser_ForDelegatedEstate_Returns200WithPackages()
        {
            var result = await GetPaginated<PackageDto>(
                $"{Route}/estates/users/packages?party={TestEntities.PersonMatilde.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&user={TestEntities.PersonPaula.Id}");

            Assert.Equal(2, result.Items.Count());
            Assert.Contains(result.Items, p => p.Id == PackageConstants.BankruptcyEstateReadAccess.Id);
            Assert.Contains(result.Items, p => p.Id == PackageConstants.BankruptcyEstateWriteAccess.Id);
        }

        /// <summary>
        /// The result is scoped to the requested estate: the packages delegated on the agent's other
        /// estate do not leak in.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatePackagesForUser_ForAgentWithTwoEstates_ReturnsOnlyRequestedEstatesPackages()
        {
            var result = await GetPaginated<PackageDto>(
                $"{Route}/estates/users/packages?party={TestEntities.PersonMatilde.Id}&estate={TestEntities.OrganizationOkernBorettslag.Id}&user={TestEntities.PersonPaula.Id}");

            var package = Assert.Single(result.Items);
            Assert.Equal(PackageConstants.BankruptcyEstateReadAccess.Id, package.Id);
        }

        /// <summary>
        /// An estate the party administrates but has not delegated to this agent yields an empty list.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstatePackagesForUser_ForEstateNotDelegatedToUser_Returns200WithEmptyList()
        {
            var result = await GetPaginated<PackageDto>(
                $"{Route}/estates/users/packages?party={TestEntities.PersonMatilde.Id}&estate={TestEntities.OrganizationNordisAS.Id}&user={TestEntities.PersonPaula.Id}");

            Assert.Empty(result.Items);
        }
    }
}
