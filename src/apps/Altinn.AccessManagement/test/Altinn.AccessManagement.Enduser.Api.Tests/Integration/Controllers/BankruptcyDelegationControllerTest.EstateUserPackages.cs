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
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BankruptcyDelegationController"/>, focused on the package list
/// that <see cref="BankruptcyDelegationController.AddBankruptcyEstateForUser"/> and
/// <see cref="BankruptcyDelegationController.RevokeBankruptcyEstateForUser"/> take in the request body.
/// </summary>
public partial class BankruptcyDelegationControllerTest
{
    /// <summary>
    /// Tests for the package list handling in <c>POST/DELETE estates/users</c>.
    /// </summary>
    /// <remarks>
    /// party (bankruptcy administrator) = <see cref="TestEntities.PersonMatilde"/> is EstateAdministrator
    /// for every estate used here, and <see cref="TestEntities.PersonPaula"/> is her Agent.
    /// <see cref="TestEntities.PersonHenrik"/> is deliberately *not* an Agent.
    /// <para>
    /// Each test that persists a delegation uses its own estate, so the tests stay independent of
    /// execution order. The tests that only assert validation failures share
    /// <see cref="TestEntities.OrganizationSolsidenSameie"/>, since a rejected request writes nothing.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class AddRevokeBankruptcyEstatePackages : IClassFixture<ApiFixture>
    {
        /// <summary>
        /// A package that is delegable for the EstateAdministrator (bobe) role directly.
        /// </summary>
        private static readonly string BobePackageUrn = PackageConstants.BankruptcyEstateReadAccess.Entity.Urn;

        /// <summary>
        /// A second package delegable for the EstateAdministrator role, used when a test needs two.
        /// </summary>
        private static readonly string SecondBobePackageUrn = PackageConstants.BankruptcyEstateWriteAccess.Entity.Urn;

        /// <summary>
        /// A package that is only reachable through the MainAdministrator (hadm) role packages. It is
        /// delegable here solely because the EstateAdministrator role can delegate the MainAdministrator
        /// package, which makes the service fold the hadm role packages into the available set.
        /// </summary>
        private static readonly string MainAdminOnlyPackageUrn = PackageConstants.ConfidentialMailToBusiness.Entity.Urn;

        /// <summary>
        /// A package that belongs to the Auditor role only and is not delegable for anyone.
        /// </summary>
        private static readonly string NonDelegablePackageUrn = PackageConstants.AuditorInCharge.Entity.Urn;

        private const string UnknownPackageUrn = "urn:altinn:accesspackage:does-not-exist";

        public AddRevokeBankruptcyEstatePackages(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<AddRevokeBankruptcyEstatePackages>(db =>
            {
                // The party administrates every estate used by these tests.
                foreach (var estate in Estates)
                {
                    db.Assignments.Add(new Assignment()
                    {
                        FromId = estate,
                        ToId = TestEntities.PersonMatilde.Id,
                        RoleId = RoleConstants.EstateAdministrator,
                    });
                }

                // Paula is an Agent for the party. Henrik deliberately is not.
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.PersonMatilde.Id,
                    ToId = TestEntities.PersonPaula.Id,
                    RoleId = RoleConstants.Agent,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private static Guid[] Estates =>
        [
            TestEntities.OrganizationSolsidenSameie.Id,
            TestEntities.OrganizationOkernBorettslag.Id,
            TestEntities.OrganizationNordisAS.Id,
            TestEntities.OrganizationVerdiqAS.Id,
            TestEntities.OrganizationNufExampleNUF.Id,
            TestEntities.OrganizationOrsta.Id,
        ];

        private static string Url(Guid estate, Guid user) =>
            $"{Route}/estates/users?party={TestEntities.PersonMatilde.Id}&estate={estate}&user={user}";

        private HttpClient CreateAdministratorClient() =>
            CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

        private static async Task<AltinnValidationProblemDetails> AssertBadRequest(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {content}");

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, JsonOptions);
            Assert.NotNull(problem);
            return problem;
        }

        private static async Task<CreateDelegationResponseDto> AssertOk(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            var result = JsonSerializer.Deserialize<CreateDelegationResponseDto>(content, JsonOptions);
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.DelegationId);
            return result;
        }

        /// <summary>
        /// Asserts that no delegation exists from the estate to the user, facilitated by the party.
        /// </summary>
        private async Task AssertNoDelegation(Guid estate, Guid user)
        {
            await Fixture.QueryDb(async db =>
            {
                var delegations = await db.Delegations
                    .AsNoTracking()
                    .Where(d =>
                        d.FacilitatorId == TestEntities.PersonMatilde.Id &&
                        d.From.FromId == estate &&
                        d.From.RoleId == RoleConstants.EstateAdministrator &&
                        d.To.ToId == user)
                    .ToListAsync(TestContext.Current.CancellationToken);

                Assert.Empty(delegations);
            });
        }

        private async Task<List<DelegationPackage>> GetDelegationPackages(Guid delegationId)
        {
            List<DelegationPackage> packages = null;

            await Fixture.QueryDb(async db =>
            {
                packages = await db.DelegationPackages
                    .AsNoTracking()
                    .Where(dp => dp.DelegationId == delegationId)
                    .ToListAsync(TestContext.Current.CancellationToken);
            });

            return packages;
        }

        /// <summary>
        /// A package urn that does not resolve to a known package is rejected with
        /// PackageNotExists (AM.VLD-00011) before anything is written.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithUnknownPackageUrn_Returns400PackageNotExists()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonPaula.Id),
                PackagesContent(UnknownPackageUrn),
                TestContext.Current.CancellationToken);

            var problem = await AssertBadRequest(response);

            var error = Assert.Single(problem.Errors);
            Assert.Equal("AM.VLD-00011", error.ErrorCode.ToString());
        }

