using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
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
/// Tests for the <c>includeSubParties</c> and <c>includeInactiveParties</c> filters on
/// <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.AuthorizedPartiesController"/> with the
/// <see cref="AccessMgmtFeatureFlags.AuthorizedPartiesSubAndInactivePartiesFilters"/> feature flag enabled.
/// Paula is ManagingDirector of the Karlstad main unit, of its subunit, and of a deleted organization seeded here.
/// </summary>
[IntegrationTest]
public class AuthorizedPartiesSubAndInactivePartiesControllerTest : IClassFixture<ApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Entity DeletedOrganization = new()
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

    public AuthorizedPartiesSubAndInactivePartiesControllerTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.AuthorizedPartiesSubAndInactivePartiesFilters);
        Fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IProfileClient, ProfileClientMock>();
            services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
        });
        Fixture.EnsureSeedOnce<AuthorizedPartiesSubAndInactivePartiesControllerTest>(db =>
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

    private async Task<List<AuthorizedPartyDto>> GetAuthorizedParties(string query)
    {
        HttpClient client = CreatePortalClient(TestEntities.PersonPaula);

        HttpResponseMessage response = await client.GetAsync($"{AuthorizedPartiesControllerTest.Route}?{query}", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);
        return result.Items.ToList();
    }

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
