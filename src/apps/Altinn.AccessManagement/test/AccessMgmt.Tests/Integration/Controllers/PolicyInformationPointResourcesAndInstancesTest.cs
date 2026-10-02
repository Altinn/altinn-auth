using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.Authorization;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for the GetAllDelegationChanges endpoint in PolicyInformationPointController.
/// Tests resource delegations using the database container.
/// </summary>
[IntegrationTest]
[Collection(PolicyInformationPointDbCollection.Name)]
public class PolicyInformationPointResourcesAndInstancesTest
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    // Test entities - persons
    private static readonly Guid PersonAliceId = Guid.Parse("0196b000-0001-7001-8001-000000000001");
    private static readonly Guid PersonBobId = Guid.Parse("0196b000-0001-7001-8001-000000000002");
    private static readonly Guid PersonCharlieId = Guid.Parse("0196b000-0001-7001-8001-000000000003");

    // Test entities - organization
    private static readonly Guid OrgAcmeId = Guid.Parse("0196b000-0001-7001-8001-000000000010");

    // Test assignments
    private static readonly Guid AssignAliceToBobDirect = Guid.Parse("0196b000-0002-7001-8001-000000000001");
    private static readonly Guid AssignAliceToOrgAcme = Guid.Parse("0196b000-0002-7001-8001-000000000002");
    private static readonly Guid AssignOrgAcmeCharlieManagingDirector = Guid.Parse("0196b000-0002-7001-8001-000000000003");

    // Party IDs
    private const int AlicePartyId = 50900001;
    private const int BobPartyId = 50900002;
    private const int BobUserId = 20900002;
    private const int CharliePartyId = 50900003;
    private const int CharlieUserId = 20900003;
    private const int OrgAcmePartyId = 50900010;

    // Main-unit / subunit inheritance test entities
    private static readonly Guid OrgMainUnitId = Guid.Parse("0196b000-0001-7001-8001-000000000050");
    private static readonly Guid OrgSubUnitId = Guid.Parse("0196b000-0001-7001-8001-000000000051");
    private static readonly Guid AssignMainUnitToBobDirect = Guid.Parse("0196b000-0002-7001-8001-000000000050");
    private const int OrgMainUnitPartyId = 50900050;
    private const int OrgSubUnitPartyId = 50900051;

    public PolicyInformationPointResourcesAndInstancesTest(AccessMgmtApiFixture fixture)
    {
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));

        fixture.EnsureSeedOnce<PolicyInformationPointResourcesAndInstancesTest>(db =>
        {
            // Entities
            db.Entities.AddRange(
                new Entity()
                {
                    Id = PersonAliceId,
                    Name = "Alice",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "01019012345",
                    RefId = "01019012345",
                    PartyId = AlicePartyId,
                    UserId = 20900001,
                    DateOfBirth = new DateOnly(1990, 1, 1),
                },
                new Entity()
                {
                    Id = PersonBobId,
                    Name = "Bob",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "02019012345",
                    RefId = "02019012345",
                    PartyId = BobPartyId,
                    UserId = BobUserId,
                    DateOfBirth = new DateOnly(1985, 2, 15),
                },
                new Entity()
                {
                    Id = PersonCharlieId,
                    Name = "Charlie",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "03019012345",
                    RefId = "03019012345",
                    PartyId = CharliePartyId,
                    UserId = CharlieUserId,
                    DateOfBirth = new DateOnly(1980, 5, 20),
                },
                new Entity()
                {
                    Id = OrgAcmeId,
                    Name = "Acme Corp",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900001",
                    RefId = "399900001",
                    PartyId = OrgAcmePartyId,
                },
                new Entity()
                {
                    Id = OrgMainUnitId,
                    Name = "Main Unit AS",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900050",
                    RefId = "399900050",
                    PartyId = OrgMainUnitPartyId,
                },
                new Entity()
                {
                    Id = OrgSubUnitId,
                    Name = "Sub Unit",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.BEDR,
                    OrganizationIdentifier = "399900051",
                    RefId = "399900051",
                    PartyId = OrgSubUnitPartyId,
                    ParentId = OrgMainUnitId,
                });

            // Assignments
            // Alice delegates resource directly to Bob (person-to-person via Rightholder role)
            db.Assignments.Add(new Assignment()
            {
                Id = AssignAliceToBobDirect,
                FromId = PersonAliceId,
                ToId = PersonBobId,
                RoleId = RoleConstants.Rightholder,
            });

            // Alice delegates resource to Acme Corp (person-to-organization)
            db.Assignments.Add(new Assignment()
            {
                Id = AssignAliceToOrgAcme,
                FromId = PersonAliceId,
                ToId = OrgAcmeId,
                RoleId = RoleConstants.Rightholder,
            });

            // Charlie is Managing Director of Acme Corp (keyrole)
            db.Assignments.Add(new Assignment()
            {
                Id = AssignOrgAcmeCharlieManagingDirector,
                FromId = OrgAcmeId,
                ToId = PersonCharlieId,
                RoleId = RoleConstants.ManagingDirector,
            });

            // Main Unit delegates resource directly to Bob (org-to-person). Used to verify main-unit
            // inheritance is still honored for a subunit reportee under DirectAccess.
            db.Assignments.Add(new Assignment()
            {
                Id = AssignMainUnitToBobDirect,
                FromId = OrgMainUnitId,
                ToId = PersonBobId,
                RoleId = RoleConstants.Rightholder,
            });

            // Resource delegations (AssignmentResources)
            // Alice -> Bob: resource delegation on NavSykepengerDialog
            db.AssignmentResources.Add(new AssignmentResource()
            {
                AssignmentId = AssignAliceToBobDirect,
                ResourceId = TestData.NavSykepengerDialog.Id,
                PolicyPath = "nav_sykepenger_dialog/50900001/u20900002/delegationpolicy.xml",
                PolicyVersion = "2024-01-01T00:00:00.0000000Z",
                DelegationChangeId = 99000001,
            });

            // Alice -> Acme Corp: resource delegation on NavSykepengerDialog
            db.AssignmentResources.Add(new AssignmentResource()
            {
                AssignmentId = AssignAliceToOrgAcme,
                ResourceId = TestData.NavSykepengerDialog.Id,
                PolicyPath = "nav_sykepenger_dialog/50900001/p50900010/delegationpolicy.xml",
                PolicyVersion = "2024-01-02T00:00:00.0000000Z",
                DelegationChangeId = 99000002,
            });

            // Main Unit -> Bob: resource delegation on NavSykepengerDialog (offered by the main unit)
            db.AssignmentResources.Add(new AssignmentResource()
            {
                AssignmentId = AssignMainUnitToBobDirect,
                ResourceId = TestData.NavSykepengerDialog.Id,
                PolicyPath = "nav_sykepenger_dialog/50900050/u20900002/delegationpolicy.xml",
                PolicyVersion = "2024-01-03T00:00:00.0000000Z",
                DelegationChangeId = 99000003,
            });

            db.SaveChanges();
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }

    /// <summary>
    /// Test: Resource delegation from person to person.
    /// Alice delegated NavSykepengerDialog to Bob.
    /// When querying delegation changes for Bob (as user) from Alice's party for that resource,
    /// the delegation should be returned.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_ResourceDelegationFromPersonToPerson_Returns200WithDelegationChange()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = BobUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = AlicePartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeDto>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Should contain the direct person-to-person delegation
        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.OfferedByPartyId == AlicePartyId &&
            d.CoveredByUserId == BobUserId);
    }

    /// <summary>
    /// Test: Resource delegation from person to organization, where a person with keyrole
    /// (Managing Director) in the organization inherits the delegated access.
    /// Alice delegated NavSykepengerDialog to Acme Corp.
    /// Charlie is Managing Director of Acme Corp.
    /// When querying delegation changes for Charlie (as user) from Alice's party for that resource,
    /// the delegation to Acme Corp should be returned (inherited via keyrole).
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_ResourceDelegationFromPersonToOrganizationInheritedViaKeyRole_Returns200WithInheritedDelegationChange()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = CharlieUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = AlicePartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeDto>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Should contain the delegation to Acme Corp (inherited by Charlie via ManagingDirector keyrole)
        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.OfferedByPartyId == AlicePartyId &&
            d.CoveredByPartyId == OrgAcmePartyId);
    }

    /// <summary>
    /// Test: DirectAccess authorization context excludes keyrole (org-to-org) inherited access.
    /// Alice delegated NavSykepengerDialog to Acme Corp and Charlie is Managing Director of Acme Corp.
    /// When querying delegation changes for Charlie with AuthContext.DirectAccess, the keyrole-inherited
    /// delegation to Acme Corp must NOT be returned.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_DirectAccess_ExcludesKeyRoleInheritedDelegation_ReturnsEmpty()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = CharlieUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = AlicePartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } },
            authContext = "DirectAccess"
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeDto>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // The keyrole-inherited delegation to Acme Corp must be excluded in DirectAccess mode.
        Assert.DoesNotContain(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.CoveredByPartyId == OrgAcmePartyId);
    }

    /// <summary>
    /// Test: DirectAccess authorization context still returns direct person-to-person delegations.
    /// Alice delegated NavSykepengerDialog directly to Bob.
    /// When querying delegation changes for Bob with AuthContext.DirectAccess, the direct delegation
    /// must still be returned since it is access held directly by the subject.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_DirectAccess_ReturnsDirectPersonToPersonDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = BobUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = AlicePartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } },
            authContext = "DirectAccess"
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeDto>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // The direct person-to-person delegation must still be returned in DirectAccess mode.
        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.OfferedByPartyId == AlicePartyId &&
            d.CoveredByUserId == BobUserId);
    }

    /// <summary>
    /// Test: DirectAccess authorization context still honors main-unit inheritance for a subunit reportee.
    /// Main Unit delegated NavSykepengerDialog directly to Bob, and the query party is the subunit.
    /// When querying delegation changes for Bob with AuthContext.DirectAccess and the subunit as party,
    /// the main-unit delegation must still be returned (main-unit inheritance covers subunits).
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_DirectAccess_ReturnsMainUnitDelegationForSubUnitReportee()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = BobUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgSubUnitPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } },
            authContext = "DirectAccess"
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeDto>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // The main-unit delegation must be returned for the subunit reportee even in DirectAccess mode.
        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.OfferedByPartyId == OrgMainUnitPartyId &&
            d.CoveredByUserId == BobUserId);
    }
}
