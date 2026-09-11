using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.Authorization.Api.Contracts.Authorization;
using Microsoft.Extensions.Configuration;
using TestData = global::Altinn.AccessManagement.Tests.Integration.Services.TestDataSet;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for the GetRolesAndAccessPackages endpoint in PolicyInformationPointController
/// covering ADOS subunit inheritance. Reproduces the reported PDP (PIP) gap where the Daglig-leder
/// of a mainunit did not inherit roles and access packages for its ADOS subunit.
/// The ADOS subunit inheritance feature flag is ENABLED for these tests.
/// </summary>
/// <remarks>
/// Uses a dedicated class fixture (not the shared PolicyInformationPoint collection) so the
/// <see cref="AccessMgmtFeatureFlags.AdosSubunitInheritance"/> feature flag can be enabled for the
/// whole test host without affecting the other PIP test classes.
/// </remarks>
[IntegrationTest]
public class PolicyInformationPointAdosSubunitTest : IClassFixture<AccessMgmtApiFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public PolicyInformationPointAdosSubunitTest(AccessMgmtApiFixture fixture)
    {
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.AdosSubunitInheritance);

        fixture.EnsureSeedOnce<PolicyInformationPointAdosSubunitTest>(db =>
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
    public async Task GetRolesAndAccessPackages_AdosSubunit_AdosInheritanceEnabled_InheritsManagingDirectorRolesAndPackages()
    {
        // AdosPer is Managing Director (DAGL) of the ADOS Mainunit. When ADOS subunit inheritance is
        // enabled, querying the ADOS subunit as reportee (from) must return the inherited ManagingDirector
        // roles and access packages, exactly like BEDR/AAFY subunits.
        var from = TestData.GetEntity("ADOS Subunit").Id;
        var to = TestData.GetEntity("AdosPer").Id;

        var response = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/roles-and-accesspackages?from={from}&to={to}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // ManagingDirector has Urn "urn:altinn:external-role:ccr:daglig-leder" and LegacyUrn "urn:altinn:rolecode:dagl"
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"));
        Assert.Contains(result.Roles, r => r == RoleUrn.Parse("urn:altinn:rolecode:dagl"));

        // RolePackage: ManagingDirector (DAGL) should grant access packages inherited on the ADOS subunit
        Assert.NotEmpty(result.AccessPackages);
    }
}
