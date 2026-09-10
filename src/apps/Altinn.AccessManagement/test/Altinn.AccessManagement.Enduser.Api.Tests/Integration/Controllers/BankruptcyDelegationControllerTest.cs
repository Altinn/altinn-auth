using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.ProblemDetails;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests for <see cref="BanckruptcyDelegationController"/>.
/// </summary>
public class BankruptcyDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/bankruptcyestate";

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/users

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.GetAgentAdminInformation(Guid, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Seed data (estate party = <see cref="TestEntities.OrganizationVerdiqAS"/>):
    /// <list type="bullet">
    ///   <item><description><see cref="TestEntities.PersonPaula"/> has only an Agent assignment (AgentForhold) -> <see cref="BankruptcyEstatePermissions.User"/>.</description></item>
    ///   <item><description><see cref="TestEntities.PersonOrjan"/> has only an administrator delegation (Rightholder + KonkursboAdministrator package) -> <see cref="BankruptcyEstatePermissions.Admin"/>.</description></item>
    ///   <item><description><see cref="TestEntities.PersonKasper"/> has both -> <see cref="BankruptcyEstatePermissions.UserAndAdmin"/>.</description></item>
    /// </list>
    /// </remarks>
    [IntegrationTest]
    public class GetAgentAdminInformation : IClassFixture<ApiFixture>
    {
        public GetAgentAdminInformation(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetAgentAdminInformation>(db =>
            {
                // Paula: only AgentForhold (Agent assignment)
                var agentToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula.Id,
                    RoleId = RoleConstants.Agent,
                };

                // Orjan: only admin access delegated (Rightholder + KonkursboAdministrator package)
                var rightholderToOrjan = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonOrjan.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                var adminPackageToOrjan = new AssignmentPackage()
                {
                    AssignmentId = rightholderToOrjan.Id,
                    PackageId = PackageConstants.KonkursboAdministrator.Id,
                };

                // Kasper: both AgentForhold and admin access delegated
                var agentToKasper = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonKasper.Id,
                    RoleId = RoleConstants.Agent,
                };
                var rightholderToKasper = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonKasper.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                var adminPackageToKasper = new AssignmentPackage()
                {
                    AssignmentId = rightholderToKasper.Id,
                    PackageId = PackageConstants.KonkursboAdministrator.Id,
                };

                db.Assignments.Add(agentToPaula);
                db.Assignments.Add(rightholderToOrjan);
                db.Assignments.Add(agentToKasper);
                db.Assignments.Add(rightholderToKasper);

                db.AssignmentPackages.Add(adminPackageToOrjan);
                db.AssignmentPackages.Add(adminPackageToKasper);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

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
        /// A request without any authentication token cannot access the service.
        /// Expects 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task GetAgentAdminInformation_WithNoToken_Returns401Unauthorized()
        {
            var client = Fixture.Server.CreateClient();

            var response = await client.GetAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Authorized request returns the agents and administrators for the estate,
        /// including a user that only has AgentForhold, a user that only has admin
        /// access delegated, and a user that has both.
        /// </summary>
        [Fact]
        public async Task GetAgentAdminInformation_Authorized_Returns200WithUsersAndAdmins()
        {
            var client = CreateClient(TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.GetAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<BankruptcyEntityDto>>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            var paula = result.Items.FirstOrDefault(x => x.Id == TestEntities.PersonPaula.Id);
            var orjan = result.Items.FirstOrDefault(x => x.Id == TestEntities.PersonOrjan.Id);
            var kasper = result.Items.FirstOrDefault(x => x.Id == TestEntities.PersonKasper.Id);

            Assert.NotNull(paula);
            Assert.NotNull(orjan);
            Assert.NotNull(kasper);

            Assert.Equal(BankruptcyEstatePermissions.User, paula.Permissions);
            Assert.Equal(BankruptcyEstatePermissions.Admin, orjan.Permissions);
            Assert.Equal(BankruptcyEstatePermissions.UserAndAdmin, kasper.Permissions);
        }
    }

    #endregion

    private static HttpClient CreateClient(ApiFixture fixture, Guid partyUuid, params string[] scopes)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
            claims.Add(new Claim("scope", string.Join(" ", scopes)));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/estates/creditors

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.GetCreditors(Guid, Guid, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// party (bankruptcy administrator) = <see cref="TestEntities.OrganizationVerdiqAS"/>,
    /// estate = <see cref="TestEntities.OrganizationSolsidenSameie"/> connected via an
    /// EstateAdministrator assignment. The creditor <see cref="TestEntities.OrganizationNufExampleNUF"/>
    /// is a Rightholder with the BankruptcyEstateReadAccess package.
    /// </remarks>
    [IntegrationTest]
    public class GetCreditors : IClassFixture<ApiFixture>
    {
        public GetCreditors(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetCreditors>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                });

                var creditorAssignment = new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationNufExampleNUF.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(creditorAssignment);
                db.AssignmentPackages.Add(new AssignmentPackage()
                {
                    AssignmentId = creditorAssignment.Id,
                    PackageId = PackageConstants.BankruptcyEstateReadAccess.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task GetCreditors_Authorized_Returns200WithCreditors()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.GetAsync(
                $"{Route}/estates/creditors?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<CompactEntityDto>>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Contains(result.Items, x => x.Id == TestEntities.OrganizationNufExampleNUF.Id);
        }
    }

    #endregion

    #region POST accessmanagement/api/v1/enduser/bankruptcyestate/estates/creditors

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.AddCreditor(Guid, Guid, Guid?, AccessManagement.Api.Enduser.Models.PersonInput, CancellationToken)"/>.
    /// </summary>
    [IntegrationTest]
    public class AddCreditor : IClassFixture<ApiFixture>
    {
        public AddCreditor(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
            });
            Fixture.EnsureSeedOnce<AddCreditor>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task AddCreditor_ForOrganization_Returns200WithAssignment()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.PostAsync(
                $"{Route}/estates/creditors?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&creditor={TestEntities.OrganizationOrsta.Id}",
                null,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AssignmaentWithAssignmentPackageDto>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestEntities.OrganizationSolsidenSameie.Id, result.FromId);
            Assert.Equal(TestEntities.OrganizationOrsta.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
            Assert.Contains(result.AssignmentPackages, p => p.PackageId == PackageConstants.BankruptcyEstateReadAccess.Id);
        }

        /// <summary>
        /// Adds a person creditor that has no existing relationship to the party by
        /// posting a <see cref="PersonInput"/> body (SSN + last name). The mock
        /// <see cref="UserProfileLookupServiceMock"/> resolves the person by SSN and
        /// matching last name; expects OK with the created assignment.
        /// </summary>
        [Fact]
        public async Task AddCreditor_ForPersonViaPersonInput_Returns200WithAssignment()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "Farmor" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{Route}/estates/creditors?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}",
                content,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AssignmaentWithAssignmentPackageDto>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestEntities.OrganizationSolsidenSameie.Id, result.FromId);
            Assert.Equal(TestData.BodilFarmor.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
            Assert.Contains(result.AssignmentPackages, p => p.PackageId == PackageConstants.BankruptcyEstateReadAccess.Id);
        }

        /// <summary>
        /// Adds a person creditor via <see cref="PersonInput"/> where the supplied last name
        /// does not match the resolved person. The mock returns null and validation fails;
        /// expects 400 BadRequest with an InvalidExternalIdentifiers validation error.
        /// </summary>
        [Fact]
        public async Task AddCreditor_ForPersonViaPersonInputWithWrongLastName_Returns400()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "WrongName" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{Route}/estates/creditors?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}",
                content,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Single(result.Errors);
            Assert.Equal("AM.VLD-00034", result.Errors.First().ErrorCode.ToString());
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v1/enduser/bankruptcyestate/estates/creditors

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.RevokeCreditor(Guid, Guid, Guid, CancellationToken)"/>.
    /// </summary>
    [IntegrationTest]
    public class RevokeCreditor : IClassFixture<ApiFixture>
    {
        public RevokeCreditor(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RevokeCreditor>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                });

                var creditorAssignment = new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationOkernBorettslag.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(creditorAssignment);
                db.AssignmentPackages.Add(new AssignmentPackage()
                {
                    AssignmentId = creditorAssignment.Id,
                    PackageId = PackageConstants.BankruptcyEstateReadAccess.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task RevokeCreditor_ForExistingCreditor_Returns204()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.DeleteAsync(
                $"{Route}/estates/creditors?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&creditor={TestEntities.OrganizationOkernBorettslag.Id}",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }
    }

    #endregion

    #region POST accessmanagement/api/v1/enduser/bankruptcyestate/users

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.AddAgent(Guid, Guid?, AccessManagement.Api.Enduser.Models.PersonInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="TestEntities.PersonOrjan"/> already has an Agent assignment from the party, so the
    /// request resolves the existing connection and returns the existing assignment (idempotent happy flow).
    /// </remarks>
    [IntegrationTest]
    public class AddAgent : IClassFixture<ApiFixture>
    {
        public AddAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
            });
            Fixture.EnsureSeedOnce<AddAgent>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonOrjan.Id,
                    RoleId = RoleConstants.Agent,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task AddAgent_ForConnectedPerson_Returns200WithAssignment()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.PostAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}&user={TestEntities.PersonOrjan.Id}",
                null,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AssignmentDto>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestEntities.OrganizationVerdiqAS.Id, result.FromId);
            Assert.Equal(TestEntities.PersonOrjan.Id, result.ToId);
            Assert.Equal(RoleConstants.Agent.Id, result.RoleId);
        }

        /// <summary>
        /// Adds a person agent that has no existing relationship to the party by
        /// posting a <see cref="PersonInput"/> body (SSN + last name). The mock
        /// <see cref="UserProfileLookupServiceMock"/> resolves the person by SSN and
        /// matching last name; expects OK with the created assignment.
        /// </summary>
        [Fact]
        public async Task AddAgent_ForPersonViaPersonInput_Returns200WithAssignment()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "Farmor" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}",
                content,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AssignmentDto>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestEntities.OrganizationVerdiqAS.Id, result.FromId);
            Assert.Equal(TestData.BodilFarmor.Id, result.ToId);
            Assert.Equal(RoleConstants.Agent.Id, result.RoleId);
        }

        /// <summary>
        /// Adds a person agent via <see cref="PersonInput"/> where the supplied last name
        /// does not match the resolved person. The mock returns null and validation fails;
        /// expects 400 BadRequest with an InvalidExternalIdentifiers validation error.
        /// </summary>
        [Fact]
        public async Task AddAgent_ForPersonViaPersonInputWithWrongLastName_Returns400()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "WrongName" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}",
                content,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Single(result.Errors);
            Assert.Equal("AM.VLD-00034", result.Errors.First().ErrorCode.ToString());
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v1/enduser/bankruptcyestate/users

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.RevokeAgent(Guid, Guid, bool, CancellationToken)"/>.
    /// </summary>
    [IntegrationTest]
    public class RevokeAgent : IClassFixture<ApiFixture>
    {
        public RevokeAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RevokeAgent>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonMargit.Id,
                    RoleId = RoleConstants.Agent,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task RevokeAgent_ForExistingAgent_Returns204()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.DeleteAsync(
                $"{Route}/users?party={TestEntities.OrganizationVerdiqAS.Id}&user={TestEntities.PersonMargit.Id}&cascade=true",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }
    }

    #endregion

    #region PUT accessmanagement/api/v1/enduser/bankruptcyestate/users/administrators

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.AddAdministrator(Guid, Guid?, AccessManagement.Api.Enduser.Models.PersonInput, CancellationToken)"/>.
    /// </summary>
    [IntegrationTest]
    public class AddAdministrator : IClassFixture<ApiFixture>
    {
        public AddAdministrator(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
            });
            Fixture.EnsureSeedOnce<AddAdministrator>(db =>
            {
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task AddAdministrator_ForOrganization_Returns200WithAdminPackage()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.PutAsync(
                $"{Route}/users/administrators?party={TestEntities.OrganizationVerdiqAS.Id}&user={TestEntities.OrganizationOrsta.Id}",
                null,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AssignmaentWithAssignmentPackageDto>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestEntities.OrganizationVerdiqAS.Id, result.FromId);
            Assert.Equal(TestEntities.OrganizationOrsta.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
            Assert.Contains(result.AssignmentPackages, p => p.PackageId == PackageConstants.KonkursboAdministrator.Id);
        }

        /// <summary>
        /// Adds a person administrator that has no existing relationship to the party by
        /// posting a <see cref="PersonInput"/> body (SSN + last name). The mock
        /// <see cref="UserProfileLookupServiceMock"/> resolves the person by SSN and
        /// matching last name; expects OK with the created assignment and admin package.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_ForPersonViaPersonInput_Returns200WithAdminPackage()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "Farmor" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"{Route}/users/administrators?party={TestEntities.OrganizationVerdiqAS.Id}",
                content,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AssignmaentWithAssignmentPackageDto>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestEntities.OrganizationVerdiqAS.Id, result.FromId);
            Assert.Equal(TestData.BodilFarmor.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
            Assert.Contains(result.AssignmentPackages, p => p.PackageId == PackageConstants.KonkursboAdministrator.Id);
        }

        /// <summary>
        /// Adds a person administrator via <see cref="PersonInput"/> where the supplied last name
        /// does not match the resolved person. The mock returns null and validation fails;
        /// expects 400 BadRequest with an InvalidExternalIdentifiers validation error.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_ForPersonViaPersonInputWithWrongLastName_Returns400()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "WrongName" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"{Route}/users/administrators?party={TestEntities.OrganizationVerdiqAS.Id}",
                content,
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Single(result.Errors);
            Assert.Equal("AM.VLD-00034", result.Errors.First().ErrorCode.ToString());
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v1/enduser/bankruptcyestate/users/administrators

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.RevokeAdministrator(Guid, Guid, CancellationToken)"/>.
    /// </summary>
    [IntegrationTest]
    public class RevokeAdministrator : IClassFixture<ApiFixture>
    {
        public RevokeAdministrator(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RevokeAdministrator>(db =>
            {
                var adminAssignment = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.OrganizationOkernBorettslag.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(adminAssignment);
                db.AssignmentPackages.Add(new AssignmentPackage()
                {
                    AssignmentId = adminAssignment.Id,
                    PackageId = PackageConstants.KonkursboAdministrator.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task RevokeAdministrator_ForExistingAdministrator_Returns204()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.DeleteAsync(
                $"{Route}/users/administrators?party={TestEntities.OrganizationVerdiqAS.Id}&user={TestEntities.OrganizationOkernBorettslag.Id}",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }
    }

    #endregion

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/estates

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.GetBankruptcyEstatesForParty(Guid, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// party = <see cref="TestEntities.OrganizationVerdiqAS"/> is EstateAdministrator for the
    /// estate <see cref="TestEntities.OrganizationSolsidenSameie"/>.
    /// </remarks>
    [IntegrationTest]
    public class GetBankruptcyEstatesForParty : IClassFixture<ApiFixture>
    {
        public GetBankruptcyEstatesForParty(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetBankruptcyEstatesForParty>(db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task GetBankruptcyEstatesForParty_Authorized_Returns200WithEstates()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.GetAsync(
                $"{Route}/estates?party={TestEntities.OrganizationVerdiqAS.Id}",
                TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<CompactEntityDto>>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Contains(result.Items, x => x.Id == TestEntities.OrganizationSolsidenSameie.Id);
        }
    }

    #endregion

    #region POST/DELETE accessmanagement/api/v1/enduser/bankruptcyestate/estates/users

    /// <summary>
    /// Tests for <see cref="BanckruptcyDelegationController.AddBankruptcyEstateForUser(Guid, Guid, Guid, CancellationToken)"/>
    /// and <see cref="BanckruptcyDelegationController.RevokeBankruptcyEstateForUser(Guid, Guid, Guid, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// party (bankruptcy administrator) = <see cref="TestEntities.OrganizationVerdiqAS"/> is
    /// EstateAdministrator for the estate <see cref="TestEntities.OrganizationSolsidenSameie"/>.
    /// <see cref="TestEntities.PersonPaula"/> is an Agent for the party. Delegating the estate to
    /// the agent (user) therefore succeeds. The estate <see cref="TestEntities.OrganizationOkernBorettslag"/>
    /// is not administrated by the party, so delegating it must fail.
    /// </remarks>
    [IntegrationTest]
    public class AddRevokeBankruptcyEstateForUser : IClassFixture<ApiFixture>
    {
        public AddRevokeBankruptcyEstateForUser(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<AddRevokeBankruptcyEstateForUser>(db =>
            {
                // party is EstateAdministrator for the estate
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationSolsidenSameie.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.EstateAdministrator,
                });

                // Paula is an Agent for the party
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula.Id,
                    RoleId = RoleConstants.Agent,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// The party is EstateAdministrator for the estate and the user is an Agent of the party,
        /// so delegating the estate to the user succeeds. Revoking it afterwards also succeeds.
        /// </summary>
        [Fact]
        public async Task AddAndRevokeBankruptcyEstateForUser_WhenPartyAdministratesEstate_ReturnsOk()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var addResponse = await client.PostAsync(
                $"{Route}/estates/users?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&user={TestEntities.PersonPaula.Id}",
                null,
                TestContext.Current.CancellationToken);

            var addContent = await addResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(addResponse.StatusCode == HttpStatusCode.OK, $"Expected OK but got {addResponse.StatusCode}. Response body: {addContent}");

            var addResult = JsonSerializer.Deserialize<CreateDelegationResponseDto>(
                addContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(addResult);
            Assert.NotEqual(Guid.Empty, addResult.DelegationId);

            var revokeResponse = await client.DeleteAsync(
                $"{Route}/estates/users?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationSolsidenSameie.Id}&user={TestEntities.PersonPaula.Id}",
                TestContext.Current.CancellationToken);

            var revokeContent = await revokeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(revokeResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {revokeResponse.StatusCode}. Response body: {revokeContent}");
        }

        /// <summary>
        /// The estate is not administrated by the party (no EstateAdministrator assignment),
        /// so <see cref="IBankruptcyDelegationService.CheckBankruptcyEstateConnection"/> fails and
        /// the controller returns 403 Forbidden for both add and revoke.
        /// </summary>
        [Fact]
        public async Task AddAndRevokeBankruptcyEstateForUser_WhenEstateNotAdministratedByParty_ReturnsForbidden()
        {
            var client = CreateClient(Fixture, TestEntities.OrganizationVerdiqAS.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var addResponse = await client.PostAsync(
                $"{Route}/estates/users?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationOkernBorettslag.Id}&user={TestEntities.PersonPaula.Id}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, addResponse.StatusCode);

            var revokeResponse = await client.DeleteAsync(
                $"{Route}/estates/users?party={TestEntities.OrganizationVerdiqAS.Id}&estate={TestEntities.OrganizationOkernBorettslag.Id}&user={TestEntities.PersonPaula.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, revokeResponse.StatusCode);
        }
    }

    #endregion
}
