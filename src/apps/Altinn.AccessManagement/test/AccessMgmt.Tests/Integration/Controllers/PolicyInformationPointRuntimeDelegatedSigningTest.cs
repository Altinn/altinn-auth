using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Models;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Integration tests verifying that instance delegations are surfaced for a subject user who holds
/// the <c>RuntimeDelegatedSigning</c> access package from an organization or one of its sub-units.
/// </summary>
/// <remarks>
/// The code under test is the <c>representativeFormTasks</c> block inside
/// <c>PolicyInformationPoint.FindAllDelegations</c>. When <c>includeInstanceDelegations</c> is
/// <see langword="true"/> and the subject is identified by <c>userId</c>, the method looks up every
/// assignment where <c>ToId == subject.Id</c> and the associated <c>AssignmentPackage</c> carries
/// the <c>RuntimeDelegatedSigning</c> package. The <c>FromId</c> of those assignments (plus any of
/// their sub-units) is added to the <c>toParties</c> set used when querying instance delegations.
/// </remarks>
[IntegrationTest]
[Collection(PolicyInformationPointDbCollection.Name)]
public class PolicyInformationPointRuntimeDelegatedSigningTest
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    // Subject: Dave (has RuntimeDelegatedSigning from TechCorp)
    private static readonly Guid PersonDaveId = Guid.Parse("0196c000-0001-7001-8001-000000000001");
    private const int DaveUserId = 20930001;
    private const int DavePartyId = 50930001;

    // Subject: Patrick (has RuntimeDelegatedSigning from TechCorpSubUnit)
    private static readonly Guid PersonPatrickId = Guid.Parse("0196c000-0001-7001-8001-000000000050");
    private const int PatrickUserId = 20930050;
    private const int PatrickPartyId = 50930050;

    // Subject: Nina (has RuntimeDelegatedSigning from TechCorpSubUnit)
    private static readonly Guid PersonNinaId = Guid.Parse("0196c000-0001-7001-8001-000000000060");
    private const int NinaUserId = 20930060;
    private const int NinaPartyId = 50930060;

    // Subject: Erica (has RuntimeDelegatedSigning from TechCorpSubUnit)
    private static readonly Guid PersonEricaId = Guid.Parse("0196c000-0001-7001-8001-000000000070");
    private const int EricaUserId = 20930070;
    private const int EricaPartyId = 50930070;

    // Signing organizations
    private static readonly Guid OrgTechCorpId = Guid.Parse("0196c000-0001-7001-8001-000000000010");
    private static readonly Guid OrgTechCorpSubUnitId = Guid.Parse("0196c000-0001-7001-8001-000000000011");
    private const int TechCorpPartyId = 50930010;
    private const int TechCorpSubUnitPartyId = 50930011;

    // Reportee: Frank (holds instance delegations to TechCorp / MegaCorp)
    private static readonly Guid PersonFrankId = Guid.Parse("0196c000-0001-7001-8001-000000000020");
    private const int FrankPartyId = 50930020;

    // Subject for negative test: Eve (has NO RuntimeDelegatedSigning from MegaCorp)
    private static readonly Guid PersonEveId = Guid.Parse("0196c000-0001-7001-8001-000000000030");
    private static readonly Guid OrgMegaCorpId = Guid.Parse("0196c000-0001-7001-8001-000000000040");
    private const int EveUserId = 20930030;
    private const int EvePartyId = 50930030;
    private const int MegaCorpPartyId = 50930040;

    // Pinned assignment IDs (v7 UUIDs)
    private static readonly Guid AssignTechCorpToDave = Guid.Parse("0196c000-0002-7001-8001-000000000001");
    private static readonly Guid AssignFrankToTechCorp = Guid.Parse("0196c000-0002-7001-8001-000000000002");
    private static readonly Guid AssignFrankToTechCorpSubUnit = Guid.Parse("0196c000-0002-7001-8001-000000000003");
    private static readonly Guid AssignFrankToMegaCorp = Guid.Parse("0196c000-0002-7001-8001-000000000004");
    private static readonly Guid AssignTechCorpSubUnitToPatrick = Guid.Parse("0196c000-0002-7001-8001-000000000005");
    private static readonly Guid AssignTechCorpToNina = Guid.Parse("0196c000-0002-7001-8001-000000000006");
    private static readonly Guid AssignTechCorpToNinaViaKeyRole = Guid.Parse("0196c000-0002-7001-8001-000000000007");
    private static readonly Guid AssignTechCorpToEricaViaKeyRole = Guid.Parse("0196c000-0002-7001-8001-000000000008");
    private static readonly Guid AssignFrankToTechCorpEndUser = Guid.Parse("0196c000-0002-7001-8001-000000000009");

    // Instance IDs used in result assertions
    private const string InstanceIdTechCorp = "urn:altinn:instance-id:50930020/aabbccdd-0001-4001-8001-000000000001";
    private const string InstanceIdTechCorpSubUnit = "urn:altinn:instance-id:50930020/aabbccdd-0002-4001-8001-000000000002";
    private const string InstanceIdMegaCorp = "urn:altinn:instance-id:50930020/aabbccdd-0003-4001-8001-000000000003";
    private const string InstanceIdTechCorpNotAppDelegated = "urn:altinn:instance-id:50930020/aabbccdd-0004-4001-8001-000000000004";

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyInformationPointRuntimeDelegatedSigningTest"/> class.
    /// </summary>
    /// <param name="fixture">Shared API fixture from the <see cref="PolicyInformationPointDbCollection"/>.</param>
    public PolicyInformationPointRuntimeDelegatedSigningTest(AccessMgmtApiFixture fixture)
    {
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));

        fixture.EnsureSeedOnce<PolicyInformationPointRuntimeDelegatedSigningTest>(db =>
        {
            db.Entities.AddRange(
                new Entity()
                {
                    Id = PersonNinaId,
                    Name = "Nina",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "08019012345",
                    RefId = "08019012345",
                    PartyId = NinaPartyId,
                    UserId = NinaUserId,
                    DateOfBirth = new DateOnly(1990, 1, 8),
                },
                new Entity()
                {
                    Id = PersonEricaId,
                    Name = "Erica",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "09019012345",
                    RefId = "09019012345",
                    PartyId = EricaPartyId,
                    UserId = EricaUserId,
                    DateOfBirth = new DateOnly(1990, 1, 9),
                },
                new Entity()
                {
                    Id = PersonPatrickId,
                    Name = "Patrick",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "07019012345",
                    RefId = "07019012345",
                    PartyId = PatrickPartyId,
                    UserId = PatrickUserId,
                    DateOfBirth = new DateOnly(1990, 1, 7),
                },
                new Entity()
                {
                    Id = PersonDaveId,
                    Name = "Dave",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "04019012345",
                    RefId = "04019012345",
                    PartyId = DavePartyId,
                    UserId = DaveUserId,
                    DateOfBirth = new DateOnly(1990, 1, 4),
                },
                new Entity()
                {
                    Id = OrgTechCorpId,
                    Name = "TechCorp",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "499900001",
                    RefId = "499900001",
                    PartyId = TechCorpPartyId,
                },
                new Entity()
                {
                    Id = OrgTechCorpSubUnitId,
                    Name = "TechCorp Sub",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.BEDR,
                    OrganizationIdentifier = "499900002",
                    RefId = "499900002",
                    PartyId = TechCorpSubUnitPartyId,
                    ParentId = OrgTechCorpId,
                },
                new Entity()
                {
                    Id = PersonFrankId,
                    Name = "Frank",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "05019012345",
                    RefId = "05019012345",
                    PartyId = FrankPartyId,
                },
                new Entity()
                {
                    Id = PersonEveId,
                    Name = "Eve",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "06019012345",
                    RefId = "06019012345",
                    PartyId = EvePartyId,
                    UserId = EveUserId,
                    DateOfBirth = new DateOnly(1992, 1, 6),
                },
                new Entity()
                {
                    Id = OrgMegaCorpId,
                    Name = "MegaCorp",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "499900099",
                    RefId = "499900099",
                    PartyId = MegaCorpPartyId,
                });

            // TechCorp grants Dave the RuntimeDelegatedSigning package
            db.Assignments.Add(new Assignment()
            {
                Id = AssignTechCorpToDave,
                FromId = OrgTechCorpId,
                ToId = PersonDaveId,
                RoleId = RoleConstants.Rightholder,
            });

            db.AssignmentPackages.Add(new AssignmentPackage()
            {
                AssignmentId = AssignTechCorpToDave,
                PackageId = PackageConstants.CompanyRepresentativeFormTasks.Id,
            });

            // TechCorp grants Nina a key role and the RuntimeDelegatedSigning package
            db.Assignments.Add(new Assignment()
            {
                Id = AssignTechCorpToNinaViaKeyRole,
                FromId = OrgTechCorpId,
                ToId = PersonNinaId,
                RoleId = RoleConstants.ManagingDirector,
            });

            db.Assignments.Add(new Assignment()
            {
                Id = AssignTechCorpToNina,
                FromId = OrgTechCorpId,
                ToId = PersonNinaId,
                RoleId = RoleConstants.Rightholder,
            });

            db.AssignmentPackages.Add(new AssignmentPackage()
            {
                AssignmentId = AssignTechCorpToNina,
                PackageId = PackageConstants.CompanyRepresentativeFormTasks.Id,
            });

            // TechCorp grants Erica a key role
            db.Assignments.Add(new Assignment()
            {
                Id = AssignTechCorpToEricaViaKeyRole,
                FromId = OrgTechCorpId,
                ToId = PersonEricaId,
                RoleId = RoleConstants.ChairOfTheBoard,
            });

            // TechCorpSubUnit grants Patrick the RuntimeDelegatedSigning package
            db.Assignments.Add(new Assignment()
            {
                Id = AssignTechCorpSubUnitToPatrick,
                FromId = OrgTechCorpSubUnitId,
                ToId = PersonPatrickId,
                RoleId = RoleConstants.Rightholder,
            });

            db.AssignmentPackages.Add(new AssignmentPackage()
            {
                AssignmentId = AssignTechCorpSubUnitToPatrick,
                PackageId = PackageConstants.CompanyRepresentativeFormTasks.Id,
            });

            // Frank → TechCorp instance delegation (direct org)
            db.Assignments.Add(new Assignment()
            {
                Id = AssignFrankToTechCorp,
                FromId = PersonFrankId,
                ToId = OrgTechCorpId,
                RoleId = RoleConstants.AppControlledRightholder,
            });

            db.AssignmentInstances.Add(new AssignmentInstance()
            {
                AssignmentId = AssignFrankToTechCorp,
                ResourceId = TestData.NavSykepengerDialog.Id,
                InstanceId = InstanceIdTechCorp,
                InstanceSourceType = InstanceSourceTypeConstants.AltinnApp,
                PolicyPath = "nav_sykepenger_dialog/50930020/p50930010/delegationpolicy.xml",
                PolicyVersion = "2025-01-01T00:00:00.0000000Z",
            });

            // Frank → TechCorpSubUnit instance delegation (sub-unit of the signing org)
            db.Assignments.Add(new Assignment()
            {
                Id = AssignFrankToTechCorpSubUnit,
                FromId = PersonFrankId,
                ToId = OrgTechCorpSubUnitId,
                RoleId = RoleConstants.AppControlledRightholder,
            });

            db.AssignmentInstances.Add(new AssignmentInstance()
            {
                AssignmentId = AssignFrankToTechCorpSubUnit,
                ResourceId = TestData.NavSykepengerDialog.Id,
                InstanceId = InstanceIdTechCorpSubUnit,
                InstanceSourceType = InstanceSourceTypeConstants.AltinnApp,
                PolicyPath = "nav_sykepenger_dialog/50930020/p50930011/delegationpolicy.xml",
                PolicyVersion = "2025-01-02T00:00:00.0000000Z",
            });

            // Frank → MegaCorp instance delegation (Eve has NO RuntimeDelegatedSigning from MegaCorp)
            db.Assignments.Add(new Assignment()
            {
                Id = AssignFrankToMegaCorp,
                FromId = PersonFrankId,
                ToId = OrgMegaCorpId,
                RoleId = RoleConstants.Rightholder,
            });

            db.AssignmentInstances.Add(new AssignmentInstance()
            {
                AssignmentId = AssignFrankToMegaCorp,
                ResourceId = TestData.NavSykepengerDialog.Id,
                InstanceId = InstanceIdMegaCorp,
                InstanceSourceType = InstanceSourceTypeConstants.AltinnApp,
                PolicyPath = "nav_sykepenger_dialog/50930020/p50930040/delegationpolicy.xml",
                PolicyVersion = "2025-01-03T00:00:00.0000000Z",
            });

            // Frank → TechCorp instance delegation (direct org) end user delegation (not AltinnApp) to verify that the source type is filtered out of the results
            db.Assignments.Add(new Assignment()
            {
                Id = AssignFrankToTechCorpEndUser,
                FromId = PersonFrankId,
                ToId = OrgTechCorpId,
                RoleId = RoleConstants.Rightholder,
            });

            db.AssignmentInstances.Add(new AssignmentInstance()
            {
                AssignmentId = AssignFrankToTechCorpEndUser,
                ResourceId = TestData.NavSykepengerDialog.Id,
                InstanceId = InstanceIdTechCorpNotAppDelegated,
                InstanceSourceType = InstanceSourceTypeConstants.EndUser,
                PolicyPath = "nav_sykepenger_dialog/50930020/p50930012/delegationpolicy.xml",
                PolicyVersion = "2025-01-04T00:00:00.0000000Z",
            });

            db.SaveChanges();
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }

    /// <summary>
    /// Test: When Dave holds the RuntimeDelegatedSigning package from TechCorp,
    /// querying delegation changes from Frank's party returns the instance delegation
    /// that Frank created for TechCorp directly and the instance given to the sub-unit.
    /// if the delegation source method was AltinnApp
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_UserHasRuntimeDelegatedSigningFromOrg_ReturnsOrgAndSubOrgAppInstanceDelegationNotEnduserInstanceDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = DaveUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = FrankPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(
            _options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpId &&
            d.InstanceId == InstanceIdTechCorp);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpSubUnitId &&
            d.InstanceId == InstanceIdTechCorpSubUnit);
    }

    /// <summary>
    /// Test: When Nina holds the RuntimeDelegatedSigning package from TechCorp and also a key role,
    /// querying delegation changes from Frank's party returns the instance delegation
    /// that Frank created for TechCorp directly and the instance given to the sub-unit.
    /// if the delegation source method was AltinnApp
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_UserHasRuntimeDelegatedSigningAndKeyRoleFromOrg_ReturnsOrgAndSubOrgAppInstanceDelegationNotEnduserInstanceDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = NinaUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = FrankPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(
            _options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpId &&
            d.InstanceId == InstanceIdTechCorp);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpSubUnitId &&
            d.InstanceId == InstanceIdTechCorpSubUnit);
    }

    /// <summary>
    /// Test: When Erica holds the a key role,
    /// querying delegation changes from Frank's party returns the instance delegation
    /// that Frank created for TechCorp directly and the instance given to the sub-unit.
    /// if the delegation source method was AltinnApp
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_UserHasKeyRoleFromOrg_ReturnsOrgAndSubOrgAppInstanceDelegationNotEnduserInstanceDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = EricaUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = FrankPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(
            _options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpId &&
            d.InstanceId == InstanceIdTechCorp);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpSubUnitId &&
            d.InstanceId == InstanceIdTechCorpSubUnit);
    }

    /// <summary>
    /// Test: When Dave holds the RuntimeDelegatedSigning package from TechCorp,
    /// instance delegations from Frank to TechCorp's sub-unit are also returned
    /// because sub-units of the signing org are included in the lookup.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_UserHasRuntimeDelegatedSigningFromSubOrg_ReturnsSubUnitAppInstanceDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = PatrickUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = FrankPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(
            _options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Single(result);

        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.FromUuid == PersonFrankId &&
            d.ToUuid == OrgTechCorpSubUnitId &&
            d.InstanceId == InstanceIdTechCorpSubUnit);
    }

    /// <summary>
    /// Test: When Eve does NOT hold the RuntimeDelegatedSigning package from MegaCorp,
    /// the instance delegation that Frank created for MegaCorp is not returned.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_UserHasNoRuntimeDelegatedSigningFromOrg_DoesNotReturnOrgAppInstanceDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = EveUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = FrankPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(
            _options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        Assert.DoesNotContain(result, d =>
            d.ToUuid == OrgMegaCorpId &&
            d.InstanceId == InstanceIdMegaCorp);
    }
}
