using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Shared setup for the <c>includeSubParties</c> and <c>includeInactiveParties</c> tests on
/// <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.AuthorizedPartiesController"/> with the
/// <see cref="AccessMgmtFeatureFlags.AuthorizedPartiesSubAndInactivePartiesFilters"/> feature flag enabled.
/// Paula is ManagingDirector of the Karlstad main unit, of its subunit, and of a deleted organization seeded here.
/// Each derived class has its own fixture, so its profile client decides Paula's profile settings for the whole class
/// (the profile is cached per user in the host).
/// </summary>
public abstract class AuthorizedPartiesSubAndInactivePartiesTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected static readonly Entity DeletedOrganization = new()
    {
        Id = Guid.Parse("a1b2c3d4-0001-0001-0001-0000000000d1"),
        Name = "SLETTET ORGANISASJON AS",
        OrganizationIdentifier = "810418999",
        RefId = "810418999",
        PartyId = 50004299,
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        IsDeleted = true,
        DeletedAt = DateTimeOffset.UtcNow.AddDays(-30),
    };

    protected AuthorizedPartiesSubAndInactivePartiesTestBase(ApiFixture fixture, IProfileClient profileClient)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.AuthorizedPartiesSubAndInactivePartiesFilters);
        Fixture.ConfigureServices(services =>
        {
            services.AddSingleton(profileClient);
            services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
        });
        Fixture.EnsureSeedOnce<AuthorizedPartiesSubAndInactivePartiesTestBase>(db =>
        {
            db.Entities.Add(DeletedOrganization);
            db.SaveChanges();

            db.Assignments.Add(new Assignment()
            {
                FromId = DeletedOrganization.Id,
                ToId = TestEntities.PersonPaula.Id,
                RoleId = RoleConstants.ManagingDirector,
            });
            db.SaveChanges();
        });
    }

    public ApiFixture Fixture { get; }

    private HttpClient CreatePortalClient(ConstantDefinition<Entity> person)
    {
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.UserId, person.Entity.UserId.ToString()));
            claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    protected async Task<List<AuthorizedPartyDto>> GetAuthorizedParties(string query)
    {
        HttpClient client = CreatePortalClient(TestEntities.PersonPaula);

        string url = string.IsNullOrEmpty(query) ? AuthorizedPartiesControllerTest.Route : $"{AuthorizedPartiesControllerTest.Route}?{query}";
        HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);
        return result.Items.ToList();
    }

    protected static void AssertSubunitNested(List<AuthorizedPartyDto> parties)
    {
        AuthorizedPartyDto mainUnit = parties.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        Assert.Contains(mainUnit.Subunits, s => s.PartyUuid == TestEntities.SubunitKarlstad.Id);
    }

    protected static void AssertNoSubunits(List<AuthorizedPartyDto> parties)
    {
        AuthorizedPartyDto mainUnit = parties.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        Assert.Empty(mainUnit.Subunits);
        Assert.DoesNotContain(parties, p => p.PartyUuid == TestEntities.SubunitKarlstad.Id);
    }

    protected static void AssertDeletedPartyIncluded(List<AuthorizedPartyDto> parties)
    {
        AuthorizedPartyDto deleted = parties.FirstOrDefault(p => p.PartyUuid == DeletedOrganization.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    protected static void AssertDeletedPartyExcluded(List<AuthorizedPartyDto> parties)
    {
        Assert.DoesNotContain(parties, p => p.PartyUuid == DeletedOrganization.Id);
        Assert.Contains(parties, p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
    }

    /// <summary>
    /// Returns the same profile for every user, with the given settings for showing subunits and deleted parties.
    /// </summary>
    protected sealed class ProfileSettingsClientStub(bool shouldShowSubEntities, bool shouldShowDeletedEntities) : IProfileClient
    {
        public Task<NewUserProfile> GetUser(UserProfileLookup userProfileLookup, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewUserProfile
            {
                UserId = userProfileLookup.UserId,
                ProfileSettingPreference = new ProfileSettingPreference
                {
                    ShowClientUnits = true,
                    ShouldShowSubEntities = shouldShowSubEntities,
                    ShouldShowDeletedEntities = shouldShowDeletedEntities,
                },
            });
    }
}

/// <summary>
/// Explicit <c>includeSubParties</c> and <c>includeInactiveParties</c> values. Paula has no profile in
/// <see cref="ProfileClientMock"/>, so only the explicit values decide the result.
/// </summary>
[IntegrationTest]
public class AuthorizedPartiesSubAndInactivePartiesControllerTest(ApiFixture fixture)
    : AuthorizedPartiesSubAndInactivePartiesTestBase(fixture, new ProfileClientMock()), IClassFixture<ApiFixture>
{
    /// <summary>
    /// With includeSubParties=false the subunit is left out, even though Paula has direct access to it.
    /// It is neither nested under the main unit nor returned as a top-level party.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_IncludeSubPartiesFalse_Returns200WithoutSubunits()
    {
        var parties = await GetAuthorizedParties("includeSubParties=false&includeInactiveParties=true");

        AuthorizedPartyDto mainUnit = parties.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        Assert.Empty(mainUnit.Subunits);
        Assert.DoesNotContain(parties, p => p.PartyUuid == TestEntities.SubunitKarlstad.Id);
    }

    [Fact]
    public async Task GetAuthorizedParties_IncludeSubPartiesTrue_Returns200WithSubunitNested()
    {
        var parties = await GetAuthorizedParties("includeSubParties=true&includeInactiveParties=true");

        AuthorizedPartyDto mainUnit = parties.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        Assert.Contains(mainUnit.Subunits, s => s.PartyUuid == TestEntities.SubunitKarlstad.Id);
    }

    [Fact]
    public async Task GetAuthorizedParties_IncludeInactivePartiesFalse_Returns200WithoutDeletedParty()
    {
        var parties = await GetAuthorizedParties("includeSubParties=true&includeInactiveParties=false");

        Assert.DoesNotContain(parties, p => p.PartyUuid == DeletedOrganization.Id);
        Assert.Contains(parties, p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
    }

    [Fact]
    public async Task GetAuthorizedParties_IncludeInactivePartiesTrue_Returns200WithDeletedParty()
    {
        var parties = await GetAuthorizedParties("includeSubParties=true&includeInactiveParties=true");

        AuthorizedPartyDto deleted = parties.FirstOrDefault(p => p.PartyUuid == DeletedOrganization.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }
}

/// <summary>
/// The Enduser default (<c>auto</c>) with a profile that hides subunits and deleted parties
/// (ShouldShowSubEntities and ShouldShowDeletedEntities false).
/// </summary>
[IntegrationTest]
public class AuthorizedPartiesProfileHidesSubAndInactivePartiesControllerTest(ApiFixture fixture)
    : AuthorizedPartiesSubAndInactivePartiesTestBase(fixture, new ProfileSettingsClientStub(shouldShowSubEntities: false, shouldShowDeletedEntities: false)), IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetAuthorizedParties_AutoWithProfileHidingSubEntities_Returns200WithoutSubunits()
    {
        var parties = await GetAuthorizedParties(query: null);

        AssertNoSubunits(parties);
    }

    [Fact]
    public async Task GetAuthorizedParties_AutoWithProfileHidingDeletedEntities_Returns200WithoutDeletedParty()
    {
        var parties = await GetAuthorizedParties(query: null);

        AssertDeletedPartyExcluded(parties);
    }

    /// <summary>
    /// An explicit value from the caller takes precedence over the profile setting.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_ExplicitTrueOverridesProfile_Returns200WithSubunitAndDeletedParty()
    {
        var parties = await GetAuthorizedParties("includeSubParties=true&includeInactiveParties=true");

        AssertSubunitNested(parties);
        AssertDeletedPartyIncluded(parties);
    }
}

/// <summary>
/// The Enduser default (<c>auto</c>) with a profile that shows subunits and deleted parties
/// (ShouldShowSubEntities and ShouldShowDeletedEntities true).
/// </summary>
[IntegrationTest]
public class AuthorizedPartiesProfileShowsSubAndInactivePartiesControllerTest(ApiFixture fixture)
    : AuthorizedPartiesSubAndInactivePartiesTestBase(fixture, new ProfileSettingsClientStub(shouldShowSubEntities: true, shouldShowDeletedEntities: true)), IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetAuthorizedParties_AutoWithProfileShowingSubEntities_Returns200WithSubunitNested()
    {
        var parties = await GetAuthorizedParties(query: null);

        AssertSubunitNested(parties);
    }

    [Fact]
    public async Task GetAuthorizedParties_AutoWithProfileShowingDeletedEntities_Returns200WithDeletedParty()
    {
        var parties = await GetAuthorizedParties(query: null);

        AssertDeletedPartyIncluded(parties);
    }

    /// <summary>
    /// An explicit value from the caller takes precedence over the profile setting.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_ExplicitFalseOverridesProfile_Returns200WithoutSubunitAndDeletedParty()
    {
        var parties = await GetAuthorizedParties("includeSubParties=false&includeInactiveParties=false");

        AssertNoSubunits(parties);
        AssertDeletedPartyExcluded(parties);
    }
}
