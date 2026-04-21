using System.Security.Cryptography;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AccessMgmt.Tests.Services;

public class RequestServiceTests : IClassFixture<PostgresFixture>
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

    private readonly AppDbContext _db;
    private readonly RequestService _requestService;

    private readonly IOptions<CoreAppsettings> _coreSettings;

    public RequestServiceTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
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

        SeedSharedData(_db).GetAwaiter().GetResult();

        _requestService = new RequestService(_db, sp.GetRequiredService<IOptions<CoreAppsettings>>());
    }

    private static async Task SeedSharedData(AppDbContext db)
    {
        db.Entities.AddRange(OrgFrom, PersonTo);
        db.ResourceTypes.Add(TestResourceType);

        try
        {
            await db.SaveChangesAsync(TestAudit);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        db.ChangeTracker.Clear();
    }

    /// <summary>
    /// Seeds a unique <see cref="Resource"/> per test so create/update operations don't interfere with each other.
    /// </summary>
    private async Task<Resource> SeedUniqueResource()
    {
        var resource = new Resource
        {
            Id = Guid.CreateVersion7(),
            Name = "UniqueTestResource",
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
        var result = await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Draft);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatus.Draft, result.Value.Status);
        Assert.Equal(resource.Id, result.Value.Resource.Id);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public async Task CreateRequestAssignmentResource_CalledTwice_ReturnsExistingPendingRequest()
    {
        var resource = await SeedUniqueResource();

        var first = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Pending)).Value;
        var second = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Pending)).Value;

        Assert.Equal(first.Id, second.Id);
    }
    #endregion

    #region GetRequest (aggregate)

    [Fact]
    public async Task GetRequest_WithResourceRequestId_ReturnsRequestDto()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Draft)).Value;

        var result = await _requestService.GetRequest(created.Id);

        Assert.Equal(created.Id, result.Value.Id);
        Assert.Equal(RequestStatus.Draft, result.Value.Status);
    }

    [Fact]
    public async Task GetRequest_WithPackageRequestId_ReturnsRequestDto()
    {        
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatus.Draft)).Value;

        var result = await _requestService.GetRequest(created.Id);

        Assert.Equal(created.Id, result.Value.Id);
        Assert.Equal(RequestStatus.Draft, result.Value.Status);
    }

    [Fact]
    public async Task GetRequest_WithNonExistingId_ReturnsNull()
    {
        var result = await _requestService.GetRequest(Guid.CreateVersion7());

        Assert.Null(result.Value);
    }
    #endregion

    #region GetRequests

    [Fact]
    public async Task GetSentRequests_ReturnsOnlyMatchingRequests()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Pending)).Value;

        var results = await _requestService.GetSentRequests(partyId: PersonTo.Id, toId: OrgFrom.Id, status: null, type: null, ct: default);

        Assert.NotEmpty(results.Value);
        Assert.All(results.Value, r => Assert.Equal(OrgFrom.Id, r.To.Id));
    }

    [Fact]
    public async Task GetReceivedRequests_ReturnsOnlyMatchingRequests()
    {
        var resource = await SeedUniqueResource();
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Pending)).Value;

        var results = await _requestService.GetReceivedRequests(partyId: OrgFrom.Id, fromId: PersonTo.Id, status: null, type: null, ct: default);

        Assert.NotEmpty(results.Value);
        Assert.All(results.Value, r => Assert.Equal(PersonTo.Id, r.From.Id));
    }

    #endregion

    #region CreateRequestAssignmentPackage
    
    [Fact]
    public async Task CreateRequestAssignmentPackage_WithValidInput_CreatesDraftRequest()
    {
        var result = await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatus.Draft);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatus.Draft, result.Value.Status);
        Assert.Equal(PackageConstants.Agriculture.Id, result.Value.Package.Id);
    }

    [Fact]
    public async Task CreateRequestAssignmentPackage_CalledTwice_ReturnsExistingPendingRequest()
    {
        var first = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatus.Draft)).Value;
        var second = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatus.Draft)).Value;

        Assert.Equal(first.Id, second.Id);
    }
    #endregion

    #region Full lifecycle scenarios

    [Fact]
    public async Task ResourceRequest_EnduserCreate_CreatesPendingRequest()
    {
        var resource = await SeedUniqueResource();
        var result = await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Pending);

        Assert.False(result.IsProblem);
        Assert.Equal(RequestStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task ResourceRequest_FullLifecycle_DraftToPendingToAccepted()
    {
        var resource = await SeedUniqueResource();

        // 1. ServiceOwner creates request — status should be Draft
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Draft)).Value;

        Assert.Equal(RequestStatus.Draft, created.Status);

        // 2. Enduser sets status to Pending (acknowledges the request)
        var pending = (await _requestService.UpdateRequest(PersonTo.Id, created.Id, RequestStatus.Pending)).Value;
        Assert.Equal(RequestStatus.Pending, pending.Status);

        // 3. ServiceOwner checks that status has changed from Draft
        var afterPending = await _requestService.GetRequest(created.Id);
        Assert.NotEqual(RequestStatus.Draft, afterPending.Value.Status);
        Assert.Equal(RequestStatus.Pending, afterPending.Value.Status);

        // 4. Enduser fetches the request
        var fetched = await _requestService.GetRequest(created.Id);
        Assert.Equal(created.Id, fetched.Value.Id);

        // 5. Enduser accepts
        var accepted = (await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatus.Approved)).Value;
        Assert.Equal(RequestStatus.Approved, accepted.Status);
    }

    [Fact]
    public async Task PackageRequest_FullLifecycle_DraftToPendingToAccepted()
    {
        // 1. ServiceOwner creates request — status should be Draft
        var created = (await _requestService.CreatePackageRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, PackageConstants.Agriculture.Id, RequestStatus.Draft)).Value;
        Assert.Equal(RequestStatus.Draft, created.Status);

        // 2. Enduser sets status to Pending (acknowledges the request)
        var pendingResult = await _requestService.UpdateRequest(PersonTo.Id, created.Id, RequestStatus.Pending);
        Assert.False(pendingResult.IsProblem);
        Assert.Equal(RequestStatus.Pending, pendingResult.Value.Status);

        // 3. ServiceOwner checks that status has changed from Draft
        var afterPending = await _requestService.GetRequest(created.Id);
        Assert.NotEqual(RequestStatus.Draft, afterPending.Value.Status);
        Assert.Equal(RequestStatus.Pending, afterPending.Value.Status);

        // 4. Enduser fetches the request
        var fetched = await _requestService.GetRequest(created.Id);
        Assert.Equal(created.Id, fetched.Value.Id);

        // 5. Enduser accepts
        var accepted = (await _requestService.UpdateRequest(OrgFrom.Id,created.Id, RequestStatus.Approved)).Value;
        Assert.Equal(RequestStatus.Approved, accepted.Status);
    }

    [Fact]
    public async Task ResourceRequest_FullLifecycle_DraftToPendingToRejected()
    {
        var resource = await SeedUniqueResource();

        // 1. ServiceOwner creates request — status should be Draft
        var created = (await _requestService.CreateResourceRequest(OrgFrom.Id, PersonTo.Id, PersonTo.Id, RoleConstants.Rightholder.Id, resource.Id, RequestStatus.Draft)).Value;
        Assert.Equal(RequestStatus.Draft, created.Status);

        // 2. Enduser sets status to Pending (acknowledges the request)
        var pending = (await _requestService.UpdateRequest(PersonTo.Id, created.Id, RequestStatus.Pending)).Value;
        Assert.Equal(RequestStatus.Pending, pending.Status);

        // 3. ServiceOwner checks that status has changed from Draft
        var afterPending = await _requestService.GetRequest(created.Id);
        Assert.NotEqual(RequestStatus.Draft, afterPending.Value.Status);
        Assert.Equal(RequestStatus.Pending, afterPending.Value.Status);

        // 4. Enduser fetches the request
        var fetched = await _requestService.GetRequest(created.Id);
        Assert.Equal(created.Id, fetched.Value.Id);

        // 5. Enduser rejects
        var rejected = (await _requestService.UpdateRequest(OrgFrom.Id, created.Id, RequestStatus.Rejected)).Value;
        Assert.Equal(RequestStatus.Rejected, rejected.Status);
    }
    #endregion
}
