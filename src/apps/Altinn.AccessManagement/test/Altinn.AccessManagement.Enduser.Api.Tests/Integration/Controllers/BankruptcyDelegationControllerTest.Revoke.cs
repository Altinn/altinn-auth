using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BankruptcyDelegationController"/>, covering the revoke routes:
/// the cascade flag on <c>DELETE users</c>, the <c>POST .../delete</c> aliases, and what is actually
/// left in the database after a revoke.
/// </summary>
public partial class BankruptcyDelegationControllerTest
{
    /// <summary>
    /// Tests for <see cref="BankruptcyDelegationController.RevokeAgent"/>, which refuses to remove an
    /// agent that still has delegated access unless <c>cascade=true</c>.
    /// </summary>
    /// <remarks>
    /// party = <see cref="TestEntities.PersonMatilde"/> administrates Solsiden and Økern.
    /// <see cref="TestEntities.PersonPaula"/> and <see cref="TestEntities.PersonKasper"/> are agents
    /// with a delegated estate each (one per test, since the cascading revoke removes it).
    /// <see cref="TestEntities.PersonOrjan"/> and <see cref="TestEntities.OrganizationOrsta"/> are
    /// agents without any delegation.
    /// </remarks>
    [IntegrationTest]
    public class RevokeAgentCascade : IClassFixture<ApiFixture>
    {
        public RevokeAgentCascade(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RevokeAgentCascade>(db =>
            {
                var solsiden = new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.PersonMatilde.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                };
                var okern = new Assignment()
                {
                    FromId = TestEntities.OrganizationOkernBorettslag.Id,
                    ToId = TestEntities.PersonMatilde.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                };

                var paula = Agent(TestEntities.PersonPaula.Id);
                var kasper = Agent(TestEntities.PersonKasper.Id);
                var orjan = Agent(TestEntities.PersonOrjan.Id);
                var orsta = Agent(TestEntities.OrganizationOrsta.Id);

                db.Assignments.AddRange(solsiden, okern, paula, kasper, orjan, orsta);

                SeedEstateDelegation(db, solsiden, paula, PackageConstants.BankruptcyEstateReadAccess.Id);
                SeedEstateDelegation(db, okern, kasper, PackageConstants.BankruptcyEstateReadAccess.Id);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private static Assignment Agent(Guid user) => new()
        {
            FromId = TestEntities.PersonMatilde.Id,
            ToId = user,
            RoleId = RoleConstants.Agent,
        };

        private HttpClient CreateAdministratorClient() =>
            CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

        private static string UserUrl(Guid user, bool? cascade = null) =>
            $"{Route}/users?party={TestEntities.PersonMatilde.Id}&user={user}"
            + (cascade is { } value ? $"&cascade={value.ToString().ToLowerInvariant()}" : string.Empty);

        /// <summary>
        /// An agent that still holds a delegated estate cannot be removed without cascade. Expects 400
        /// with DelegationHasActiveConnections (AM.VLD-00031) and the agent assignment left intact.
        /// </summary>
        [Fact]
        public async Task RevokeAgent_WithoutCascadeWhenAgentHasDelegatedPackages_Returns400()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(UserUrl(TestEntities.PersonPaula.Id), TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {content}");

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, JsonOptions);
            Assert.NotNull(problem);
            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.DelegationHasActiveConnections.ErrorCode);

            await Fixture.QueryDb(async db =>
            {
                Assert.NotNull(await db.Assignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.FromId == TestEntities.PersonMatilde.Id && a.ToId == TestEntities.PersonPaula.Id && a.RoleId == RoleConstants.Agent,
                        TestContext.Current.CancellationToken));
            });
        }

