using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.Authorization.Api.Contracts.Authorization;
using Microsoft.Extensions.Configuration;
using TestData = global::AccessMgmt.Tests.Services.TestDataSet;

namespace Altinn.AccessManagement.Tests.Controllers;

/// <summary>
/// Integration tests for the GetRolesAndAccessPackages endpoint in PolicyInformationPointController.
/// Reuses test data from <see cref="TestData"/>.
/// </summary>
public class PolicyInformationPointRolesAndAccessPackagesTest : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public PolicyInformationPointRolesAndAccessPackagesTest(ApiFixture fixture)
    {
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));

        fixture.EnsureSeedOnce<PolicyInformationPointRolesAndAccessPackagesTest>(db =>
        {
            db.Entities.AddRange(TestData.Entities);
            db.Assignments.AddRange(TestData.Assignments);
            db.Delegations.AddRange(TestData.Delegations);
            db.AssignmentPackages.AddRange(TestData.AssignmentPackages);
            db.DelegationPackages.AddRange(TestData.DelegationPackages);
            db.SaveChanges();
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Petter_ShouldGetManagingDirectorRoleAndPackages_FromRegnskaperne()
    {
        var from = TestData.GetEntity("Regnskaperne").Id;
        var to = TestData.GetEntity("Petter").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // ManagingDirector has Urn "urn:altinn:external-role:ccr:daglig-leder" and LegacyUrn "urn:altinn:rolecode:dagl"
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:dagl"));

        // RoleMap: ManagingDirector (DAGL) should grant mapped A2 roles
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:hadm"));  // Hovedadministrator
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:utinn")); // Utfyller/Innsender
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:regna")); // Regnskapsmedarbeider
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:admai")); // Tilgangsstyring

        // RolePackage: ManagingDirector (DAGL) should grant access packages
        Assert.NotEmpty(result.AccessPackages);
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:klientadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:tilgangsstyrer"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:hovedadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:maskinporten-administrator"));
    }

    [Fact]
    public async Task Nina_ShouldGetManagingDirectorRoleAndPackages_ButNotRightholder_FromSkrikFrisor()
    {
        var from = TestData.GetEntity("Skrik Frisør").Id;
        var to = TestData.GetEntity("Nina").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // ManagingDirector has Urn "urn:altinn:external-role:ccr:daglig-leder" and LegacyUrn "urn:altinn:rolecode:dagl"
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:dagl"));

        // Rightholder should be excluded by the controller logic
        Assert.DoesNotContain(result.Roles, r => r == RoleUrn.Parse("urn:altinn:role:rettighetshaver"));

        // RoleMap: ManagingDirector (DAGL) should grant mapped A2 roles
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:hadm"));  // Hovedadministrator
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:utinn")); // Utfyller/Innsender
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:regna")); // Regnskapsmedarbeider
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:admai")); // Tilgangsstyring

        // RolePackage: ManagingDirector (DAGL) should grant access packages
        Assert.NotEmpty(result.AccessPackages);
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:klientadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:tilgangsstyrer"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:hovedadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:maskinporten-administrator"));

        // AssignmentPackage: Rightholder assignment should grant AOrderSystem package
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:a-ordning"));
    }

    [Fact]
    public async Task William_ShouldGetManagingDirectorRoleAndPackages_FromRevi()
    {
        var from = TestData.GetEntity("Revi").Id;
        var to = TestData.GetEntity("William").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // ManagingDirector has Urn "urn:altinn:external-role:ccr:daglig-leder" and LegacyUrn "urn:altinn:rolecode:dagl"
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:dagl"));

        // RoleMap: ManagingDirector (DAGL) should grant mapped A2 roles
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:hadm"));  // Hovedadministrator
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:utinn")); // Utfyller/Innsender
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:regna")); // Regnskapsmedarbeider
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:admai")); // Tilgangsstyring

        // RolePackage: ManagingDirector (DAGL) should grant access packages
        Assert.NotEmpty(result.AccessPackages);
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:klientadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:tilgangsstyrer"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:hovedadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:maskinporten-administrator"));
    }

    [Fact]
    public async Task Terje_ShouldGetChairOfTheBoardRole_FromRevi()
    {
        var from = TestData.GetEntity("Revi").Id;
        var to = TestData.GetEntity("Terje").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:external-role:ccr:styreleder"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:lede"));

        // RoleMap: ChairOfTheBoard (LEDE) should grant mapped roles
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:hadm"));  // Hovedadministrator
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:utinn")); // Utfyller/Innsender
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:regna")); // Regnskapsmedarbeider
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:admai")); // Tilgangsstyring

        // RolePackage: ChairOfTheBoard (LEDE) should grant access packages
        Assert.NotEmpty(result.AccessPackages);
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:klientadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:tilgangsstyrer"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:hovedadministrator"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:maskinporten-administrator"));
    }

    [Fact]
    public async Task Gunnar_AgentRole_ShouldBeExcluded_FromRegnskaperne()
    {
        var from = TestData.GetEntity("Regnskaperne").Id;
        var to = TestData.GetEntity("Gunnar").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // Agent role should be excluded by the controller logic
        Assert.DoesNotContain(result.Roles, r => r == RoleUrn.Parse("urn:altinn:role:agent"));
    }

    [Fact]
    public async Task Gunnar_ShouldGetAccountantPackages_FromBakerJohnsen_ViaDelegation()
    {
        // Gunnar is Agent of Regnskaperne, and Regnskaperne delegated Baker Johnsen's Accountant role to Gunnar.
        // The delegation flow is: Baker Johnsen→Regnskaperne (Accountant) → Regnskaperne→Gunnar (Agent).
        // DelegationPackage rows are seeded for the accountant packages on this delegation.
        var from = TestData.GetEntity("Baker Johnsen").Id;
        var to = TestData.GetEntity("Gunnar").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // Delegation connections have no AssignmentId, so no roles are extracted from them
        Assert.Empty(result.Roles);

        // DelegationPackage entries provide accountant access packages through the delegation
        Assert.NotEmpty(result.AccessPackages);
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:regnskapsforer-lonn"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:regnskapsforer-uten-signeringsrettighet"));
    }

    [Fact]
    public async Task Petter_ShouldGetAccountantPackages_FromBakerJohnsen_ViaKeyRole()
    {
        // Petter is ManagingDirector of Regnskaperne, and Baker Johnsen has Regnskaperne as Accountant.
        // Through key role access: Baker Johnsen→Regnskaperne (Accountant) + Regnskaperne→Petter (ManagingDirector)
        var from = TestData.GetEntity("Baker Johnsen").Id;
        var to = TestData.GetEntity("Petter").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // RolePackage: Accountant (REGN) packages should be available through key role access
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:regnskapsforer-lonn"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:regnskapsforer-uten-signeringsrettighet"));

        // Accountant role URNs should also be present
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:external-role:ccr:regnskapsforer"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:regn"));
    }

    [Fact]
    public async Task Terje_ShouldGetPrivatePersonRoleAndInnbyggerPackages_ForHimself()
    {
        // Terje has a PrivatePerson self-assignment (Terje→Terje)
        var from = TestData.GetEntity("Terje").Id;
        var to = TestData.GetEntity("Terje").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // PrivatePerson role should be present
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:role:privatperson"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:priv"));

        // RoleMap: PrivatePerson should grant mapped A2 roles
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:utinn")); // Utfyller/Innsender
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:regna")); // Regnskapsmedarbeider
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:admai")); // Tilgangsstyring

        // RolePackage: PrivatePerson should grant Innbygger access packages
        Assert.NotEmpty(result.AccessPackages);
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:innbygger-skatteforhold-privatpersoner"));
        Assert.Contains(result.AccessPackages, p => p == AccessPackageUrn.Parse("urn:altinn:accesspackage:innbygger-kjoretoy"));
    }

    [Fact]
    public async Task NoConnection_ShouldReturnEmptyResult()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result.Roles);
        Assert.Empty(result.AccessPackages);
    }
}
