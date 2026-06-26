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
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests for <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.AuthorizedPartiesController"/>
/// </summary>
[IntegrationTest]
public class AuthorizedPartiesControllerTest : IClassFixture<ApiFixture>
{
    public const string Route = "accessmanagement/api/v1/enduser/authorizedparties";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthorizedPartiesControllerTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IProfileClient, ProfileClientMock>();
            services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
        });
    }

    public ApiFixture Fixture { get; }

    /// <summary>
    /// Creates an HTTP client authenticated as the given user with portal enduser scope.
    /// </summary>
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

    /// <summary>
    /// Creates an HTTP client with only the specified scopes and no user identity claims.
    /// </summary>
    private HttpClient CreateClientWithScopes(params string[] scopes)
    {
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("scope", string.Join(" ", scopes)));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    /// <summary>
    /// Malin (Managing Director of Dumbo Adventures) requests authorized parties with portal scope.
    /// Expects 200 OK.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsManagingDirectorWithPortalScope_Returns200WithAuthorizedParties()
    {
        var client = CreatePortalClient(TestData.MalinEmilie);

        var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    /// <summary>
    /// Thea (Rightholder of Dumbo Adventures, MD of Mille Hundefrisør) requests authorized parties with portal scope.
    /// Expects 200 OK.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsRightholderWithPortalScope_Returns200WithAuthorizedParties()
    {
        var client = CreatePortalClient(TestData.Thea);

        var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    /// <summary>
    /// Malin requests authorized parties with includeRoles=true.
    /// Expects 200 OK and Dumbo Adventures in the result.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsManagingDirectorWithIncludeRoles_Returns200WithRepresentedOrgAndSelfWithRoles()
    {
        HttpClient client = CreatePortalClient(TestData.MalinEmilie);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeRoles=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto dumbo = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
        Assert.NotNull(dumbo);
        Assert.Equal("Dumbo Adventures AS", dumbo.Name);
        AssertHasDaglRoles(dumbo);
        Assert.Empty(dumbo.AuthorizedAccessPackages);
        Assert.Empty(dumbo.AuthorizedResources);

        AuthorizedPartyDto malinSelf = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.MalinEmilie.Id);
        Assert.NotNull(malinSelf);
        Assert.Equal("Malin Emilie", malinSelf.Name);
        AssertHasPrivRoles(malinSelf);
        Assert.Empty(malinSelf.AuthorizedAccessPackages);
        Assert.Empty(malinSelf.AuthorizedResources);
    }

    /// <summary>
    /// Thea requests authorized parties with includeRoles=true.
    /// Expects self (PRIV), Mille Hundefrisør (DAGL), and Dumbo Adventures (via Rightholder assignment) in the result.
    /// Packages and resources should be empty since only roles are requested.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsRightholderWithIncludeRoles_Returns200WithRepresentedOrgsWithRoles()
    {
        HttpClient client = CreatePortalClient(TestData.Thea);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeRoles=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto theaSelf = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.Thea.Id);
        Assert.NotNull(theaSelf);
        Assert.Equal("Thea BFF", theaSelf.Name);
        AssertHasPrivRoles(theaSelf);
        Assert.Empty(theaSelf.AuthorizedAccessPackages);
        Assert.Empty(theaSelf.AuthorizedResources);

        AuthorizedPartyDto mille = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.MilleHundefrisor.Id);
        Assert.NotNull(mille);
        Assert.Equal("Mille Hundefrisør", mille.Name);
        AssertHasDaglRoles(mille);
        Assert.Empty(mille.AuthorizedAccessPackages);
        Assert.Empty(mille.AuthorizedResources);

        AuthorizedPartyDto dumbo = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
        Assert.NotNull(dumbo);
        Assert.Equal("Dumbo Adventures AS", dumbo.Name);
        Assert.Empty(dumbo.AuthorizedRoles);
        Assert.Empty(dumbo.AuthorizedAccessPackages);
        Assert.Empty(dumbo.AuthorizedResources);
    }

    /// <summary>
    /// Malin requests authorized parties with includeAccessPackages=true.
    /// Dumbo should have DAGL packages (Malin is MD), self should have PRIV packages.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsManagingDirectorWithIncludeAccessPackages_Returns200WithRepresentedOrgAndSelfWithPackages()
    {
        HttpClient client = CreatePortalClient(TestData.MalinEmilie);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeAccessPackages=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto dumbo = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
        Assert.NotNull(dumbo);
        AssertHasDaglPackages(dumbo);

        AuthorizedPartyDto malinSelf = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.MalinEmilie.Id);
        Assert.NotNull(malinSelf);
        AssertHasPrivPackages(malinSelf);
    }

    /// <summary>
    /// Thea requests authorized parties with includeAccessPackages=true.
    /// Mille should have DAGL packages (Thea is MD), self should have PRIV packages.
    /// Dumbo should have the SalarySpecialCategory package via Thea's Rightholder assignment.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsRightholderWithIncludeAccessPackages_Returns200WithRepresentedOrgsWithPackages()
    {
        HttpClient client = CreatePortalClient(TestData.Thea);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeAccessPackages=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto theaSelf = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.Thea.Id);
        Assert.NotNull(theaSelf);
        AssertHasPrivPackages(theaSelf);

        AuthorizedPartyDto mille = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.MilleHundefrisor.Id);
        Assert.NotNull(mille);
        AssertHasDaglPackages(mille);

        AuthorizedPartyDto dumbo = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
        Assert.NotNull(dumbo);
        Assert.Contains("lonn-personopplysninger-saerlig-kategori", dumbo.AuthorizedAccessPackages);
    }

    /// <summary>
    /// Malin requests authorized parties with multiple include flags.
    /// Expects 200 OK.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsManagingDirectorWithMultipleIncludeFlags_Returns200WithAuthorizedParties()
    {
        var client = CreatePortalClient(TestData.MalinEmilie);

        var response = await client.GetAsync($"{Route}?includeRoles=true&includeResources=true&includeInstances=true", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    /// <summary>
    /// Request with a wrong scope (connections scope instead of portal/authorizedparties scope).
    /// Expects 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_WithWrongScope_Returns403Forbidden()
    {
        var client = CreateClientWithScopes(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Request without any authentication token.
    /// Expects 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_WithNoToken_Returns401Unauthorized()
    {
        var client = Fixture.Server.CreateClient();

        var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Malin requests authorized parties with the system scope (SCOPE_AUTHORIZEDPARTIES_ENDUSERSYSTEM).
    /// Expects 200 OK since the policy accepts both portal and system scopes.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsManagingDirectorWithSystemScope_Returns200WithAuthorizedParties()
    {
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.UserId, TestData.MalinEmilie.Entity.UserId.ToString()));
            claims.Add(new Claim("scope", AuthzConstants.SCOPE_AUTHORIZEDPARTIES_ENDUSERSYSTEM));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    /// <summary>
    /// Thea requests authorized parties and verifies the response deserializes correctly.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_Returns200WithPaginatedResult()
    {
        HttpClient client = CreatePortalClient(TestData.Thea);

        HttpResponseMessage response = await client.GetAsync(Route, TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Alex (ChairOfTheBoard of Kaos Magic Design and Arts) requests authorized parties with includeRoles and includeAccessPackages.
    /// Expects self (PRIV), Kaos (LEDE), and Dumbo Adventures (REVI via key role in Kaos, which is auditor for Dumbo).
    /// Validates roles and packages for all parties.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsChairOfTheBoardWithKeyRoles_Returns200WithRepresentedOrgAndAuditedOrgViaKeyRole()
    {
        HttpClient client = CreatePortalClient(TestData.AlexTheArtist);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeRoles=true&includeAccessPackages=true&includePartiesViaKeyRoles=True", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto alexSelf = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.AlexTheArtist.Id);
        Assert.NotNull(alexSelf);
        Assert.Equal("Alex The Artist", alexSelf.Name);
        AssertHasPrivRoles(alexSelf);
        AssertHasPrivPackages(alexSelf);

        AuthorizedPartyDto kaos = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
        Assert.NotNull(kaos);
        Assert.Equal("Kaos Magic Design and Arts", kaos.Name);
        AssertHasLedeRoles(kaos);
        AssertHasLedePackages(kaos);

        AuthorizedPartyDto dumbo = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
        Assert.NotNull(dumbo);
        Assert.Equal("Dumbo Adventures AS", dumbo.Name);
        AssertHasReviRoles(dumbo);
        AssertHasReviPackages(dumbo);
    }

    /// <summary>
    /// Josephine requests authorized parties with includeInstances=true.
    /// Expects Kaos Magic Design and Arts to appear with instance rights for SiriusSkattemelding and MattilsynetBakeryService.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsInstanceRecipientWithIncludeInstances_Returns200WithDelegatingOrgWithInstances()
    {
        HttpClient client = CreatePortalClient(TestData.JosephineYvonnesdottir);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeInstances=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto kaos = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
        Assert.NotNull(kaos);
        Assert.Equal("Kaos Magic Design and Arts", kaos.Name);
        Assert.True(kaos.AuthorizedInstances.Count >= 2, $"Expected at least 2 instances but got {kaos.AuthorizedInstances.Count}. Response body: {content}");
        Assert.Contains(kaos.AuthorizedInstances, i => i.ResourceId == "app_skd_sirius-skattemelding-v1" && i.InstanceId == "b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
        Assert.Contains(kaos.AuthorizedInstances, i => i.ResourceId == "app_mat_mattilsynet-baker-konditorvare" && i.InstanceId == "a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e");
    }

    /// <summary>
    /// Josephine requests authorized parties without includeInstances.
    /// Expects Kaos to appear but with empty instances list.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsInstanceRecipientWithoutIncludeInstances_Returns200WithDelegatingOrgWithoutInstances()
    {
        HttpClient client = CreatePortalClient(TestData.JosephineYvonnesdottir);

        HttpResponseMessage response = await client.GetAsync(Route, TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto kaos = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
        Assert.NotNull(kaos);
        Assert.Empty(kaos.AuthorizedInstances);
    }

    /// <summary>
    /// Josephine requests authorized parties with includeResources=true.
    /// Kaos should appear with AuthorizedResources populated from the instance delegations
    /// AuthorizedInstances should be empty since includeInstances is not set.
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsInstanceRecipientWithIncludeResources_Returns200WithDelegatingOrgWithResources()
    {
        HttpClient client = CreatePortalClient(TestData.JosephineYvonnesdottir);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeResources=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto kaos = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
        Assert.NotNull(kaos);
        Assert.Contains("app_skd_sirius-skattemelding-v1", kaos.AuthorizedResources);
        Assert.Contains("app_mat_mattilsynet-baker-konditorvare", kaos.AuthorizedResources);
        Assert.Empty(kaos.AuthorizedInstances);
    }

    /// <summary>
    /// Josephine requests authorized parties with both includeResources and includeInstances.
    /// Kaos should have both resources and instances populated (from seed).
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsInstanceRecipientWithIncludeResourcesAndInstances_Returns200WithDelegatingOrgWithResourcesAndInstances()
    {
        HttpClient client = CreatePortalClient(TestData.JosephineYvonnesdottir);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeResources=true&includeInstances=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        AuthorizedPartyDto kaos = result.Items.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
        Assert.NotNull(kaos);
        Assert.True(kaos.AuthorizedResources.Count >= 2, $"Expected at least 2 resources but got {kaos.AuthorizedResources.Count}. Response: {content}");
        Assert.Contains("app_skd_sirius-skattemelding-v1", kaos.AuthorizedResources);
        Assert.Contains("app_mat_mattilsynet-baker-konditorvare", kaos.AuthorizedResources);
        Assert.True(kaos.AuthorizedInstances.Count >= 2, $"Expected at least 2 instances but got {kaos.AuthorizedInstances.Count}. Response: {content}");
        Assert.Contains(kaos.AuthorizedInstances, i => i.ResourceId == "app_skd_sirius-skattemelding-v1");
        Assert.Contains(kaos.AuthorizedInstances, i => i.ResourceId == "app_mat_mattilsynet-baker-konditorvare");
    }

    /// <summary>
    /// Paula is ManagingDirector of both the Karlstad main unit and its subunit. The authorized-parties
    /// response returns the main unit at the top level with the subunit nested under Subunits, never as a
    /// separate top-level party (no duplicate party UUIDs). The subunit exposes at least the main unit's
    /// roles and access packages. Guards the hovedenhet/underenhet response contract (#3498 area 5).
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsMainUnitManagingDirector_Returns200WithSubunitNestedUnderMainUnitWithoutDuplicates()
    {
        HttpClient client = CreatePortalClient(TestEntities.PersonPaula);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeRoles=true&includeAccessPackages=true&includeInstances=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);

        // The main unit is a top-level party; the subunit is nested, never a separate top-level entry.
        AuthorizedPartyDto mainUnit = result.Items.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        Assert.DoesNotContain(result.Items, p => p.PartyUuid == TestEntities.SubunitKarlstad.Id);

        AuthorizedPartyDto subUnit = mainUnit.Subunits.FirstOrDefault(p => p.PartyUuid == TestEntities.SubunitKarlstad.Id);
        Assert.NotNull(subUnit);

        // The subunit carries at least the main unit's roles and access packages (hovedenhet/underenhet contract).
        Assert.NotEmpty(mainUnit.AuthorizedRoles);
        Assert.NotEmpty(mainUnit.AuthorizedAccessPackages);
        Assert.All(mainUnit.AuthorizedRoles, role => Assert.Contains(role, subUnit.AuthorizedRoles));
        Assert.All(mainUnit.AuthorizedAccessPackages, pkg => Assert.Contains(pkg, subUnit.AuthorizedAccessPackages));

        // No party UUID appears more than once across the main units and their nested subunits.
        List<Guid> allPartyUuids = result.Items
            .SelectMany(p => p.Subunits.Select(s => s.PartyUuid).Append(p.PartyUuid))
            .ToList();
        Assert.Equal(allPartyUuids.Count, allPartyUuids.Distinct().Count());
    }

    /// <summary>
    /// A partyFilter on the subunit returns the main unit at the top level with the subunit nested under
    /// it (the subunit is reachable only through the main unit's hierarchy, not as a top-level party).
    /// Guards the subunit partyFilter contract (#3498 area 5).
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_WithSubunitPartyFilter_Returns200WithMainUnitWithSubunitNested()
    {
        HttpClient client = CreatePortalClient(TestEntities.PersonPaula);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeRoles=true&partyFilter={TestEntities.SubunitKarlstad.Id}", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);

        AuthorizedPartyDto mainUnit = result.Items.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        Assert.Contains(mainUnit.Subunits, s => s.PartyUuid == TestEntities.SubunitKarlstad.Id);
        Assert.DoesNotContain(result.Items, p => p.PartyUuid == TestEntities.SubunitKarlstad.Id);
    }

    /// <summary>
    /// Paula holds an instance delegation on the Karlstad main unit. The authorized-parties response
    /// surfaces that instance on the main unit but must not inherit it onto the nested subunit, even
    /// though roles and access packages are inherited. Guards the instance-exclusion half of the
    /// hovedenhet/underenhet contract (#3498 area 5).
    /// </summary>
    [Fact]
    public async Task GetAuthorizedParties_AsMainUnitManagingDirector_Returns200WithSubunitWithoutMainUnitInstances()
    {
        HttpClient client = CreatePortalClient(TestEntities.PersonPaula);

        HttpResponseMessage response = await client.GetAsync($"{Route}?includeRoles=true&includeAccessPackages=true&includeInstances=true", TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

        PaginatedResult<AuthorizedPartyDto> result = JsonSerializer.Deserialize<PaginatedResult<AuthorizedPartyDto>>(content, JsonOptions);
        Assert.NotNull(result);

        AuthorizedPartyDto mainUnit = result.Items.FirstOrDefault(p => p.PartyUuid == TestEntities.MainUnitKarlstad.Id);
        Assert.NotNull(mainUnit);
        AuthorizedPartyDto subUnit = mainUnit.Subunits.FirstOrDefault(p => p.PartyUuid == TestEntities.SubunitKarlstad.Id);
        Assert.NotNull(subUnit);

        // The instance is delegated on the main unit, so it surfaces there.
        Assert.Contains(mainUnit.AuthorizedInstances, i => i.InstanceId == "c3d4e5f6-a7b8-4c9d-8e0f-1a2b3c4d5e6f");

        // The subunit inherits the main unit's roles, but not its authorized instances.
        Assert.NotEmpty(subUnit.AuthorizedRoles);
        Assert.Empty(subUnit.AuthorizedInstances);
    }

    /// <summary>
    /// Asserts that the authorized party has the expected DAGL (ManagingDirector) role and its subroles.
    /// Codes come from RoleConstants via the ConnectionQuery role map expansion.
    /// </summary>
    private static void AssertHasDaglRoles(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedRoles.Count >= 25, $"Expected at least 25 roles for DAGL, but got {party.AuthorizedRoles.Count}");
        Assert.Contains("daglig-leder", party.AuthorizedRoles);
        Assert.Contains("dagl", party.AuthorizedRoles);
        Assert.Contains("utinn", party.AuthorizedRoles);
        Assert.Contains("siskd", party.AuthorizedRoles);
        Assert.Contains("regna", party.AuthorizedRoles);
        Assert.Contains("apiadm", party.AuthorizedRoles);
        Assert.Contains("eckeyrole", party.AuthorizedRoles);
        Assert.Contains("hadm", party.AuthorizedRoles);
        Assert.Contains("signe", party.AuthorizedRoles);
    }

    /// <summary>
    /// Asserts that the authorized party has the expected LEDE (ChairOfTheBoard) role and its subroles.
    /// Codes come from RoleConstants via the ConnectionQuery role map expansion.
    /// </summary>
    private static void AssertHasLedeRoles(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedRoles.Count >= 23, $"Expected at least 23 roles for LEDE, but got {party.AuthorizedRoles.Count}");
        Assert.Contains("styreleder", party.AuthorizedRoles);
        Assert.Contains("lede", party.AuthorizedRoles);
        Assert.Contains("utinn", party.AuthorizedRoles);
        Assert.Contains("siskd", party.AuthorizedRoles);
        Assert.Contains("regna", party.AuthorizedRoles);
        Assert.Contains("apiadm", party.AuthorizedRoles);
        Assert.Contains("eckeyrole", party.AuthorizedRoles);
        Assert.Contains("hadm", party.AuthorizedRoles);
        Assert.Contains("signe", party.AuthorizedRoles);
    }

    /// <summary>
    /// Asserts that the authorized party has the expected PRIV (PrivatePerson) role and its subroles.
    /// Codes come from RoleConstants via the ConnectionQuery role map expansion.
    /// </summary>
    private static void AssertHasPrivRoles(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedRoles.Count >= 16, $"Expected at least 16 roles for PRIV, but got {party.AuthorizedRoles.Count}");
        Assert.Contains("privatperson", party.AuthorizedRoles);
        Assert.Contains("priv", party.AuthorizedRoles);
        Assert.Contains("utinn", party.AuthorizedRoles);
        Assert.Contains("siskd", party.AuthorizedRoles);
        Assert.Contains("regna", party.AuthorizedRoles);
        Assert.Contains("priut", party.AuthorizedRoles);
        Assert.Contains("boadm", party.AuthorizedRoles);
        Assert.Contains("admai", party.AuthorizedRoles);
        Assert.Contains("komab", party.AuthorizedRoles);
    }

    /// <summary>
    /// Asserts that the authorized party has the expected REVI (Auditor) role and its subroles.
    /// Codes come from RoleConstants via the ConnectionQuery role map expansion.
    /// </summary>
    private static void AssertHasReviRoles(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedRoles.Count >= 3, $"Expected at least 3 roles for REVI, but got {party.AuthorizedRoles.Count}");
        Assert.Contains("revisor", party.AuthorizedRoles);
        Assert.Contains("revi", party.AuthorizedRoles);
        Assert.Contains("a0237", party.AuthorizedRoles);
        Assert.Contains("a0238", party.AuthorizedRoles);
    }

    /// <summary>
    /// Asserts that the authorized party has access packages expected for the LEDE (ChairOfTheBoard) role.
    /// Based on IngestRolePackage.cs role-to-package mappings for ChairOfTheBoard.
    /// </summary>
    private static void AssertHasLedePackages(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedAccessPackages.Count >= 80, $"Expected at least 80 packages for LEDE, but got {party.AuthorizedAccessPackages.Count}");
        Assert.Contains("lonn", party.AuthorizedAccessPackages);
        Assert.Contains("skatt-naering", party.AuthorizedAccessPackages);
        Assert.Contains("merverdiavgift", party.AuthorizedAccessPackages);
        Assert.Contains("ansettelsesforhold", party.AuthorizedAccessPackages);
        Assert.Contains("toll", party.AuthorizedAccessPackages);
        Assert.Contains("regnskap-okonomi-rapport", party.AuthorizedAccessPackages);
        Assert.Contains("dokumentbasert-tilsyn", party.AuthorizedAccessPackages);
        Assert.Contains("maskinporten-scopes", party.AuthorizedAccessPackages);
    }

    /// <summary>
    /// Asserts that the authorized party has access packages expected for the REVI (Auditor) role.
    /// Based on IngestRolePackage.cs role-to-package mappings for Auditor.
    /// </summary>
    private static void AssertHasReviPackages(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedAccessPackages.Count >= 2, $"Expected at least 2 packages for REVI, but got {party.AuthorizedAccessPackages.Count}");
        Assert.Contains("ansvarlig-revisor", party.AuthorizedAccessPackages);
        Assert.Contains("revisormedarbeider", party.AuthorizedAccessPackages);
    }

    /// <summary>
    /// Asserts that the authorized party has access packages expected for the DAGL role.
    /// Based on IngestRolePackage.cs role-to-package mappings for ManagingDirector.
    /// </summary>
    private static void AssertHasDaglPackages(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedAccessPackages.Count >= 80, $"Expected at least 80 packages for DAGL, but got {party.AuthorizedAccessPackages.Count}");
        Assert.Contains("lonn", party.AuthorizedAccessPackages);
        Assert.Contains("skatt-naering", party.AuthorizedAccessPackages);
        Assert.Contains("merverdiavgift", party.AuthorizedAccessPackages);
        Assert.Contains("ansettelsesforhold", party.AuthorizedAccessPackages);
        Assert.Contains("toll", party.AuthorizedAccessPackages);
        Assert.Contains("regnskap-okonomi-rapport", party.AuthorizedAccessPackages);
        Assert.Contains("dokumentbasert-tilsyn", party.AuthorizedAccessPackages);
        Assert.Contains("maskinporten-scopes", party.AuthorizedAccessPackages);
    }

    /// <summary>
    /// Asserts that the authorized party has access packages expected for the PRIV role.
    /// Based on IngestRolePackage.cs role-to-package mappings for PrivatePerson.
    /// </summary>
    private static void AssertHasPrivPackages(AuthorizedPartyDto party)
    {
        Assert.True(party.AuthorizedAccessPackages.Count >= 35, $"Expected at least 35 packages for PRIV, but got {party.AuthorizedAccessPackages.Count}");
        Assert.Contains("innbygger-tilgangsstyring-privatperson", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-arbeidsliv", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-skatteforhold-privatpersoner", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-byggesoknad", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-kjoretoy", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-helsetjenester", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-barn-foreldre", party.AuthorizedAccessPackages);
        Assert.Contains("innbygger-utdanning", party.AuthorizedAccessPackages);
    }
}
