using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Models;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for v2 client-delegated resources in the GetAllDelegationChanges endpoint.
/// Feature flag is ENABLED for these tests.
/// </summary>
[IntegrationTest]
[Collection(PolicyInformationPointDbCollection.Name)]
public class PolicyInformationPointClientDelegationResourceTest
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    // Test entities
    private static readonly Guid OrgMainUnitId = Guid.Parse("0196b000-0001-7001-8001-000000000020");
    private static readonly Guid OrgSubUnitId = Guid.Parse("0196b000-0001-7001-8001-000000000024");
    private static readonly Guid PersonRecipientId = Guid.Parse("0196b000-0001-7001-8001-000000000021");
    private static readonly Guid PersonNonRecipientId = Guid.Parse("0196b000-0001-7001-8001-000000000022");
    private static readonly Guid ClientProviderId = Guid.Parse("0196b000-0001-7001-8001-000000000023");
    private static readonly Guid SystemUserRecipientId = Guid.Parse("0196b000-0001-7001-8001-000000000025");

    // Party/User IDs
    private const int OrgMainUnitPartyId = 50900020;
    private const int OrgSubUnitPartyId = 50900024;
    private const int RecipientUserId = 20900021;
    private const int RecipientPartyId = 50900021;
    private const int NonRecipientUserId = 20900022;

    // Assignment IDs
    private static readonly Guid AssignOrgToClientId = Guid.Parse("0196b000-0002-7001-8001-000000000020");
    private static readonly Guid AssignClientToRecipientId = Guid.Parse("0196b000-0002-7001-8001-000000000021");
    private static readonly Guid AssignClientToSystemUserId = Guid.Parse("0196b000-0002-7001-8001-000000000022");

    // Policy paths for assertion
    private const string ResourcePolicyPath = "nav_sykepenger_dialog/50900020/client_delegated/delegationpolicy.xml";

    private const string AppPolicyPath = "app_skd_sirius-skattemelding-v1/50900020/client_delegated/delegationpolicy.xml";

    public PolicyInformationPointClientDelegationResourceTest(AccessMgmtApiFixture fixture)
    {
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.IncludeClientDelegatedResourcesInPip);

        fixture.EnsureSeedOnce<PolicyInformationPointClientDelegationResourceTest>(db =>
        {
            // Entities
            db.Entities.AddRange(
                new Entity()
                {
                    Id = OrgMainUnitId,
                    Name = "Main Unit Org",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900020",
                    RefId = "399900020",
                    PartyId = OrgMainUnitPartyId,
                },
                new Entity()
                {
                    Id = OrgSubUnitId,
                    Name = "Sub Unit Org",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900024",
                    RefId = "399900024",
                    PartyId = OrgSubUnitPartyId,
                    ParentId = OrgMainUnitId,
                },
                new Entity()
                {
                    Id = PersonRecipientId,
                    Name = "Recipient User",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "14019099901",
                    RefId = "14019099901",
                    PartyId = RecipientPartyId,
                    UserId = RecipientUserId,
                    DateOfBirth = new DateOnly(1990, 1, 4),
                },
                new Entity()
                {
                    Id = PersonNonRecipientId,
                    Name = "Non-Recipient User",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "15019099902",
                    RefId = "15019099902",
                    PartyId = 50900022,
                    UserId = NonRecipientUserId,
                    DateOfBirth = new DateOnly(1988, 3, 10),
                },
                new Entity()
                {
                    Id = ClientProviderId,
                    Name = "Client Provider",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900023",
                    RefId = "399900023",
                    PartyId = 50900023,
                },
                new Entity()
                {
                    Id = SystemUserRecipientId,
                    Name = "System User Recipient",
                    TypeId = EntityTypeConstants.SystemUser,
                    VariantId = EntityVariantConstants.AgentSystem,
                    RefId = "system-user-recipient-01",
                });

            // === Scenario 1: Resource delegation from MainUnit via client to Person ===
            var assignOrgToClient = new Assignment()
            {
                Id = AssignOrgToClientId,
                FromId = OrgMainUnitId,
                ToId = ClientProviderId,
                RoleId = RoleConstants.Rightholder,
            };
            var assignClientToRecipient = new Assignment()
            {
                Id = AssignClientToRecipientId,
                FromId = ClientProviderId,
                ToId = PersonRecipientId,
                RoleId = RoleConstants.Agent,
            };

            var assignmentResource = new AssignmentResource()
            {
                AssignmentId = AssignOrgToClientId,
                ResourceId = TestData.NavSykepengerDialog.Id,
                PolicyPath = ResourcePolicyPath,
                PolicyVersion = "2024-06-01T00:00:00.0000000Z",
                DelegationChangeId = 99000020,
            };

            var delegation = new Delegation()
            {
                FromId = AssignOrgToClientId,
                ToId = AssignClientToRecipientId,
                FacilitatorId = ClientProviderId,
            };

            var delegationResource = new DelegationResource()
            {
                DelegationId = delegation.Id,
                ResourceId = TestData.NavSykepengerDialog.Id,
                AssignmentResourceId = assignmentResource.Id,
            };

            // === Scenario 2: Resource delegation from MainUnit via client to SystemUser ===
            var assignClientToSystemUser = new Assignment()
            {
                Id = AssignClientToSystemUserId,
                FromId = ClientProviderId,
                ToId = SystemUserRecipientId,
                RoleId = RoleConstants.Agent,
            };

            var delegationToSystemUser = new Delegation()
            {
                FromId = AssignOrgToClientId,
                ToId = AssignClientToSystemUserId,
                FacilitatorId = ClientProviderId,
            };

            var delegationResourceSu = new DelegationResource()
            {
                DelegationId = delegationToSystemUser.Id,
                ResourceId = TestData.NavSykepengerDialog.Id,
                AssignmentResourceId = assignmentResource.Id,
            };

            // === Scenario 3: AltinnAppId resource delegation from MainUnit via client to Person ===
            // Reuses assignOrgToClient and assignClientToRecipient assignments, and the delegation between them.
            // Only adds a different resource (SiriusSkattemelding) to the same assignment and delegation.
            var assignmentResourceApp = new AssignmentResource()
            {
                AssignmentId = AssignOrgToClientId,
                ResourceId = TestData.SiriusSkattemelding.Id,
                PolicyPath = AppPolicyPath,
                PolicyVersion = "2024-07-01T00:00:00.0000000Z",
                DelegationChangeId = 99000022,
            };

            var delegationResourceApp = new DelegationResource()
            {
                DelegationId = delegation.Id,
                ResourceId = TestData.SiriusSkattemelding.Id,
                AssignmentResourceId = assignmentResourceApp.Id,
            };

            // Persist all
            db.Assignments.AddRange(assignOrgToClient, assignClientToRecipient, assignClientToSystemUser);
            db.AssignmentResources.AddRange(assignmentResource, assignmentResourceApp);
            db.Delegations.AddRange(delegation, delegationToSystemUser);
            db.DelegationResources.AddRange(delegationResource, delegationResourceSu, delegationResourceApp);

            db.SaveChanges();
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }

    /// <summary>
    /// Subject is userId, party matches the delegating org.
    /// Expects the client-delegated resource to be returned.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_SubjectUserId_MatchingParty_ReturnsDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = RecipientUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgMainUnitPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        var delegation = Assert.Single(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.BlobStoragePolicyPath == ResourcePolicyPath);

        // Verify from/to mapping is populated for PDP consumption
        Assert.Equal(OrgMainUnitPartyId, delegation.OfferedByPartyId);
        Assert.Equal(OrgMainUnitId, delegation.FromUuid);
        Assert.Equal(PersonRecipientId, delegation.ToUuid);
        Assert.Equal(RecipientUserId, delegation.CoveredByUserId);
        Assert.Null(delegation.CoveredByPartyId);
    }

    /// <summary>
    /// Subject is a system user UUID, party matches the delegating org.
    /// Expects the client-delegated resource to be returned.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_SubjectSystemUserUuid_MatchingParty_ReturnsDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:systemuser:uuid", value = SystemUserRecipientId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgMainUnitPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        var delegation = Assert.Single(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.BlobStoragePolicyPath == ResourcePolicyPath);

        // Verify system user gets ToUuid/ToUuidType populated
        Assert.Equal(OrgMainUnitPartyId, delegation.OfferedByPartyId);
        Assert.Equal(SystemUserRecipientId, delegation.ToUuid);
        Assert.Null(delegation.CoveredByUserId);
        Assert.Null(delegation.CoveredByPartyId);
    }

    /// <summary>
    /// Party is the subunit of the main unit that has the delegation.
    /// The query should resolve the parent and find the delegation.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_PartyIsSubUnit_ResolvesParentAndReturnsDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = RecipientUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgSubUnitPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Contains(result, d =>
            d.ResourceId == "nav_sykepenger_dialog" &&
            d.BlobStoragePolicyPath == ResourcePolicyPath);
    }

    /// <summary>
    /// Resource is specified as AltinnAppId (org/app format).
    /// Expects the delegation to be found via the app_ prefixed RefId.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_AltinnAppIdResource_ReturnsDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = RecipientUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgMainUnitPartyId.ToString() },
            resource = new[]
            {
                new { id = "urn:altinn:org", value = "skd" },
                new { id = "urn:altinn:app", value = "sirius-skattemelding-v1" }
            }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Contains(result, d =>
            d.BlobStoragePolicyPath == AppPolicyPath);
    }

    /// <summary>
    /// Subject does not match any client delegation recipient.
    /// Expects no client-delegated resources in the result.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_NonMatchingSubject_DoesNotReturnClientDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = NonRecipientUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgMainUnitPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.DoesNotContain(result, d => d.BlobStoragePolicyPath == ResourcePolicyPath);
    }

    /// <summary>
    /// Resource does not match any client delegation.
    /// Expects no client-delegated resources in the result.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_NonMatchingResource_DoesNotReturnClientDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = RecipientUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgMainUnitPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "non_existing_resource" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.DoesNotContain(result, d => d.BlobStoragePolicyPath == ResourcePolicyPath);
    }

    /// <summary>
    /// Party does not match the offering entity or its parent.
    /// Expects no client-delegated resources in the result.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_NonMatchingParty_DoesNotReturnClientDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = RecipientUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = RecipientPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.DoesNotContain(result, d => d.BlobStoragePolicyPath == ResourcePolicyPath);
    }
}

