using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.Notification;
using Altinn.Authorization.Integration.Platform.Notification.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

// No web host: builds an AppDbContext directly from the EfDatabaseFixture
// database (a clone of the shared migrated + seeded template), then instantiates
// RequestService by hand. Each test seeds a unique Resource to avoid inter-test
// interference, so the class keeps its own per-class fixture.
namespace Altinn.AccessManagement.Tests.Integration.Services;

[IntegrationTest]
public class RequestServiceTests : IClassFixture<EfDatabaseFixture>, IAsyncLifetime
{
    private static readonly AuditValues TestAudit = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest);

    private static readonly Entity OrgFrom = new()
    {
        Id = Guid.Parse("01960001-0000-7000-8000-000000000001"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "RequestTest OrgFrom",
        RefId = "REQ-TEST-ORG-01",
    };

    private static readonly Entity PersonTo = new()
    {
        Id = Guid.Parse("01960001-0000-7000-8000-000000000002"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "RequestTest PersonTo",
        RefId = "REQ-TEST-PERSON-01",
    };

    private static readonly ResourceType TestResourceType = new()
    {
        Id = Guid.Parse("01960001-0000-0000-0000-000000000001"),
        Name = "RequestTestResourceType",
    };

    private readonly EfDatabaseFixture _fixture;
    private readonly AppDbContext _db;
    private readonly RequestService _requestService;

    public RequestServiceTests(EfDatabaseFixture fixture)
    {
        _fixture = fixture;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Db.Admin.ToString())
            .Options;

        _db = new AppDbContext(options)
        {
            AuditAccessor = new AuditAccessor { AuditValues = TestAudit }
        };

        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection([
            ]);

        var collection = new ServiceCollection()
            .AddSingleton<IConfiguration>(configurationBuilder.Build())
            .AddOptions<CoreAppsettings>();

        var sp = collection.Services.BuildServiceProvider();

        _requestService = new RequestService(
            _db,
            sp.GetRequiredService<IOptions<CoreAppsettings>>(),
            new DisabledFeatureManager(),
            new PolicyRetrievalPointMock());
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await _fixture.EnsureSeedOnceAsync(() => SeedSharedData(_db));

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task SeedSharedData(AppDbContext db)
    {
        db.Entities.AddRange(OrgFrom, PersonTo);
        db.ResourceTypes.Add(TestResourceType);

        await db.SaveChangesAsync(TestAudit);

        db.ChangeTracker.Clear();
    }

    /// <summary>
    /// Seeds a unique <see cref="Resource"/> per test so create/update operations don't interfere with each other.
    /// </summary>
    private async Task<Resource> SeedUniqueResource(string name = "UniqueTestResource")
    {
        var resource = new Resource
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = "UniqueTestResourceDescription",
            RefId = Guid.CreateVersion7().ToString(),
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = TestResourceType.Id,
        };
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(TestAudit);
        _db.ChangeTracker.Clear();
        return resource;
    }

    #region CreateRequestAssignmentResource
    [Fact]
    public async Task CreateRequestAssignmentResource_WithValidInput_CreatesDraftRequest()
    {
        var resource = await SeedUniqueResource();
        var result = await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Draft, TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatusV2.Draft, result.Value.Status);
        Assert.Equal(resource.Id, result.Value.Resource.Id);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public async Task CreateRequestAssignmentResource_CalledTwice_ReturnsExistingPendingRequest()
    {
        var resource = await SeedUniqueResource();

        var first = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        var second = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateRequestAssignmentResource_CalledAgainWithSameRightKeysInOtherOrder_ReturnsExistingPendingRequest()
    {
        var resource = await SeedUniqueResource();

        var first = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, ["key-a", "key-b"], RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        var second = await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, ["key-b", "key-a", "key-b"], RequestStatusV2.Pending, TestContext.Current.CancellationToken);

        Assert.False(second.IsProblem);
        Assert.Equal(first.Id, second.Value.Id);
    }

    [Theory]
    [InlineData(new[] { "key-a" }, new[] { "key-b" })]
    [InlineData(new[] { "key-a" }, new[] { "key-a", "key-b" })]
    [InlineData(new[] { "key-a" }, null)]
    [InlineData(null, new[] { "key-a" })]
    public async Task CreateRequestAssignmentResource_CalledAgainWithOtherRightKeys_ReturnsConflictAndKeepsPendingRequest(string[]? pendingKeys, string[]? newKeys)
    {
        var resource = await SeedUniqueResource();
        var first = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, pendingKeys, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        var second = await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, newKeys, RequestStatusV2.Pending, TestContext.Current.CancellationToken);

        Assert.True(second.IsProblem);
        Assert.Equal(Problems.PendingRequestWithOtherRightKeys.ErrorCode, second.Problem.ErrorCode);
        var stored = Assert.Single(await _db.RequestAssignmentResources.AsNoTracking()
            .Where(r => r.ResourceId == resource.Id)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(first.Id, stored.Id);
        Assert.Equal(pendingKeys, stored.RequestedRightKeys);
    }
    #endregion

    #region CreateRequestAssignmentResource with right keys

    // The service stores the keys it is given; validating them against the policy is tested separately,
    // so plain strings are enough here.
    [Fact]
    public async Task CreateRequestAssignmentResource_WithRightKeys_StoresAndReturnsKeys()
    {
        var resource = await SeedUniqueResource();

        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, ["key-a", "key-b"], RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        var fetched = (await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(["key-a", "key-b"], created.RequestedRightKeys);
        Assert.Equal(["key-a", "key-b"], fetched.RequestedRightKeys);
    }

    [Fact]
    public async Task CreateRequestAssignmentResource_WithDuplicateRightKeys_StoresEachKeyOnceInGivenOrder()
    {
        var resource = await SeedUniqueResource();

        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, ["key-b", "key-a", "key-b"], RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(["key-b", "key-a"], created.RequestedRightKeys);
    }

    [Fact]
    public async Task CreateRequestAssignmentResource_WithEmptyRightKeys_StoresNull()
    {
        var resource = await SeedUniqueResource();

        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, [], RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        var stored = await _db.RequestAssignmentResources.AsNoTracking().SingleAsync(r => r.Id == created.Id, TestContext.Current.CancellationToken);

        Assert.Null(created.RequestedRightKeys);
        Assert.Null(stored.RequestedRightKeys);
    }

    [Fact]
    public async Task GetSentAndReceivedRequests_IncludeRightKeys()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, ["key-a"], RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        var sent = await _requestService.GetSentRequests(partyId: PersonTo.Id, toId: OrgFrom.Id, status: [RequestStatusV2.Pending], type: "resource", ct: TestContext.Current.CancellationToken);
        var received = await _requestService.GetReceivedRequests(partyId: OrgFrom.Id, fromId: PersonTo.Id, status: [RequestStatusV2.Pending], type: "resource", ct: TestContext.Current.CancellationToken);

        Assert.Equal(["key-a"], Assert.Single(sent.Value, r => r.Id == created.Id).RequestedRightKeys);
        Assert.Equal(["key-a"], Assert.Single(received.Value, r => r.Id == created.Id).RequestedRightKeys);
    }

    #endregion

    #region ApproveResourceRequest — status and approved right keys

    // The delegation is made by the caller; the service records the outcome on the request.
    private async Task<AccessRequest> CreatePendingResourceRequest(IEnumerable<string>? requestedRightKeys)
    {
        var resource = await SeedUniqueResource();
        return (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, requestedRightKeys, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
    }

    [Fact]
    public async Task ApproveResourceRequest_StoresStatusAndApprovedRightKeys()
    {
        var created = await CreatePendingResourceRequest(["key-a", "key-b"]);

        var result = await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.PartialApprove, ["key-b", "key-c", "key-b"], TestContext.Current.CancellationToken);
        var stored = await _db.RequestAssignmentResources.AsNoTracking().SingleAsync(r => r.Id == created.Id, TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatusV2.PartialApprove, result.Value.Status);
        Assert.Equal(["key-b", "key-c"], result.Value.ApprovedRightKeys);
        Assert.Equal(["key-a", "key-b"], result.Value.RequestedRightKeys);
        Assert.Equal(RequestStatusV2.PartialApprove, stored.Status);
        Assert.Equal(["key-b", "key-c"], stored.ApprovedRightKeys);
    }

    [Fact]
    public async Task ApproveResourceRequest_ByRequester_ReturnsProblemAndLeavesRequestPending()
    {
        var created = await CreatePendingResourceRequest(["key-a"]);

        var result = await _requestService.ApproveResourceRequest(PersonTo.Id, created.Id, RequestStatusV2.Approved, ["key-a"], TestContext.Current.CancellationToken);
        var stored = await _db.RequestAssignmentResources.AsNoTracking().SingleAsync(r => r.Id == created.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsProblem);
        Assert.Equal(RequestStatusV2.Pending, stored.Status);
        Assert.Null(stored.ApprovedRightKeys);
    }

    [Fact]
    public async Task ApproveResourceRequest_AlreadyPartlyApproved_ReturnsProblem()
    {
        var created = await CreatePendingResourceRequest(["key-a"]);
        await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.PartialApprove, ["key-b"], TestContext.Current.CancellationToken);

        var again = await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.Approved, ["key-a"], TestContext.Current.CancellationToken);
        var rejected = await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Rejected, TestContext.Current.CancellationToken);
        var stored = await _db.RequestAssignmentResources.AsNoTracking().SingleAsync(r => r.Id == created.Id, TestContext.Current.CancellationToken);

        Assert.True(again.IsProblem);
        Assert.True(rejected.IsProblem);
        Assert.Equal(RequestStatusV2.PartialApprove, stored.Status);
        Assert.Equal(["key-b"], stored.ApprovedRightKeys);
    }

    [Fact]
    public async Task ApproveResourceRequest_PackageRequest_ReturnsProblem()
    {
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Fishing.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        var result = await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.Approved, ["key-a"], TestContext.Current.CancellationToken);

        Assert.True(result.IsProblem);
    }

    [Theory]
    [InlineData(RequestStatusV2.Pending)]
    [InlineData(RequestStatusV2.Rejected)]
    [InlineData(RequestStatusV2.Withdrawn)]
    public async Task ApproveResourceRequest_WithStatusThatIsNotApproval_Throws(RequestStatusV2 status)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _requestService.ApproveResourceRequest(OrgFrom.Id, Guid.NewGuid(), status, ["key-a"], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApproveResourceRequest_PartialApprove_WritesReviewedNotification()
    {
        var created = await CreatePendingResourceRequest(["key-a"]);

        await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.PartialApprove, ["key-b"], TestContext.Current.CancellationToken);

        var resource = await GetReviewedResource(created.Resource.Id);
        Assert.Equal(RequestStatusV2.PartialApprove, resource.RequestStatus);
        Assert.True(resource.IsApproved);
    }

    #endregion

    #region Reviewed notification

    // One outbox message is kept per requester and recipient, and the other tests in this class use the same pair,
    // so these tests look for their own resource or package in the message rather than for the message itself.
    // Each test uses a package no other test here reviews, because a package's entry is overwritten by the latest review.
    private async Task<RequestReviewNotificationMessageResponse<Guid>> GetReviewedResource(Guid? resourceId)
    {
        var messages = await GetReviewedMessages();
        return Assert.Single(messages.SelectMany(m => m.Resources), r => r.Ref == resourceId);
    }

    private async Task<RequestReviewNotificationMessageResponse<Guid>> GetReviewedPackage(Guid packageId)
    {
        var messages = await GetReviewedMessages();
        return Assert.Single(messages.SelectMany(m => m.Packages), p => p.Ref == packageId);
    }

    private async Task<List<RequestReviewNotificationMessage>> GetReviewedMessages()
    {
        var outbox = await _db.OutboxMessages.AsNoTracking()
            .Where(m => m.Handler == RequestReviewedNotification.Handler)
            .ToListAsync(TestContext.Current.CancellationToken);
        return outbox.Select(m => JsonSerializer.Deserialize<RequestReviewNotificationMessage>(m.Data)).ToList();
    }

    [Fact]
    public async Task ApproveResourceRequest_Approved_WritesReviewedNotification()
    {
        var created = await CreatePendingResourceRequest(["key-a"]);

        await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.Approved, ["key-a"], TestContext.Current.CancellationToken);

        var resource = await GetReviewedResource(created.Resource.Id);
        Assert.Equal(RequestStatusV2.Approved, resource.RequestStatus);
        Assert.True(resource.IsApproved);
    }

    [Fact]
    public async Task UpdateRequest_RejectedResourceRequest_WritesReviewedNotification()
    {
        var created = await CreatePendingResourceRequest(["key-a"]);

        await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Rejected, TestContext.Current.CancellationToken);

        var resource = await GetReviewedResource(created.Resource.Id);
        Assert.Equal(RequestStatusV2.Rejected, resource.RequestStatus);
        Assert.False(resource.IsApproved);
    }

    [Fact]
    public async Task UpdateRequest_ApprovedPackageRequest_WritesReviewedNotification()
    {
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.AnimalHusbandry.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Approved, TestContext.Current.CancellationToken);

        var package = await GetReviewedPackage(PackageConstants.AnimalHusbandry.Id);
        Assert.Equal(RequestStatusV2.Approved, package.RequestStatus);
        Assert.True(package.IsApproved);
    }

    [Fact]
    public async Task UpdateRequest_RejectedPackageRequest_WritesReviewedNotification()
    {
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Aquaculture.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Rejected, TestContext.Current.CancellationToken);

        var package = await GetReviewedPackage(PackageConstants.Aquaculture.Id);
        Assert.Equal(RequestStatusV2.Rejected, package.RequestStatus);
        Assert.False(package.IsApproved);
    }

    // Runs the handler on the stored outbox message, so the status is followed from the request
    // through the database and into the email text.
    [Fact]
    public async Task RequestReviewedNotificationHandler_PartiallyApprovedResource_EmailSaysApprovedWithAdjustments()
    {
        var resourceName = $"Delvis godkjent tjeneste {Guid.CreateVersion7()}";
        var resource = await SeedUniqueResource(resourceName);
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, ["key-a"], RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        await _requestService.ApproveResourceRequest(OrgFrom.Id, created.Id, RequestStatusV2.PartialApprove, ["key-b"], TestContext.Current.CancellationToken);

        var outbox = await _db.OutboxMessages.AsNoTracking()
            .Where(m => m.Handler == RequestReviewedNotification.Handler)
            .ToListAsync(TestContext.Current.CancellationToken);
        var message = Assert.Single(outbox, m => m.Data.Contains(resource.Id.ToString(), StringComparison.OrdinalIgnoreCase));

        var notification = new CapturingNotification();
        var handler = new RequestReviewedNotificationHandler(_db, notification, new EnabledFeatureManager(), new EntityService(_db));

        var status = await handler.Handle(message, TestContext.Current.CancellationToken);

        Assert.Equal(OutboxStatus.Completed, status);
        var sent = Assert.Single(notification.Sent);
        var body = sent.Recipient.RecipientOrganization?.EmailSettings?.Body ?? sent.Recipient.RecipientPerson?.EmailSettings?.Body;
        Assert.Contains($"<li>{resourceName}. <strong><em>Godkjent med justeringer.</em></strong></li>", body);
        Assert.Contains("har svart på din forespørsel om fullmakter.", body);
    }

    #endregion

    #region GetRequest (aggregate)

    [Fact]
    public async Task GetRequest_WithResourceRequestId_ReturnsRequestDto()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;

        var result = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);

        Assert.Equal(created.Id, result.Value.Id);
        Assert.Equal(RequestStatusV2.Draft, result.Value.Status);
    }

    [Fact]
    public async Task GetRequest_WithPackageRequestId_ReturnsRequestDto()
    {
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;

        var result = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);

        Assert.Equal(created.Id, result.Value.Id);
        Assert.Equal(RequestStatusV2.Draft, result.Value.Status);
    }

    [Fact]
    public async Task GetRequest_WithNonExistingId_ReturnsNull()
    {
        var result = await _requestService.GetRequest(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        Assert.Null(result.Value);
    }
    #endregion

    #region GetRequests

    [Fact]
    public async Task GetSentRequests_ReturnsOnlyMatchingRequests()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        var results = await _requestService.GetSentRequests(partyId: PersonTo.Id, toId: OrgFrom.Id, status: null, type: null, ct: TestContext.Current.CancellationToken);

        Assert.NotEmpty(results.Value);
        Assert.All(results.Value, r => Assert.Equal(OrgFrom.Id, r.To.Id));
    }

    [Fact]
    public async Task GetReceivedRequests_ReturnsOnlyMatchingRequests()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;

        var results = await _requestService.GetReceivedRequests(partyId: OrgFrom.Id, fromId: PersonTo.Id, status: null, type: null, ct: TestContext.Current.CancellationToken);

        Assert.NotEmpty(results.Value);
        Assert.All(results.Value, r => Assert.Equal(PersonTo.Id, r.From.Id));
    }

    #endregion

    #region CreateRequestAssignmentPackage

    [Fact]
    public async Task CreateRequestAssignmentPackage_WithValidInput_CreatesDraftRequest()
    {
        var result = await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Draft, TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatusV2.Draft, result.Value.Status);
        Assert.Equal(PackageConstants.Agriculture.Id, result.Value.Package.Id);
    }

    [Fact]
    public async Task CreateRequestAssignmentPackage_CalledTwice_ReturnsExistingPendingRequest()
    {
        var first = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;
        var second = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(first.Id, second.Id);
    }
    #endregion

    #region Outbox

    [Fact]
    public async Task CreateResourceRequest_WithDraftStatus_DoesNotWriteToOutbox()
    {
        var testStart = DateTime.UtcNow;
        var resource = await SeedUniqueResource();
        await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Draft, TestContext.Current.CancellationToken);

        var refId = $"{RequestPendingNotification.Handler}_{PersonTo.Id}_{OrgFrom.Id}";
        var outbox = await _db.OutboxMessages.AsNoTracking().Where(o => o.RefId == refId && o.CreatedAt >= testStart).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(outbox);
    }

    [Fact]
    public async Task CreatePackageRequest_WithDraftStatus_DoesNotWriteToOutbox()
    {
        var testStart = DateTime.UtcNow;
        await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Draft, TestContext.Current.CancellationToken);

        var refId = $"{RequestPendingNotification.Handler}_{PersonTo.Id}_{OrgFrom.Id}";
        var outbox = await _db.OutboxMessages.AsNoTracking().Where(o => o.RefId == refId && o.CreatedAt >= testStart).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(outbox);
    }

    [Fact]
    public async Task CreateResourceRequest_WithPendingStatus_WritesToOutbox()
    {
        var resource = await SeedUniqueResource();
        await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Pending, TestContext.Current.CancellationToken);

        var refId = $"{RequestPendingNotification.Handler}_{PersonTo.Id}_{OrgFrom.Id}";
        var outbox = await _db.OutboxMessages.AsNoTracking().Where(o => o.RefId == refId).ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(outbox);
    }

    [Fact]
    public async Task CreatePackageRequest_WithPendingStatus_WritesToOutbox()
    {
        await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken);

        var refId = $"{RequestPendingNotification.Handler}_{PersonTo.Id}_{OrgFrom.Id}";
        var outbox = await _db.OutboxMessages.AsNoTracking().Where(o => o.RefId == refId).ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(outbox);
    }

    #endregion

    #region Full lifecycle scenarios

    [Fact]
    public async Task ResourceRequest_EnduserCreate_CreatesPendingRequest()
    {
        var resource = await SeedUniqueResource();
        var result = await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Pending, TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatusV2.Pending, result.Value.Status);
    }

    [Fact]
    public async Task ResourceRequest_FullLifecycle_DraftToPendingToAccepted()
    {
        var resource = await SeedUniqueResource();

        // 1. ServiceOwner creates request — status should be Draft
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;

        Assert.Equal(RequestStatusV2.Draft, created.Status);

        // 2. Enduser sets status to Pending (acknowledges the request)
        var pending = (await _requestService.UpdateRequest(PersonTo.Id, created.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Pending, pending.Status);

        // 3. ServiceOwner checks that status has changed from Draft
        var afterPending = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);
        Assert.NotEqual(RequestStatusV2.Draft, afterPending.Value.Status);
        Assert.Equal(RequestStatusV2.Pending, afterPending.Value.Status);

        // 4. Enduser fetches the request
        var fetched = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(created.Id, fetched.Value.Id);

        // 5. Enduser accepts
        var accepted = (await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Approved, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Approved, accepted.Status);
    }

    [Fact]
    public async Task PackageRequest_FullLifecycle_DraftToPendingToAccepted()
    {
        // 1. ServiceOwner creates request — status should be Draft
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Draft, created.Status);

        // 2. Enduser sets status to Pending (acknowledges the request)
        var pendingResult = await _requestService.UpdateRequest(PersonTo.Id, created.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken);
        Assert.False(pendingResult.IsProblem);
        Assert.Equal(RequestStatusV2.Pending, pendingResult.Value.Status);

        // 3. ServiceOwner checks that status has changed from Draft
        var afterPending = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);
        Assert.NotEqual(RequestStatusV2.Draft, afterPending.Value.Status);
        Assert.Equal(RequestStatusV2.Pending, afterPending.Value.Status);

        // 4. Enduser fetches the request
        var fetched = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(created.Id, fetched.Value.Id);

        // 5. Enduser accepts
        var accepted = (await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Approved, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Approved, accepted.Status);
    }

    [Fact]
    public async Task ResourceRequest_FullLifecycle_DraftToPendingToRejected()
    {
        var resource = await SeedUniqueResource();

        // 1. ServiceOwner creates request — status should be Draft
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, null, RequestStatusV2.Draft, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Draft, created.Status);

        // 2. Enduser sets status to Pending (acknowledges the request)
        var pending = (await _requestService.UpdateRequest(PersonTo.Id, created.Id, RequestStatusV2.Pending, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Pending, pending.Status);

        // 3. ServiceOwner checks that status has changed from Draft
        var afterPending = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);
        Assert.NotEqual(RequestStatusV2.Draft, afterPending.Value.Status);
        Assert.Equal(RequestStatusV2.Pending, afterPending.Value.Status);

        // 4. Enduser fetches the request
        var fetched = await _requestService.GetRequest(created.Id, TestContext.Current.CancellationToken);
        Assert.Equal(created.Id, fetched.Value.Id);

        // 5. Enduser rejects
        var rejected = (await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatusV2.Rejected, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(RequestStatusV2.Rejected, rejected.Status);
    }
    #endregion

    private sealed class EnabledFeatureManager : IFeatureManager
    {
        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<bool> IsEnabledAsync(string feature) => Task.FromResult(true);

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) => Task.FromResult(true);
    }

    private sealed class CapturingNotification : IAltinnNotification
    {
        public List<NotificationOrderChainRequestExt> Sent { get; } = [];

        public Task<PlatformResponse<NotificationOrderChainResponseExt>> Send(NotificationOrderChainRequestExt model, CancellationToken cancellationToken = default)
        {
            Sent.Add(model);
            return Task.FromResult(new PlatformResponse<NotificationOrderChainResponseExt> { IsSuccessful = true, StatusCode = HttpStatusCode.OK });
        }
    }

    private sealed class DisabledFeatureManager : IFeatureManager
    {
        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<bool> IsEnabledAsync(string feature) => Task.FromResult(false);

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) => Task.FromResult(false);
    }
}
