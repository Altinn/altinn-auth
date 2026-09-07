using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Tests.Integration.Services;

/// <summary>
/// Exercises the database triggers that populate dbo.activitylog, and the
/// <see cref="ActivityLogQuery"/> on top of it. Every test creates its own rows and asserts
/// only on those, so the tests are independent of other data in the shared database.
/// </summary>
[IntegrationTest]
public class ActivityLogTests : IClassFixture<EfDatabaseFixture>, IAsyncLifetime
{
    private static readonly AuditValues Seeder = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest);

    private readonly EfDatabaseFixture _fixture;
    private readonly AppDbContext _db;
    private readonly ActivityLogQuery _query;

    public ActivityLogTests(EfDatabaseFixture fixture)
    {
        _fixture = fixture;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Db.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);
        _query = new ActivityLogQuery(_db);
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static Entity NewOrganization(string name) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        RefId = $"activitylog-test-{Guid.NewGuid():N}",
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
    };

    private async Task<(Entity From, Entity To, Assignment Assignment)> SeedAssignment()
    {
        var from = NewOrganization("Aktivitetslogg Fra AS");
        var to = NewOrganization("Aktivitetslogg Til AS");
        var assignment = new Assignment { Id = Guid.CreateVersion7(), FromId = from.Id, ToId = to.Id, RoleId = RoleConstants.Rightholder };

        _db.Entities.AddRange(from, to);
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(Seeder);

        return (from, to, assignment);
    }

    private async Task<ActivityLog> SingleEvent(Guid itemId, ActivityLogTrigger trigger)
    {
        return Assert.Single(await _db.ActivityLogs.AsNoTracking()
            .Where(t => t.ItemId == itemId && t.Trigger == trigger)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AssignmentInsert_WritesCreatedEventWithNameSnapshots()
    {
        var (from, to, assignment) = await SeedAssignment();

        var entry = await SingleEvent(assignment.Id, ActivityLogTrigger.Created);

        Assert.Equal(ActivityLogType.Assignment, entry.Type);
        Assert.Null(entry.Subtype);
        Assert.Equal(from.Id, entry.FromId);
        Assert.Equal(from.Name, entry.FromName);
        Assert.Equal(to.Id, entry.ToId);
        Assert.Equal(to.Name, entry.ToName);
        Assert.Equal((Guid)RoleConstants.Rightholder, entry.RoleId);
        Assert.False(string.IsNullOrEmpty(entry.RoleName));
        Assert.Equal((Guid)SystemEntityConstants.StaticDataIngest, entry.ById);
        Assert.Equal((Guid)SystemEntityConstants.StaticDataIngest, entry.SourceId);
        Assert.False(string.IsNullOrEmpty(entry.OperationId));
        Assert.Null(entry.ParentId);
    }

    [Fact]
    public async Task AssignmentPackageInsertAndDelete_WritesEventsWithPackageSnapshot()
    {
        var (from, to, assignment) = await SeedAssignment();
        var package = await _db.Packages.AsNoTracking().OrderBy(p => p.Id).FirstAsync(TestContext.Current.CancellationToken);

        var assignmentPackage = new AssignmentPackage { Id = Guid.CreateVersion7(), AssignmentId = assignment.Id, PackageId = package.Id };
        _db.AssignmentPackages.Add(assignmentPackage);
        await _db.SaveChangesAsync(Seeder);

        var created = await SingleEvent(assignmentPackage.Id, ActivityLogTrigger.Created);
        Assert.Equal(ActivityLogType.Assignment, created.Type);
        Assert.Equal(ActivityLogSubtype.Package, created.Subtype);
        Assert.Equal(assignment.Id, created.ParentId);
        Assert.Equal(package.Id, created.PackageId);
        Assert.Equal(package.Name, created.PackageName);
        Assert.Equal(from.Name, created.FromName);
        Assert.Equal(to.Name, created.ToName);

        _db.AssignmentPackages.Remove(assignmentPackage);
        await _db.SaveChangesAsync(Seeder);

        var deleted = await SingleEvent(assignmentPackage.Id, ActivityLogTrigger.Deleted);
        Assert.Equal(package.Name, deleted.PackageName);
        Assert.Equal((Guid)SystemEntityConstants.StaticDataIngest, deleted.ById);
        Assert.Equal(assignment.Id, deleted.ParentId);
    }

    [Fact]
    public async Task DelegationInsert_ResolvesPartiesAndRolesThroughAssignments()
    {
        var client = NewOrganization("Aktivitetslogg Klient AS");
        var facilitator = NewOrganization("Aktivitetslogg Fasilitator AS");
        var agent = NewOrganization("Aktivitetslogg Agent AS");

        var clientAssignment = new Assignment { Id = Guid.CreateVersion7(), FromId = client.Id, ToId = facilitator.Id, RoleId = RoleConstants.Rightholder };
        var agentAssignment = new Assignment { Id = Guid.CreateVersion7(), FromId = facilitator.Id, ToId = agent.Id, RoleId = RoleConstants.Agent };
        var delegation = new Delegation { Id = Guid.CreateVersion7(), FromId = clientAssignment.Id, ToId = agentAssignment.Id, FacilitatorId = facilitator.Id };

        _db.Entities.AddRange(client, facilitator, agent);
        _db.Assignments.AddRange(clientAssignment, agentAssignment);
        _db.Delegations.Add(delegation);
        await _db.SaveChangesAsync(Seeder);

        var entry = await SingleEvent(delegation.Id, ActivityLogTrigger.Created);

        Assert.Equal(ActivityLogType.Delegation, entry.Type);
        Assert.Equal(client.Id, entry.FromId);
        Assert.Equal(client.Name, entry.FromName);
        Assert.Equal(agent.Id, entry.ToId);
        Assert.Equal(agent.Name, entry.ToName);
        Assert.Equal(facilitator.Id, entry.ViaId);
        Assert.Equal(facilitator.Name, entry.ViaName);
        Assert.Equal((Guid)RoleConstants.Rightholder, entry.RoleId);
        Assert.Equal((Guid)RoleConstants.Agent, entry.ViaRoleId);
    }

    [Fact]
    public async Task CascadeDeleteOfEntity_WritesDeletedEventsWithResolvedNames()
    {
        var client = NewOrganization("Aktivitetslogg Kaskade Klient AS");
        var facilitator = NewOrganization("Aktivitetslogg Kaskade Fasilitator AS");
        var agent = NewOrganization("Aktivitetslogg Kaskade Agent AS");

        var clientAssignment = new Assignment { Id = Guid.CreateVersion7(), FromId = client.Id, ToId = facilitator.Id, RoleId = RoleConstants.Rightholder };
        var agentAssignment = new Assignment { Id = Guid.CreateVersion7(), FromId = facilitator.Id, ToId = agent.Id, RoleId = RoleConstants.Agent };
        var delegation = new Delegation { Id = Guid.CreateVersion7(), FromId = clientAssignment.Id, ToId = agentAssignment.Id, FacilitatorId = facilitator.Id };
        var package = await _db.Packages.AsNoTracking().OrderBy(p => p.Id).FirstAsync(TestContext.Current.CancellationToken);
        var delegationPackage = new DelegationPackage { Id = Guid.CreateVersion7(), DelegationId = delegation.Id, PackageId = package.Id };

        _db.Entities.AddRange(client, facilitator, agent);
        _db.Assignments.AddRange(clientAssignment, agentAssignment);
        _db.Delegations.Add(delegation);
        _db.DelegationPackages.Add(delegationPackage);
        await _db.SaveChangesAsync(Seeder);

        // Deleting the facilitator cascades through both assignments, the delegation and its
        // package; the triggers must still resolve every name via the history fallback.
        _db.Entities.Remove(await _db.Entities.FirstAsync(e => e.Id == facilitator.Id, TestContext.Current.CancellationToken));
        await _db.SaveChangesAsync(Seeder);

        var delegationDeleted = await SingleEvent(delegation.Id, ActivityLogTrigger.Deleted);
        Assert.Equal(client.Name, delegationDeleted.FromName);
        Assert.Equal(agent.Name, delegationDeleted.ToName);
        Assert.Equal(facilitator.Name, delegationDeleted.ViaName);
        Assert.Equal((Guid)SystemEntityConstants.StaticDataIngest, delegationDeleted.ById);

        var packageDeleted = await SingleEvent(delegationPackage.Id, ActivityLogTrigger.Deleted);
        Assert.Equal(ActivityLogSubtype.Package, packageDeleted.Subtype);
        Assert.Equal(package.Name, packageDeleted.PackageName);
        Assert.Equal(facilitator.Name, packageDeleted.ViaName);

        var assignmentDeleted = await SingleEvent(clientAssignment.Id, ActivityLogTrigger.Deleted);
        Assert.Equal(client.Name, assignmentDeleted.FromName);
        Assert.Equal(facilitator.Name, assignmentDeleted.ToName);
    }

    [Fact]
    public async Task RequestStatusUpdate_WritesUpdatedEventWithPreviousStatus()
    {
        var (from, to, _) = await SeedAssignment();
        var package = await _db.Packages.AsNoTracking().OrderBy(p => p.Id).FirstAsync(TestContext.Current.CancellationToken);

        var request = new RequestAssignment { Id = Guid.CreateVersion7(), FromId = from.Id, ToId = to.Id, RoleId = RoleConstants.Rightholder, ById = from.Id };
        var requestPackage = new RequestAssignmentPackage { Id = Guid.CreateVersion7(), AssignmentId = request.Id, PackageId = package.Id, Status = RequestStatus.Pending };
        _db.RequestAssignments.Add(request);
        _db.RequestAssignmentPackages.Add(requestPackage);
        await _db.SaveChangesAsync(Seeder);

        var created = await SingleEvent(requestPackage.Id, ActivityLogTrigger.Created);
        Assert.Equal(ActivityLogType.Request, created.Type);
        Assert.Equal(RequestStatus.Pending, created.Status);
        Assert.Equal(request.Id, created.ParentId);
        Assert.Contains("requestedById", created.Details);

        var tracked = await _db.RequestAssignmentPackages.FirstAsync(t => t.Id == requestPackage.Id, TestContext.Current.CancellationToken);
        tracked.Status = RequestStatus.Approved;
        await _db.SaveChangesAsync(Seeder);

        var updated = await SingleEvent(requestPackage.Id, ActivityLogTrigger.Updated);
        Assert.Equal(RequestStatus.Approved, updated.Status);
        Assert.Contains("previousStatus", updated.Details);
        Assert.Equal(from.Name, updated.FromName);
        Assert.Equal(to.Name, updated.ToName);
    }

    [Fact]
    public async Task AssignmentResourceUpdate_PolicyChangesProduceNoEvent()
    {
        var (_, _, assignment) = await SeedAssignment();

        var resourceType = new ResourceType { Id = Guid.CreateVersion7(), Name = $"activitylog-rt-{Guid.NewGuid():N}" };
        var resource = new Resource
        {
            Id = Guid.CreateVersion7(),
            Name = "Aktivitetslogg Ressurs",
            RefId = $"activitylog-resource-{Guid.NewGuid():N}",
            Description = "Test resource for activity log",
            TypeId = resourceType.Id,
            ProviderId = ProviderConstants.Altinn3,
        };
        var assignmentResource = new AssignmentResource { Id = Guid.CreateVersion7(), AssignmentId = assignment.Id, ResourceId = resource.Id };

        _db.Set<ResourceType>().Add(resourceType);
        _db.Set<Resource>().Add(resource);
        await _db.SaveChangesAsync(Seeder);
        _db.AssignmentResources.Add(assignmentResource);
        await _db.SaveChangesAsync(Seeder);

        var created = await SingleEvent(assignmentResource.Id, ActivityLogTrigger.Created);
        Assert.Equal(ActivityLogSubtype.Resource, created.Subtype);
        Assert.Equal(resource.Name, created.ResourceName);

        var tracked = await _db.AssignmentResources.FirstAsync(t => t.Id == assignmentResource.Id, TestContext.Current.CancellationToken);
        tracked.PolicyPath = "activitylog/test/path";
        tracked.PolicyVersion = "2";
        await _db.SaveChangesAsync(Seeder);

        var events = await _db.ActivityLogs.AsNoTracking()
            .Where(t => t.ItemId == assignmentResource.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(events);
    }

    [Fact]
    public async Task AssignmentInstanceReparent_WritesUpdatedEventWithPreviousAssignment()
    {
        var (from, to, assignment) = await SeedAssignment();
        var secondAssignment = new Assignment { Id = Guid.CreateVersion7(), FromId = from.Id, ToId = to.Id, RoleId = RoleConstants.Agent };
        _db.Assignments.Add(secondAssignment);

        var resourceType = new ResourceType { Id = Guid.CreateVersion7(), Name = $"activitylog-rt-{Guid.NewGuid():N}" };
        var resource = new Resource
        {
            Id = Guid.CreateVersion7(),
            Name = "Aktivitetslogg Instansressurs",
            RefId = $"activitylog-resource-{Guid.NewGuid():N}",
            Description = "Test resource for activity log",
            TypeId = resourceType.Id,
            ProviderId = ProviderConstants.Altinn3,
        };
        _db.Set<ResourceType>().Add(resourceType);
        _db.Set<Resource>().Add(resource);
        await _db.SaveChangesAsync(Seeder);

        var instance = new AssignmentInstance
        {
            Id = Guid.CreateVersion7(),
            AssignmentId = assignment.Id,
            ResourceId = resource.Id,
            InstanceId = $"urn:altinn:instance-id:{Guid.NewGuid():N}",
        };
        _db.AssignmentInstances.Add(instance);
        await _db.SaveChangesAsync(Seeder);

        var created = await SingleEvent(instance.Id, ActivityLogTrigger.Created);
        Assert.Equal(ActivityLogSubtype.Instance, created.Subtype);
        Assert.Equal(instance.InstanceId, created.InstanceId);
        Assert.Equal(assignment.Id, created.ParentId);

        var tracked = await _db.AssignmentInstances.FirstAsync(t => t.Id == instance.Id, TestContext.Current.CancellationToken);
        tracked.AssignmentId = secondAssignment.Id;
        await _db.SaveChangesAsync(Seeder);

        var updated = await SingleEvent(instance.Id, ActivityLogTrigger.Updated);
        Assert.Equal(secondAssignment.Id, updated.ParentId);
        Assert.Contains(assignment.Id.ToString(), updated.Details);
    }

    [Fact]
    public async Task ActivityLogQuery_FiltersOnInvolvedPartyAndPaginatesWithoutOverlap()
    {
        var (from, to, assignment) = await SeedAssignment();
        var package = await _db.Packages.AsNoTracking().OrderBy(p => p.Id).FirstAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < 5; i++)
        {
            var assignmentPackage = new AssignmentPackage { Id = Guid.CreateVersion7(), AssignmentId = assignment.Id, PackageId = package.Id };
            _db.AssignmentPackages.Add(assignmentPackage);
            await _db.SaveChangesAsync(Seeder);
            _db.AssignmentPackages.Remove(assignmentPackage);
            await _db.SaveChangesAsync(Seeder);
        }

        var filter = new ActivityLogQueryFilter { InvolvedIds = [from.Id] };

        var all = await _query.GetAsync(filter, 100, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(11, all.Items.Count);
        Assert.False(all.HasMore);
        Assert.All(all.Items, t => Assert.True(t.FromId == from.Id || t.ToId == from.Id || t.ViaId == from.Id));

        var page1 = await _query.GetAsync(filter, 4, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(4, page1.Items.Count);
        Assert.True(page1.HasMore);

        var page2 = await _query.GetAsync(filter, 4, 1, TestContext.Current.CancellationToken);
        Assert.Equal(4, page2.Items.Count);
        Assert.True(page2.HasMore);
        Assert.Empty(page1.Items.Select(t => t.Id).Intersect(page2.Items.Select(t => t.Id)));

        var page3 = await _query.GetAsync(filter, 4, 2, TestContext.Current.CancellationToken);
        Assert.Equal(3, page3.Items.Count);
        Assert.False(page3.HasMore);
        Assert.Empty(page3.Items.Select(t => t.Id).Intersect(page1.Items.Concat(page2.Items).Select(t => t.Id)));

        var deletesOnly = await _query.GetAsync(
            filter with { Triggers = [ActivityLogTrigger.Deleted], Subtypes = [ActivityLogSubtype.Package] },
            100,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(5, deletesOnly.Items.Count);
    }

    [Fact]
    public async Task ActivityTypeCatalog_IsIngestedIntoTable()
    {
        var rows = await _db.ActivityTypes.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ActivityTypeConstants.AllEntities().Count, rows.Count);
        Assert.All(ActivityTypeConstants.AllEntities(), definition =>
        {
            var row = Assert.Single(rows, r => r.Id == definition.Id);
            Assert.Equal(definition.Entity.Name, row.Name);
            Assert.Equal((definition.Entity.Type, definition.Entity.Subtype, definition.Entity.Trigger, definition.Entity.Status), (row.Type, row.Subtype, row.Trigger, row.Status));
        });
    }

    [Fact]
    public async Task ActivityLogQuery_FiltersOnActivityTypeKeysAsWholeConjunctions()
    {
        var (from, _, assignment) = await SeedAssignment();
        var package = await _db.Packages.AsNoTracking().OrderBy(p => p.Id).FirstAsync(TestContext.Current.CancellationToken);

        var assignmentPackage = new AssignmentPackage { Id = Guid.CreateVersion7(), AssignmentId = assignment.Id, PackageId = package.Id };
        _db.AssignmentPackages.Add(assignmentPackage);
        await _db.SaveChangesAsync(Seeder);
        _db.AssignmentPackages.Remove(assignmentPackage);
        await _db.SaveChangesAsync(Seeder);

        var filter = new ActivityLogQueryFilter { InvolvedIds = [from.Id] };

        var packageCreatedKey = new ActivityTypeKey(ActivityLogType.Assignment, ActivityLogSubtype.Package, ActivityLogTrigger.Created, null);
        var assignmentDeletedKey = new ActivityTypeKey(ActivityLogType.Assignment, null, ActivityLogTrigger.Deleted, null);

        var single = await _query.GetAsync(
            filter with { ActivityTypeKeys = [packageCreatedKey] },
            100,
            cancellationToken: TestContext.Current.CancellationToken);
        var packageCreated = Assert.Single(single.Items);
        Assert.Equal(assignmentPackage.Id, packageCreated.ItemId);

        // Two keys OR whole conjunctions: package-created plus assignment-deleted must not
        // leak the cross-combinations (assignment-created, package-deleted).
        var both = await _query.GetAsync(
            filter with { ActivityTypeKeys = [packageCreatedKey, assignmentDeletedKey] },
            100,
            cancellationToken: TestContext.Current.CancellationToken);
        var match = Assert.Single(both.Items);
        Assert.Equal(assignmentPackage.Id, match.ItemId);
        Assert.Equal(ActivityLogTrigger.Created, match.Trigger);
    }
}
