using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests for <see cref="BankruptcyDelegationController"/>.
/// </summary>
/// <remarks>
/// <para>
/// Actors:
/// - Bodil Bostyrer: a lawyer who holds the CCR role <c>bostyrer</c> for two bankruptcy estates (KBO)
/// - Bjarne Bostyrer: a lawyer with no bankruptcy estates
/// - Assistants: persons Bodil already has a rightholder connection to, so they pass the "user" input validation
/// - Ulrik Ukjent: a person with no connection to Bodil
/// </para>
/// <para>
/// Each nested class has its own <see cref="ApiFixture"/> and therefore its own database. Write tests use a
/// separate assistant each, so the order the tests run in does not matter.
/// </para>
/// </remarks>
public class BankruptcyDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/bankruptcyestate";

    public static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Guid LawyerBodil = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000001");
    private static readonly Guid LawyerBjarne = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000002");
    private static readonly Guid ExistingAdmin = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000003");
    private static readonly Guid AssistantAnne = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000004");
    private static readonly Guid AssistantArne = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000005");
    private static readonly Guid AssistantAstrid = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000006");
    private static readonly Guid AssistantAsle = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000007");
    private static readonly Guid AssistantAud = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000008");
    private static readonly Guid UnconnectedUlrik = Guid.Parse("b0a7e5c1-0001-4000-8000-000000000009");
    private static readonly Guid EstateOne = Guid.Parse("b0a7e5c1-0002-4000-8000-000000000001");
    private static readonly Guid EstateTwo = Guid.Parse("b0a7e5c1-0002-4000-8000-000000000002");

    private const string UlrikUsername = "ulrikukjent";

    private static readonly Guid[] AssistantIds = [ExistingAdmin, AssistantAnne, AssistantArne, AssistantAstrid, AssistantAsle, AssistantAud];

    private static readonly Guid[] EstatePackages =
    [
        PackageConstants.MainAdministrator,
        PackageConstants.BankruptcyEstateReadAccess,
        PackageConstants.BankruptcyEstateWriteAccess,
    ];

    /// <summary>
    /// Seeds the lawyers, assistants and estates shared by every nested class: both estates have Bodil as
    /// <c>bostyrer</c>, Bodil has a rightholder connection to every assistant, and <see cref="ExistingAdmin"/>
    /// already holds the bankruptcy estate administrator role from Bodil.
    /// </summary>
    private static void SeedBankruptcyEstates(AppDbContext db)
    {
        db.Entities.AddRange(
            Person(LawyerBodil, "Bodil Bostyrer", "19017700001", 50970001, 20970001),
            Person(LawyerBjarne, "Bjarne Bostyrer", "19017700002", 50970002, 20970002),
            Person(ExistingAdmin, "Eva Eksisterende", "19017700003", 50970003, 20970003),
            Person(AssistantAnne, "Anne Assistent", "19017700004", 50970004, 20970004),
            Person(AssistantArne, "Arne Assistent", "19017700005", 50970005, 20970005),
            Person(AssistantAstrid, "Astrid Assistent", "19017700006", 50970006, 20970006),
            Person(AssistantAsle, "Asle Assistent", "19017700007", 50970007, 20970007),
            Person(AssistantAud, "Aud Assistent", "19017700008", 50970008, 20970008),
            Person(UnconnectedUlrik, "Ulrik Ukjent", "19017700009", 50970009, 20970009, UlrikUsername),
            Estate(EstateOne, "Konkursbo Én", "399970001", 50970101),
            Estate(EstateTwo, "Konkursbo To", "399970002", 50970102));

        db.Assignments.AddRange(
            new Assignment { FromId = EstateOne, ToId = LawyerBodil, RoleId = RoleConstants.EstateAdministrator },
            new Assignment { FromId = EstateTwo, ToId = LawyerBodil, RoleId = RoleConstants.EstateAdministrator },
            new Assignment { FromId = LawyerBodil, ToId = ExistingAdmin, RoleId = RoleConstants.BankruptcyEstateAdministrator });

        foreach (var assistant in AssistantIds)
        {
            db.Assignments.Add(new Assignment { FromId = LawyerBodil, ToId = assistant, RoleId = RoleConstants.Rightholder });
        }

        db.SaveChanges();
    }

    private static Entity Person(Guid id, string name, string ssn, int partyId, int userId, string username = null) => new()
    {
        Username = username,
        Id = id,
        Name = name,
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        PersonIdentifier = ssn,
        RefId = ssn,
        DateOfBirth = new(1977, 1, 19),
        PartyId = partyId,
        UserId = userId,
    };

    private static Entity Estate(Guid id, string name, string orgNo, int partyId) => new()
    {
        Id = id,
        Name = name,
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.KBO,
        OrganizationIdentifier = orgNo,
        RefId = orgNo,
        PartyId = partyId,
    };

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

    private static async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<T>(body, SerializerOptions);
    }

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == expected, $"Expected {expected} but got {response.StatusCode}. Response body: {body}");
    }

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/estates/any

    /// <summary>
    /// <see cref="BankruptcyDelegationController.HasBankruptcyEstates(Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class HasBankruptcyEstates : IClassFixture<ApiFixture>
    {
        public HasBankruptcyEstates(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<HasBankruptcyEstates>(SeedBankruptcyEstates);
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// Bodil is <c>bostyrer</c> for two estates; expects true.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstates_ForLawyerWithEstates_ReturnsTrue()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.GetAsync($"{Route}/estates/any?party={LawyerBodil}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            Assert.True(await Deserialize<bool>(response));
        }

        /// <summary>
        /// Bjarne has no estates; expects false.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstates_ForLawyerWithoutEstates_ReturnsFalse()
        {
            var client = CreateClient(Fixture, LawyerBjarne, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.GetAsync($"{Route}/estates/any?party={LawyerBjarne}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            Assert.False(await Deserialize<bool>(response));
        }

        /// <summary>
        /// The portal scope also grants read access.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstates_WithPortalScope_Returns200()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.GetAsync($"{Route}/estates/any?party={LawyerBodil}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
        }

        /// <summary>
        /// The write scope alone does not grant read access; expects 403.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstates_WithWriteScopeOnly_Returns403()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.GetAsync($"{Route}/estates/any?party={LawyerBodil}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// No token; expects 401.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstates_WithoutToken_Returns401()
        {
            var client = Fixture.Server.CreateClient();

            var response = await client.GetAsync($"{Route}/estates/any?party={LawyerBodil}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    #endregion

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/estates

    /// <summary>
    /// <see cref="BankruptcyDelegationController.GetBankruptcyEstates(Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetBankruptcyEstates : IClassFixture<ApiFixture>
    {
        public GetBankruptcyEstates(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetBankruptcyEstates>(SeedBankruptcyEstates);
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// Bodil gets both her estates back.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstates_ForLawyerWithEstates_ReturnsEstates()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.GetAsync($"{Route}/estates?party={LawyerBodil}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<PaginatedResult<CompactEntityDto>>(response);
            Assert.Equal([EstateOne, EstateTwo], result.Items.Select(e => e.Id).Order());
        }

        /// <summary>
        /// Bjarne has no estates; expects an empty list.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstates_ForLawyerWithoutEstates_ReturnsEmpty()
        {
            var client = CreateClient(Fixture, LawyerBjarne, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.GetAsync($"{Route}/estates?party={LawyerBjarne}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<PaginatedResult<CompactEntityDto>>(response);
            Assert.Empty(result.Items);
        }

        /// <summary>
        /// The write scope alone does not grant read access; expects 403.
        /// </summary>
        [Fact]
        public async Task GetBankruptcyEstates_WithWriteScopeOnly_Returns403()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.GetAsync($"{Route}/estates?party={LawyerBodil}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region GET accessmanagement/api/v1/enduser/bankruptcyestate/administrators

    /// <summary>
    /// <see cref="BankruptcyDelegationController.GetAgentAdminInformation(Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class GetAdministrators : IClassFixture<ApiFixture>
    {
        public GetAdministrators(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<GetAdministrators>(SeedBankruptcyEstates);
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// Bodil has one administrator; plain rightholders are not listed.
        /// </summary>
        [Fact]
        public async Task GetAdministrators_ForLawyerWithAdministrator_ReturnsOnlyAdministrators()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.GetAsync($"{Route}/administrators?party={LawyerBodil}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<PaginatedResult<CompactEntityDto>>(response);
            var admin = Assert.Single(result.Items);
            Assert.Equal(ExistingAdmin, admin.Id);
        }

        /// <summary>
        /// Bjarne has no administrators; expects an empty list.
        /// </summary>
        [Fact]
        public async Task GetAdministrators_ForLawyerWithoutAdministrators_ReturnsEmpty()
        {
            var client = CreateClient(Fixture, LawyerBjarne, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.GetAsync($"{Route}/administrators?party={LawyerBjarne}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<PaginatedResult<CompactEntityDto>>(response);
            Assert.Empty(result.Items);
        }

        /// <summary>
        /// The write scope alone does not grant read access; expects 403.
        /// </summary>
        [Fact]
        public async Task GetAdministrators_WithWriteScopeOnly_Returns403()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.GetAsync($"{Route}/administrators?party={LawyerBodil}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region POST accessmanagement/api/v1/enduser/bankruptcyestate/administrators

    /// <summary>
    /// <see cref="BankruptcyDelegationController.AddBankruptcyEstateAdministrator(Guid, PersonInput?, CancellationToken)"/>
    /// </summary>
    /// <remarks>
    /// The administrator is identified only through the PersonInput body (SSN or username, and last name).
    /// Mocks <see cref="IUserProfileLookupService"/> with <see cref="EstatePersonLookupMock"/>, which knows the
    /// persons seeded here and falls back to <see cref="UserProfileLookupServiceMock"/> for the shared test data.
    /// </remarks>
    [IntegrationTest]
    public class AddAdministrator : IClassFixture<ApiFixture>
    {
        public AddAdministrator(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IUserProfileLookupService, EstatePersonLookupMock>();
            });
            Fixture.EnsureSeedOnce<AddAdministrator>(SeedBankruptcyEstates, db =>
            {
                // Arne already has a rightholder assignment on estate one with one of the three packages.
                var arneOnEstateOne = new Assignment { FromId = EstateOne, ToId = AssistantArne, RoleId = RoleConstants.Rightholder };
                db.Assignments.Add(arneOnEstateOne);
                db.AssignmentPackages.Add(new AssignmentPackage { AssignmentId = arneOnEstateOne.Id, PackageId = PackageConstants.MainAdministrator });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// Bodil makes Anne administrator by SSN and last name. Expects the administrator role from Bodil to Anne,
        /// and a rightholder assignment from each estate to Anne carrying the three bankruptcy estate packages.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_BySsn_Returns200AndDelegatesEstatePackages()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700004", "Assistent"), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<AssignmentDto>(response);
            Assert.Equal(LawyerBodil, result.FromId);
            Assert.Equal(AssistantAnne, result.ToId);
            Assert.Equal(RoleConstants.BankruptcyEstateAdministrator.Id, result.RoleId);

            await AssertEstatePackages(AssistantAnne);
        }

        /// <summary>
        /// Bodil makes Ulrik administrator by username and last name. Ulrik has no prior connection to Bodil, which
        /// the PersonInput lookup does not require.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_ByUsername_Returns200AndDelegatesEstatePackages()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody(UlrikUsername, "Ukjent"), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<AssignmentDto>(response);
            Assert.Equal(UnconnectedUlrik, result.ToId);

            await AssertEstatePackages(UnconnectedUlrik);
        }

        /// <summary>
        /// Arne already has a rightholder assignment on estate one with the main administrator package.
        /// Expects the existing assignment to be reused and only the missing packages added.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_WithExistingEstateAssignment_AddsOnlyMissingPackages()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700005", "Assistent"), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            await AssertEstatePackages(AssistantArne);
        }

        /// <summary>
        /// Bodil adds Astrid twice. The second call must succeed and must not duplicate any assignment or package.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_Twice_IsIdempotent()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var first = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700006", "Assistent"), TestContext.Current.CancellationToken);
            await AssertStatus(HttpStatusCode.OK, first);

            var second = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700006", "Assistent"), TestContext.Current.CancellationToken);
            await AssertStatus(HttpStatusCode.OK, second);

            await AssertEstatePackages(AssistantAstrid);
        }

        /// <summary>
        /// Bodil adds Asle in several simultaneous requests. All must succeed, and Asle must end up with a single
        /// administrator assignment and a single set of packages on each estate.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_Concurrently_AllSucceedWithoutDuplicates()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
                client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700007", "Assistent"), TestContext.Current.CancellationToken)));

            foreach (var response in responses)
            {
                await AssertStatus(HttpStatusCode.OK, response);
            }

            await AssertEstatePackages(AssistantAsle);
        }

        /// <summary>
        /// Bodil adds Bodil Farmor from the shared test data through the PersonInput body.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_ByPersonInput_Returns200()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody(TestData.BodilFarmor.Entity.PersonIdentifier, "Farmor"), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.OK, response);
            var result = await Deserialize<AssignmentDto>(response);
            Assert.Equal(TestData.BodilFarmor.Id, result.ToId);
            Assert.Equal(RoleConstants.BankruptcyEstateAdministrator.Id, result.RoleId);
        }

        /// <summary>
        /// The SSN is right but the last name is wrong; expects 400 and nothing written.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_WithWrongLastName_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700008", "Feilnavn"), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
            var problem = await Deserialize<AltinnValidationProblemDetails>(response);
            Assert.Contains(problem.Errors, e => e.ErrorCode == ValidationErrors.InvalidExternalIdentifiers.ErrorCode);
            await AssertNoAdministratorRole(AssistantAud);
        }

        /// <summary>
        /// An SSN nobody has; expects 400.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_ForUnknownPerson_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017799999", "Ukjent"), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// The administrator must be a person. An organization number does not resolve to a person profile;
        /// expects 400 and nothing written.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_ForOrganization_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);
            var organization = TestEntities.OrganizationVerdiqAS.Entity;

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody(organization.OrganizationIdentifier, organization.Name), TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
            await AssertNoAdministratorRole(TestEntities.OrganizationVerdiqAS.Id);
        }

        /// <summary>
        /// The "user" query parameter is no longer accepted: a connected assistant given only by uuid is not
        /// delegated to; expects 400 and nothing written.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_WithUserUuidOnly_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}&user={AssistantAud}", null, TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
            await AssertNoAdministratorRole(AssistantAud);
        }

        /// <summary>
        /// No PersonInput body; expects 400.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_WithoutPersonInput_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", null, TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// The read scope does not grant write access; expects 403 and nothing written.
        /// </summary>
        [Fact]
        public async Task AddAdministrator_WithReadScope_Returns403()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.PostAsync($"{Route}/administrators?party={LawyerBodil}", PersonBody("19017700008", "Assistent"), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertNoAdministratorRole(AssistantAud);
        }

        private static StringContent PersonBody(string personIdentifier, string lastName)
        {
            var personInput = new PersonInput { PersonIdentifier = personIdentifier, LastName = lastName };
            return new StringContent(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");
        }

        private async Task AssertEstatePackages(Guid user)
        {
            await Fixture.QueryDb(async db =>
            {
                var adminAssignments = await db.Assignments.AsNoTracking()
                    .Where(a => a.FromId == LawyerBodil && a.ToId == user && a.RoleId == RoleConstants.BankruptcyEstateAdministrator)
                    .ToListAsync(TestContext.Current.CancellationToken);
                Assert.Single(adminAssignments);

                foreach (var estate in new[] { EstateOne, EstateTwo })
                {
                    var estateAssignments = await db.Assignments.AsNoTracking()
                        .Where(a => a.FromId == estate && a.ToId == user && a.RoleId == RoleConstants.Rightholder)
                        .ToListAsync(TestContext.Current.CancellationToken);
                    var estateAssignment = Assert.Single(estateAssignments);

                    var packages = await db.AssignmentPackages.AsNoTracking()
                        .Where(p => p.AssignmentId == estateAssignment.Id)
                        .Select(p => p.PackageId)
                        .ToListAsync(TestContext.Current.CancellationToken);
                    Assert.Equal(EstatePackages.Order(), packages.Order());
                }
            });
        }

        private async Task AssertNoAdministratorRole(Guid user)
        {
            await Fixture.QueryDb(async db =>
            {
                var exists = await db.Assignments.AsNoTracking()
                    .AnyAsync(a => a.FromId == LawyerBodil && a.ToId == user && a.RoleId == RoleConstants.BankruptcyEstateAdministrator, TestContext.Current.CancellationToken);
                Assert.False(exists);
            });
        }
    }

    /// <summary>
    /// Profile lookup for <see cref="AddAdministrator"/>: resolves the persons seeded by
    /// <see cref="SeedBankruptcyEstates"/> by SSN or username and last name, and otherwise defers to
    /// <see cref="UserProfileLookupServiceMock"/>.
    /// </summary>
    private sealed class EstatePersonLookupMock : IUserProfileLookupService
    {
        private static readonly (Guid Id, string Ssn, string Username, string LastName, int UserId)[] Persons =
        [
            (AssistantAnne, "19017700004", null, "Assistent", 20970004),
            (AssistantArne, "19017700005", null, "Assistent", 20970005),
            (AssistantAstrid, "19017700006", null, "Assistent", 20970006),
            (AssistantAsle, "19017700007", null, "Assistent", 20970007),
            (AssistantAud, "19017700008", null, "Assistent", 20970008),
            (UnconnectedUlrik, "19017700009", UlrikUsername, "Ukjent", 20970009),
        ];

        private readonly UserProfileLookupServiceMock _fallback = new();

        public Task<NewUserProfile> GetUserProfile(int authnUserId, UserProfileLookup lookupIdentifier, string lastName)
        {
            foreach (var person in Persons)
            {
                var identifierMatches = (lookupIdentifier.Ssn is { } ssn && ssn == person.Ssn)
                    || (lookupIdentifier.Username is { } username && username == person.Username);

                if (identifierMatches && person.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new NewUserProfile { UserId = person.UserId, UserUuid = person.Id, UserName = person.Username });
                }
            }

            return _fallback.GetUserProfile(authnUserId, lookupIdentifier, lastName);
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v1/enduser/bankruptcyestate/administrators

    /// <summary>
    /// <see cref="BankruptcyDelegationController.RevokeBankruptcyEstateAdministrator(Guid, Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
    public class RevokeAdministrator : IClassFixture<ApiFixture>
    {
        public RevokeAdministrator(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<RevokeAdministrator>(SeedBankruptcyEstates, db =>
            {
                // Anne, Arne, Asle and Aud are full administrators: the role from Bodil plus packages on both estates.
                foreach (var assistant in new[] { AssistantAnne, AssistantArne, AssistantAsle, AssistantAud })
                {
                    db.Assignments.Add(new Assignment { FromId = LawyerBodil, ToId = assistant, RoleId = RoleConstants.BankruptcyEstateAdministrator });
                    foreach (var estate in new[] { EstateOne, EstateTwo })
                    {
                        var estateAssignment = new Assignment { FromId = estate, ToId = assistant, RoleId = RoleConstants.Rightholder };
                        db.Assignments.Add(estateAssignment);
                        foreach (var package in EstatePackages)
                        {
                            db.AssignmentPackages.Add(new AssignmentPackage { AssignmentId = estateAssignment.Id, PackageId = package });
                        }
                    }
                }

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// Bodil revokes Anne. Expects 204, and the administrator role and both estate assignments with their packages removed.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_ForAdministrator_Returns204AndRemovesAccess()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.DeleteAsync($"{Route}/administrators?party={LawyerBodil}&user={AssistantAnne}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.NoContent, response);
            await AssertAccessRemoved(AssistantAnne);
        }

        /// <summary>
        /// Bodil revokes Asle in several simultaneous requests. All must return 204 and Asle's access must be removed.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_Concurrently_AllSucceed()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
                client.DeleteAsync($"{Route}/administrators?party={LawyerBodil}&user={AssistantAsle}", TestContext.Current.CancellationToken)));

            foreach (var response in responses)
            {
                await AssertStatus(HttpStatusCode.NoContent, response);
            }

            await AssertAccessRemoved(AssistantAsle);
        }

        /// <summary>
        /// The POST alias <c>administrators/delete</c> behaves like DELETE.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_ViaPostAlias_Returns204AndRemovesAccess()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.PostAsync($"{Route}/administrators/delete?party={LawyerBodil}&user={AssistantArne}", null, TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.NoContent, response);
            await AssertAccessRemoved(AssistantArne);
        }

        /// <summary>
        /// Astrid is not an administrator; revoking is a no-op and returns 204. Her rightholder connection to Bodil is kept.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_ForNonAdministrator_Returns204AndKeepsOtherAssignments()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.DeleteAsync($"{Route}/administrators?party={LawyerBodil}&user={AssistantAstrid}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.NoContent, response);
            await Fixture.QueryDb(async db =>
            {
                var rightholder = await db.Assignments.AsNoTracking()
                    .AnyAsync(a => a.FromId == LawyerBodil && a.ToId == AssistantAstrid && a.RoleId == RoleConstants.Rightholder, TestContext.Current.CancellationToken);
                Assert.True(rightholder);
            });
        }

        /// <summary>
        /// A user that does not exist; expects 400.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_ForUnknownUser_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.DeleteAsync($"{Route}/administrators?party={LawyerBodil}&user={Guid.NewGuid()}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// "user" is required; expects 400.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_WithoutUser_Returns400()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await client.DeleteAsync($"{Route}/administrators?party={LawyerBodil}", TestContext.Current.CancellationToken);

            await AssertStatus(HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// The read scope does not grant write access; expects 403 and Aud keeps her access.
        /// </summary>
        [Fact]
        public async Task RevokeAdministrator_WithReadScope_Returns403()
        {
            var client = CreateClient(Fixture, LawyerBodil, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await client.DeleteAsync($"{Route}/administrators?party={LawyerBodil}&user={AssistantAud}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await Fixture.QueryDb(async db =>
            {
                var admin = await db.Assignments.AsNoTracking()
                    .AnyAsync(a => a.FromId == LawyerBodil && a.ToId == AssistantAud && a.RoleId == RoleConstants.BankruptcyEstateAdministrator, TestContext.Current.CancellationToken);
                Assert.True(admin);
            });
        }

        private async Task AssertAccessRemoved(Guid user)
        {
            await Fixture.QueryDb(async db =>
            {
                var admin = await db.Assignments.AsNoTracking()
                    .AnyAsync(a => a.FromId == LawyerBodil && a.ToId == user && a.RoleId == RoleConstants.BankruptcyEstateAdministrator, TestContext.Current.CancellationToken);
                Assert.False(admin);

                var estateAssignments = await db.Assignments.AsNoTracking()
                    .Where(a => (a.FromId == EstateOne || a.FromId == EstateTwo) && a.ToId == user)
                    .ToListAsync(TestContext.Current.CancellationToken);
                Assert.Empty(estateAssignments);
            });
        }
    }

    /// <summary>
    /// Tests for the methods the ER import (<c>RoleSyncService</c>) calls when an <c>EstateAdministrator</c>
    /// (bostyrer) role is added or removed on an estate.
    /// </summary>
    /// <remarks>
    /// Seeds Bjarne as a second bostyrer on estate two, with Anne as his bankruptcy estate administrator.
    /// </remarks>
    [IntegrationTest]
    public class ImportEstateAdministrator : IClassFixture<ApiFixture>
    {
        public ImportEstateAdministrator(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeedOnce<ImportEstateAdministrator>(SeedBankruptcyEstates, db =>
            {
                db.Assignments.AddRange(
                    new Assignment { FromId = EstateTwo, ToId = LawyerBjarne, RoleId = RoleConstants.EstateAdministrator },
                    new Assignment { FromId = LawyerBjarne, ToId = AssistantAnne, RoleId = RoleConstants.BankruptcyEstateAdministrator });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// Bodil is registered as bostyrer on estate one. Expects her administrator to get a single rightholder
        /// assignment from estate one with the three packages, also when the import runs twice.
        /// </summary>
        [Fact]
        public async Task GrantEstateAccessToAdministrators_Twice_GivesAdministratorsEstatePackagesOnce()
        {
            await Fixture.QueryDb(async db =>
            {
                var service = new BankruptcyEstateDelegationService(db);
                await service.GrantEstateAccessToAdministrators([(EstateOne, LawyerBodil)], TestContext.Current.CancellationToken);
                await service.GrantEstateAccessToAdministrators([(EstateOne, LawyerBodil)], TestContext.Current.CancellationToken);
            });

            await AssertEstatePackages(EstateOne, ExistingAdmin);
        }

        /// <summary>
        /// Bjarne is bostyrer on estate two. Expects the import to grant Anne access when the role is added,
        /// and to remove the rightholder assignment when the role is removed.
        /// </summary>
        [Fact]
        public async Task RevokeEstateAccessFromAdministrators_AfterGrant_RemovesRightholder()
        {
            await Fixture.QueryDb(async db =>
            {
                var service = new BankruptcyEstateDelegationService(db);
                await service.GrantEstateAccessToAdministrators([(EstateTwo, LawyerBjarne)], TestContext.Current.CancellationToken);
            });
            await AssertEstatePackages(EstateTwo, AssistantAnne);

            await Fixture.QueryDb(async db =>
            {
                var service = new BankruptcyEstateDelegationService(db);
                await service.RevokeEstateAccessFromAdministrators([(EstateTwo, LawyerBjarne)], TestContext.Current.CancellationToken);
            });

            await Fixture.QueryDb(async db =>
            {
                var estateAssignments = await db.Assignments.AsNoTracking()
                    .Where(a => a.FromId == EstateTwo && a.ToId == AssistantAnne)
                    .ToListAsync(TestContext.Current.CancellationToken);
                Assert.Empty(estateAssignments);
            });
        }

        /// <summary>
        /// One import batch with several relations: Bjarne on estate one, Bodil on estate two, and a duplicate of
        /// the first. Expects each relation's administrators to get access on its estate, once.
        /// </summary>
        [Fact]
        public async Task GrantEstateAccessToAdministrators_Batch_GrantsEachRelation()
        {
            await Fixture.QueryDb(async db =>
            {
                var service = new BankruptcyEstateDelegationService(db);
                await service.GrantEstateAccessToAdministrators(
                    [(EstateOne, LawyerBjarne), (EstateTwo, LawyerBodil), (EstateOne, LawyerBjarne)],
                    TestContext.Current.CancellationToken);
            });

            await AssertEstatePackages(EstateOne, AssistantAnne);
            await AssertEstatePackages(EstateTwo, ExistingAdmin);
        }

        /// <summary>
        /// Ulrik has no bankruptcy estate administrators. Expects nothing to change.
        /// </summary>
        [Fact]
        public async Task GrantEstateAccessToAdministrators_WithoutAdministrators_ChangesNothing()
        {
            await Fixture.QueryDb(async db =>
            {
                var service = new BankruptcyEstateDelegationService(db);
                var changed = await service.GrantEstateAccessToAdministrators([(EstateOne, UnconnectedUlrik)], TestContext.Current.CancellationToken);
                Assert.Equal(0, changed);
            });
        }

        private async Task AssertEstatePackages(Guid estate, Guid user)
        {
            await Fixture.QueryDb(async db =>
            {
                var estateAssignments = await db.Assignments.AsNoTracking()
                    .Where(a => a.FromId == estate && a.ToId == user && a.RoleId == RoleConstants.Rightholder)
                    .ToListAsync(TestContext.Current.CancellationToken);
                var estateAssignment = Assert.Single(estateAssignments);

                var packages = await db.AssignmentPackages.AsNoTracking()
                    .Where(p => p.AssignmentId == estateAssignment.Id)
                    .Select(p => p.PackageId)
                    .ToListAsync(TestContext.Current.CancellationToken);
                Assert.Equal(EstatePackages.Order(), packages.Order());
            });
        }
    }

    #endregion
}