        /// <summary>
        /// The same request with cascade=true removes the agent assignment along with the delegation
        /// and its packages.
        /// </summary>
        [Fact]
        public async Task RevokeAgent_WithCascadeWhenAgentHasDelegatedPackages_Returns204AndRemovesDelegation()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(UserUrl(TestEntities.PersonKasper.Id, cascade: true), TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {content}");

            await Fixture.QueryDb(async db =>
            {
                Assert.Null(await db.Assignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.FromId == TestEntities.PersonMatilde.Id && a.ToId == TestEntities.PersonKasper.Id && a.RoleId == RoleConstants.Agent,
                        TestContext.Current.CancellationToken));

                Assert.Empty(await db.Delegations
                    .AsNoTracking()
                    .Where(d => d.To.ToId == TestEntities.PersonKasper.Id)
                    .ToListAsync(TestContext.Current.CancellationToken));
            });
        }

        /// <summary>
        /// An agent without delegated access is removed without cascade.
        /// </summary>
        [Fact]
        public async Task RevokeAgent_WithoutCascadeWhenAgentHasNoDelegations_Returns204()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(UserUrl(TestEntities.PersonOrjan.Id), TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {content}");

            await Fixture.QueryDb(async db =>
            {
                Assert.Null(await db.Assignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.FromId == TestEntities.PersonMatilde.Id && a.ToId == TestEntities.PersonOrjan.Id && a.RoleId == RoleConstants.Agent,
                        TestContext.Current.CancellationToken));
            });
        }

        /// <summary>
        /// Revoking a user that never was an agent is a no-op, not an error.
        /// </summary>
        [Fact]
        public async Task RevokeAgent_ForUserThatIsNotAgent_Returns204()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(UserUrl(TestEntities.PersonHenrik.Id), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// The POST alias exists for clients that cannot send DELETE, and behaves identically.
        /// </summary>
        [Fact]
        public async Task RevokeAgent_ViaPostDeleteAlias_Returns204AndRemovesAssignment()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                $"{Route}/users/delete?party={TestEntities.PersonMatilde.Id}&user={TestEntities.OrganizationOrsta.Id}",
                null,
                TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {content}");

            await Fixture.QueryDb(async db =>
            {
                Assert.Null(await db.Assignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.FromId == TestEntities.PersonMatilde.Id && a.ToId == TestEntities.OrganizationOrsta.Id && a.RoleId == RoleConstants.Agent,
                        TestContext.Current.CancellationToken));
            });
        }
    }

    /// <summary>
    /// Tests for what <see cref="BankruptcyDelegationController.RevokeCreditor"/> and
    /// <see cref="BankruptcyDelegationController.RevokeAdministrator"/> leave behind.
    /// </summary>
    /// <remarks>
    /// Both services remove their own package from the Rightholder assignment and then call
    /// <c>DeleteAssignment(..., cascade: false, ...)</c> while discarding its result. When the
    /// assignment carries another package that delete fails, but the endpoint still answers 204. These
    /// tests pin that, so a change in either direction is visible.
    /// </remarks>
    [IntegrationTest]
    public class RevokeResidualState : IClassFixture<ApiFixture>
    {
        public RevokeResidualState(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RevokeResidualState>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.PersonMatilde.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                });

                // Creditor with an extra package on the same Rightholder assignment.
                var creditorWithExtra = Rightholder(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.OrganizationOrsta.Id);
                db.Assignments.Add(creditorWithExtra);
                db.AssignmentPackages.Add(Package(creditorWithExtra, PackageConstants.BankruptcyEstateReadAccess.Id));
                db.AssignmentPackages.Add(Package(creditorWithExtra, PackageConstants.AccessManager.Id));

                // Creditor with only the bankruptcy read access package.
                var creditorPlain = Rightholder(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.OrganizationOkernBorettslag.Id);
                db.Assignments.Add(creditorPlain);
                db.AssignmentPackages.Add(Package(creditorPlain, PackageConstants.BankruptcyEstateReadAccess.Id));

                // Administrator with an extra package on the same Rightholder assignment.
                var adminWithExtra = Rightholder(TestEntities.PersonMatilde.Id, TestEntities.PersonPaula.Id);
                db.Assignments.Add(adminWithExtra);
                db.AssignmentPackages.Add(Package(adminWithExtra, PackageConstants.KonkursboAdministrator.Id));
                db.AssignmentPackages.Add(Package(adminWithExtra, PackageConstants.AccessManager.Id));

                // Administrator with only the admin package, revoked through the POST alias.
                var adminPlain = Rightholder(TestEntities.PersonMatilde.Id, TestEntities.PersonKasper.Id);
                db.Assignments.Add(adminPlain);
                db.AssignmentPackages.Add(Package(adminPlain, PackageConstants.KonkursboAdministrator.Id));

                // Rightholder without the admin package at all.
                db.Assignments.Add(Rightholder(TestEntities.PersonMatilde.Id, TestEntities.PersonOrjan.Id));

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private static Assignment Rightholder(Guid from, Guid to) => new()
        {
            FromId = from,
            ToId = to,
            RoleId = RoleConstants.Rightholder,
        };

        private static AssignmentPackage Package(Assignment assignment, Guid packageId) => new()
        {
            AssignmentId = assignment.Id,
            PackageId = packageId,
        };

        private HttpClient CreateAdministratorClient() =>
            CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

        private async Task<List<AssignmentPackage>> GetAssignmentPackages(Guid from, Guid to)
        {
            List<AssignmentPackage> packages = null;

            await Fixture.QueryDb(async db =>
            {
                packages = await db.AssignmentPackages
                    .AsNoTracking()
                    .Where(ap => ap.Assignment.FromId == from && ap.Assignment.ToId == to && ap.Assignment.RoleId == RoleConstants.Rightholder)
                    .ToListAsync(TestContext.Current.CancellationToken);
            });

            return packages;
        }

        private async Task<Assignment> GetRightholderAssignment(Guid from, Guid to)
        {
            Assignment assignment = null;

            await Fixture.QueryDb(async db =>
            {
                assignment = await db.Assignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        a => a.FromId == from && a.ToId == to && a.RoleId == RoleConstants.Rightholder,
                        TestContext.Current.CancellationToken);
            });

            return assignment;
        }

        /// <summary>
        /// Revoking a creditor whose Rightholder assignment only carries the bankruptcy read access
        /// package removes the package and the assignment.
        /// </summary>
        [Fact]
        public async Task RevokeCreditor_WhenAssignmentHasNoOtherPackage_Returns204AndRemovesAssignment()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(
                $"{Route}/estates/creditors?party={TestEntities.PersonMatilde.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&creditor={TestEntities.OrganizationOkernBorettslag.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.Empty(await GetAssignmentPackages(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.OrganizationOkernBorettslag.Id));
            Assert.Null(await GetRightholderAssignment(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.OrganizationOkernBorettslag.Id));
        }

        /// <summary>
        /// When the creditor's Rightholder assignment also carries an unrelated package, only the
        /// bankruptcy read access package is removed. The assignment itself survives, because the
        /// non-cascading delete that follows cannot remove an assignment that still has packages — and
        /// its result is discarded, so the caller still gets 204.
        /// </summary>
        [Fact]
        public async Task RevokeCreditor_WhenAssignmentHasAnotherPackage_Returns204ButKeepsAssignmentAndOtherPackage()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(
                $"{Route}/estates/creditors?party={TestEntities.PersonMatilde.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&creditor={TestEntities.OrganizationOrsta.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var packages = await GetAssignmentPackages(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.OrganizationOrsta.Id);
            var remaining = Assert.Single(packages);
            Assert.Equal(PackageConstants.AccessManager.Id, remaining.PackageId);

            Assert.NotNull(await GetRightholderAssignment(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.OrganizationOrsta.Id));
        }

        /// <summary>
        /// Revoking a creditor that was never added is a no-op, not an error.
        /// </summary>
        [Fact]
        public async Task RevokeCreditor_ForUnknownCreditor_Returns204()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(
                $"{Route}/estates/creditors?party={TestEntities.PersonMatilde.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&creditor={TestEntities.OrganizationNordisAS.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Same residual state on the administrator route: the admin package goes, the unrelated
        /// package and the assignment stay.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_WhenAssignmentHasAnotherPackage_Returns204ButKeepsAssignmentAndOtherPackage()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(
                $"{Route}/users/administrators?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonPaula.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var packages = await GetAssignmentPackages(TestEntities.PersonMatilde.Id, TestEntities.PersonPaula.Id);
            var remaining = Assert.Single(packages);
            Assert.Equal(PackageConstants.AccessManager.Id, remaining.PackageId);

            Assert.NotNull(await GetRightholderAssignment(TestEntities.PersonMatilde.Id, TestEntities.PersonPaula.Id));
        }

        /// <summary>
        /// A rightholder without the administrator package is left untouched, and the endpoint still
        /// answers 204.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_ForRightholderWithoutAdminPackage_Returns204AndKeepsAssignment()
        {
            var client = CreateAdministratorClient();

            var response = await client.DeleteAsync(
                $"{Route}/users/administrators?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonOrjan.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.NotNull(await GetRightholderAssignment(TestEntities.PersonMatilde.Id, TestEntities.PersonOrjan.Id));
        }

        /// <summary>
        /// The POST alias for revoking an administrator behaves like the DELETE route.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_ViaPostDeleteAlias_Returns204AndRemovesAssignment()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                $"{Route}/users/administrators/delete?party={TestEntities.PersonMatilde.Id}&user={TestEntities.PersonKasper.Id}",
                null,
                TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {content}");

            Assert.Empty(await GetAssignmentPackages(TestEntities.PersonMatilde.Id, TestEntities.PersonKasper.Id));
            Assert.Null(await GetRightholderAssignment(TestEntities.PersonMatilde.Id, TestEntities.PersonKasper.Id));
        }
    }
}