/// <summary>
/// Integration test verifying that client-delegated resources are NOT returned
/// when the feature flag is disabled.
/// </summary>
[IntegrationTest]
public class PolicyInformationPointClientDelegationsDisabledTest : IClassFixture<AccessMgmtApiFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private static readonly Guid OrgId = Guid.Parse("0196b000-0001-7001-8001-000000000030");
    private static readonly Guid PersonId = Guid.Parse("0196b000-0001-7001-8001-000000000031");
    private static readonly Guid ClientId = Guid.Parse("0196b000-0001-7001-8001-000000000032");

    private const int OrgPartyId = 50900030;
    private const int PersonUserId = 20900031;
    private const string PolicyPath = "nav_sykepenger_dialog/50900030/client_disabled/delegationpolicy.xml";

    public PolicyInformationPointClientDelegationsDisabledTest(AccessMgmtApiFixture fixture)
    {
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
        fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.IncludeClientDelegatedResourcesInPip);

        fixture.EnsureSeedOnce<PolicyInformationPointClientDelegationsDisabledTest>(db =>
        {
            db.Entities.AddRange(
                new Entity()
                {
                    Id = OrgId,
                    Name = "Org Disabled Test",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900030",
                    RefId = "399900030",
                    PartyId = OrgPartyId,
                },
                new Entity()
                {
                    Id = PersonId,
                    Name = "Person Disabled Test",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "16019099903",
                    RefId = "16019099903",
                    PartyId = 50900031,
                    UserId = PersonUserId,
                    DateOfBirth = new DateOnly(1992, 6, 1),
                },
                new Entity()
                {
                    Id = ClientId,
                    Name = "Client Disabled Test",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399900032",
                    RefId = "399900032",
                    PartyId = 50900032,
                });

            var assignFrom = new Assignment() { Id = Guid.Parse("0196b000-0002-7001-8001-000000000030"), FromId = OrgId, ToId = ClientId, RoleId = RoleConstants.Rightholder };
            var assignTo = new Assignment() { Id = Guid.Parse("0196b000-0002-7001-8001-000000000031"), FromId = ClientId, ToId = PersonId, RoleId = RoleConstants.Agent };
            var assignmentResource = new AssignmentResource() { AssignmentId = assignFrom.Id, ResourceId = TestData.NavSykepengerDialog.Id, PolicyPath = PolicyPath, PolicyVersion = "2024-06-01T00:00:00.0000000Z", DelegationChangeId = 99000030 };
            var delegation = new Delegation() { FromId = assignFrom.Id, ToId = assignTo.Id, FacilitatorId = ClientId };
            var delegationResource = new DelegationResource() { DelegationId = delegation.Id, ResourceId = TestData.NavSykepengerDialog.Id, AssignmentResourceId = assignmentResource.Id };

            db.Assignments.AddRange(assignFrom, assignTo);
            db.AssignmentResources.Add(assignmentResource);
            db.Delegations.Add(delegation);
            db.DelegationResources.Add(delegationResource);
            db.SaveChanges();
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }

    /// <summary>
    /// When the feature flag is disabled, client-delegated resources should not be returned
    /// even when subject/party/resource all match.
    /// </summary>
    [Fact]
    public async Task GetDelegationChanges_FeatureDisabled_DoesNotReturnClientDelegation()
    {
        var request = new
        {
            subject = new { id = "urn:altinn:userid", value = PersonUserId.ToString() },
            party = new { id = "urn:altinn:partyid", value = OrgPartyId.ToString() },
            resource = new[] { new { id = "urn:altinn:resource", value = "nav_sykepenger_dialog" } }
        };

        var response = await _client.PostAsJsonAsync(
            "accessmanagement/api/v1/policyinformation/getdelegationchanges",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DelegationChangeExternal>>(_options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.DoesNotContain(result, d => d.BlobStoragePolicyPath == PolicyPath);
    }
}