        /// <summary>
        /// Every unknown urn in the list produces its own error, and a valid urn alongside them
        /// does not rescue the request.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithTwoUnknownAndOneValidUrn_Returns400WithErrorPerUnknownUrn()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonPaula.Id),
                PackagesContent(UnknownPackageUrn, BobePackageUrn, "urn:altinn:accesspackage:also-unknown"),
                TestContext.Current.CancellationToken);

            var problem = await AssertBadRequest(response);

            Assert.Equal(2, problem.Errors.Count);
            Assert.All(problem.Errors, e => Assert.Equal("AM.VLD-00011", e.ErrorCode.ToString()));
        }

        /// <summary>
        /// A package that exists but is not delegable for the EstateAdministrator role (nor through the
        /// folded-in MainAdministrator role packages) is rejected with PackageIsNotDelegable (AM.VLD-00033),
        /// and nothing is persisted.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithNonDelegablePackage_Returns400PackageIsNotDelegable()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonPaula.Id),
                PackagesContent(NonDelegablePackageUrn),
                TestContext.Current.CancellationToken);

            var problem = await AssertBadRequest(response);

            var error = Assert.Single(problem.Errors);
            Assert.Equal("AM.VLD-00033", error.ErrorCode.ToString());

            await Fixture.QueryDb(async db =>
            {
                Assert.Empty(await db.DelegationPackages
                    .AsNoTracking()
                    .Where(dp => dp.PackageId == PackageConstants.AuditorInCharge.Id)
                    .ToListAsync(TestContext.Current.CancellationToken));
            });
        }

        /// <summary>
        /// A package that only the MainAdministrator role can delegate is accepted, because the
        /// EstateAdministrator role may delegate the MainAdministrator package and the service therefore
        /// folds the MainAdministrator role packages into the available set. The resulting
        /// DelegationPackage points at the MainAdministrator role package.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithMainAdministratorOnlyPackage_Returns200AndUsesMainAdminRolePackage()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationOkernBorettslag.Id, TestEntities.PersonPaula.Id),
                PackagesContent(MainAdminOnlyPackageUrn),
                TestContext.Current.CancellationToken);

            var result = await AssertOk(response);

            await Fixture.QueryDb(async db =>
            {
                var delegationPackage = await db.DelegationPackages
                    .AsNoTracking()
                    .Where(dp => dp.DelegationId == result.DelegationId)
                    .SingleAsync(TestContext.Current.CancellationToken);

                Assert.Equal(PackageConstants.ConfidentialMailToBusiness.Id, delegationPackage.PackageId);

                var rolePackage = await db.RolePackages
                    .AsNoTracking()
                    .SingleAsync(rp => rp.Id == delegationPackage.RolePackageId, TestContext.Current.CancellationToken);

                Assert.Equal(RoleConstants.MainAdministrator.Id, rolePackage.RoleId);
            });
        }

        /// <summary>
        /// The MainAdministrator package itself is delegable directly for the EstateAdministrator role.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithMainAdministratorPackage_Returns200()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationNordisAS.Id, TestEntities.PersonPaula.Id),
                PackagesContent(PackageConstants.MainAdministrator.Entity.Urn),
                TestContext.Current.CancellationToken);

            var result = await AssertOk(response);

            var packages = await GetDelegationPackages(result.DelegationId);
            var package = Assert.Single(packages);
            Assert.Equal(PackageConstants.MainAdministrator.Id, package.PackageId);
        }

        /// <summary>
        /// The user must already be an Agent of the party. A user without that assignment is rejected
        /// with MissingAssignment (AM.VLD-00032) even though the party administrates the estate.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WhenUserIsNotAgent_Returns400MissingAssignment()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonHenrik.Id),
                PackagesContent(BobePackageUrn),
                TestContext.Current.CancellationToken);

            var problem = await AssertBadRequest(response);

            var error = Assert.Single(problem.Errors);
            Assert.Equal("AM.VLD-00032", error.ErrorCode.ToString());
        }

        /// <summary>
        /// An empty package list passes model binding (the body is present, just empty), so the
        /// controller guards against it explicitly. Expects 400 with a Required validation error and
        /// no delegation created.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithEmptyPackageList_Returns400Required()
        {
            var client = CreateAdministratorClient();

            var response = await client.PostAsync(
                Url(TestEntities.OrganizationVerdiqAS.Id, TestEntities.PersonPaula.Id),
                PackagesContent(),
                TestContext.Current.CancellationToken);

            var problem = await AssertBadRequest(response);

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.Required.ErrorCode);
            Assert.Single(problem.Errors, e => e.Paths.Contains("/packages"));

            await AssertNoDelegation(TestEntities.OrganizationVerdiqAS.Id, TestEntities.PersonPaula.Id);
        }

        /// <summary>
        /// Revoking with an empty package list is rejected the same way, so that a request that
        /// expresses no intent never reaches the service.
        /// </summary>
        [Fact]
        public async Task RevokeBankruptcyEstateForUser_WithEmptyPackageList_Returns400Required()
        {
            var client = CreateAdministratorClient();

            var response = await DeleteWithBodyAsync(
                client,
                Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonPaula.Id),
                PackagesContent());

            var problem = await AssertBadRequest(response);

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.Required.ErrorCode);
            Assert.Single(problem.Errors, e => e.Paths.Contains("/packages"));
        }

        /// <summary>
        /// The empty-list guard runs after the estate-connection check, so a caller that does not
        /// administrate the estate still gets 403 rather than feedback about the request body.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithEmptyPackageListAndNoEstateConnection_ReturnsForbidden()
        {
            var client = CreateAdministratorClient();

            // Kasper is an entity the party is not EstateAdministrator for.
            var response = await client.PostAsync(
                Url(TestEntities.PersonKasper.Id, TestEntities.PersonPaula.Id),
                PackagesContent(),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Delegating the same package twice is idempotent: the second call returns the same delegation
        /// and does not add a second DelegationPackage row.
        /// </summary>
        [Fact]
        public async Task AddBankruptcyEstateForUser_WithSamePackageTwice_DoesNotDuplicateDelegationPackage()
        {
            var client = CreateAdministratorClient();
            var url = Url(TestEntities.OrganizationNufExampleNUF.Id, TestEntities.PersonPaula.Id);

            var first = await AssertOk(await client.PostAsync(url, PackagesContent(BobePackageUrn), TestContext.Current.CancellationToken));
            var second = await AssertOk(await client.PostAsync(url, PackagesContent(BobePackageUrn), TestContext.Current.CancellationToken));

            Assert.Equal(first.DelegationId, second.DelegationId);

            var packages = await GetDelegationPackages(first.DelegationId);
            var package = Assert.Single(packages);
            Assert.Equal(PackageConstants.BankruptcyEstateReadAccess.Id, package.PackageId);
        }

        /// <summary>
        /// Revoking a subset of the delegated packages removes only those packages and keeps the
        /// delegation alive with the remaining ones.
        /// </summary>
        [Fact]
        public async Task RevokeBankruptcyEstateForUser_WithSubsetOfPackages_KeepsDelegationWithRemainingPackage()
        {
            var client = CreateAdministratorClient();
            var url = Url(TestEntities.OrganizationOrsta.Id, TestEntities.PersonPaula.Id);

            var added = await AssertOk(await client.PostAsync(
                url,
                PackagesContent(BobePackageUrn, SecondBobePackageUrn),
                TestContext.Current.CancellationToken));

            Assert.Equal(2, (await GetDelegationPackages(added.DelegationId)).Count);

            var revokeResponse = await DeleteWithBodyAsync(client, url, PackagesContent(BobePackageUrn));

            var revokeContent = await revokeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(revokeResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {revokeResponse.StatusCode}. Response body: {revokeContent}");

            var remaining = await GetDelegationPackages(added.DelegationId);
            var package = Assert.Single(remaining);
            Assert.Equal(PackageConstants.BankruptcyEstateWriteAccess.Id, package.PackageId);

            await Fixture.QueryDb(async db =>
            {
                Assert.NotNull(await db.Delegations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == added.DelegationId, TestContext.Current.CancellationToken));
            });
        }

        /// <summary>
        /// An unknown package urn is rejected on revoke as well, since the urn list is resolved before
        /// anything else happens.
        /// </summary>
        [Fact]
        public async Task RevokeBankruptcyEstateForUser_WithUnknownPackageUrn_Returns400PackageNotExists()
        {
            var client = CreateAdministratorClient();

            var response = await DeleteWithBodyAsync(
                client,
                Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonPaula.Id),
                PackagesContent(UnknownPackageUrn));

            var problem = await AssertBadRequest(response);

            var error = Assert.Single(problem.Errors);
            Assert.Equal("AM.VLD-00011", error.ErrorCode.ToString());
        }

        /// <summary>
        /// Revoke and add disagree about a user that is not an Agent of the party: add reports
        /// MissingAssignment, while revoke returns 204 without validating the packages at all — the
        /// non-delegable package in the body is never reported. This pins the asymmetry.
        /// </summary>
        [Fact]
        public async Task RevokeBankruptcyEstateForUser_WhenUserIsNotAgent_Returns204WithoutValidatingPackages()
        {
            var client = CreateAdministratorClient();
            var url = Url(TestEntities.OrganizationSolsidenSameie.Id, TestEntities.PersonHenrik.Id);

            var addResponse = await client.PostAsync(url, PackagesContent(NonDelegablePackageUrn), TestContext.Current.CancellationToken);
            await AssertBadRequest(addResponse);

            var revokeResponse = await DeleteWithBodyAsync(client, url, PackagesContent(NonDelegablePackageUrn));

            var revokeContent = await revokeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(revokeResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {revokeResponse.StatusCode}. Response body: {revokeContent}");
        }
    }
}
