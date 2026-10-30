using System.Net;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Enums;
using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Altinn.AccessMgmt.Core.Tests.Integration.HostedServices.Services.ResourceSyncServiceTest;

namespace Altinn.AccessMgmt.Core.Tests.Integration.HostedServices.Services;

/// <summary>
/// <see cref="Altinn.AccessMgmt.Core.HostedServices.Services.ResourceSyncService"/> with
/// <see cref="AccessMgmtFeatureFlags.HostedServicesResourceRegistrySyncChangeFeed"/> enabled: phase 1 reads
/// resource metadata from the change feed before phase 2 syncs subject relations from the updated feed.
/// The flag is set per fixture, so these tests live apart from <see cref="ResourceSyncServiceTest"/>.
/// </summary>
[IntegrationTest]
public class ResourceSyncServiceChangeFeedTest : IClassFixture<ApiFixture>
{
    private const string NextLink1 = "https://platform.test/resourceregistry/api/v1/resource/changes?token=one&limit=100";
    private const string NextLink2 = "https://platform.test/resourceregistry/api/v1/resource/changes?token=two&limit=100";

    private static readonly FakeResourceRegistry Registry = new();

    public ApiFixture Fixture { get; }

    public ResourceSyncServiceChangeFeedTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.HostedServicesResourceRegistrySyncChangeFeed);
        Fixture.ConfigureServices(services =>
        {
            services.RemoveAll<IAltinnResourceRegistry>();
            services.AddSingleton<IAltinnResourceRegistry>(Registry);
        });
    }

    [Fact]
    public async Task SyncResources_UpsertsResourceMetadata_WithoutSubjectChange()
    {
        var refId = "rsst-change-meta-" + Guid.NewGuid().ToString("N");
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, refId)];
        Registry.ResourceFactory = id => id == refId ? Success(NewResourceModel(refId)) : Problem<ResourceModel>();

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertResourceName(refId, "Resource " + refId);

        // A second change to the metadata only, with no subject change, updates the resource.
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink2, refId)];
        Registry.ResourceFactory = id =>
        {
            var model = NewResourceModel(refId);
            model.Title = new ResourceTitle { Nb = "Renamed " + refId };
            return Success(model);
        };

        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertResourceName(refId, "Renamed " + refId);
        Assert.Equal(NextLink2, lease.Data.ResourceChangesNextPageLink);
    }

    [Fact]
    public async Task SyncResources_ReadsChangeFeedBeforeUpdatedFeed_AndLinksNewResource()
    {
        var refId = "rsst-change-order-" + Guid.NewGuid().ToString("N");
        Registry.StreamCalls.Clear();
        Registry.GetResourceCalls.Clear();
        Registry.ResourceFactory = id => id == refId ? Success(NewResourceModel(refId)) : Problem<ResourceModel>();
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, refId)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(new ResourceUpdatedModel
        {
            SubjectUrn = "urn:altinn:rolecode:priv",
            ResourceUrn = "urn:altinn:resource:" + refId,
            UpdatedAt = DateTime.UtcNow,
        })];

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        Assert.Equal(["changes", "updated"], Registry.StreamCalls);

        // Only phase 1 fetches the resource; phase 2 links the subject to the resource it synced.
        Assert.Equal([refId], Registry.GetResourceCalls);
        await Fixture.QueryDb(async db =>
        {
            var roleResource = await db.RoleResources
                .AsNoTracking()
                .Include(r => r.Resource)
                .Include(r => r.Role)
                .FirstOrDefaultAsync(r => r.Resource.RefId == refId && r.Role.LegacyUrn == "urn:altinn:rolecode:priv", TestContext.Current.CancellationToken);
            Assert.NotNull(roleResource);
        });
        Assert.NotEqual(default, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_ResumesFromStoredLink_AndKeepsItWhenFeedHasNoMoreChanges()
    {
        var refId = "rsst-change-resume-" + Guid.NewGuid().ToString("N");
        string? requestedLink = null;
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ResourceFactory = id => id == refId ? Success(NewResourceModel(refId)) : Problem<ResourceModel>();
        Registry.ChangePagesFactory = nextPage =>
        {
            requestedLink = nextPage;
            return [ChangesPage(NextLink2, refId), ChangesPage(null)];
        };

        var lease = new FakeLease { Data = new ResourceRegistryLease { ResourceChangesNextPageLink = NextLink1 } };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // The run starts from the stored link, and the closing empty page without a next link
        // does not clear the position reached by the page before it.
        Assert.Equal(NextLink1, requestedLink);
        Assert.Equal(NextLink2, lease.Data.ResourceChangesNextPageLink);
        await AssertResourceName(refId, "Resource " + refId);
    }

    [Fact]
    public async Task SyncResources_SkipsUpdatedFeed_WhenChangeFeedPageIsProblem()
    {
        Registry.StreamCalls.Clear();
        Registry.ChangePagesFactory = _ => [Problem<PageStream<ResourceChangeModel>>()];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(new ResourceUpdatedModel
        {
            SubjectUrn = "urn:altinn:rolecode:priv",
            ResourceUrn = "urn:altinn:resource:rsst-never",
            UpdatedAt = DateTime.UtcNow,
        })];

        var lease = new FakeLease { Data = new ResourceRegistryLease { ResourceChangesNextPageLink = NextLink1 } };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        Assert.Equal(["changes"], Registry.StreamCalls);
        Assert.Equal(NextLink1, lease.Data.ResourceChangesNextPageLink);
        Assert.Equal(default, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_PersistsCursorOfLastSuccessfulPage_WhenLaterPageIsProblem()
    {
        var refId = "rsst-change-partial-" + Guid.NewGuid().ToString("N");
        Registry.StreamCalls.Clear();
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ResourceFactory = id => id == refId ? Success(NewResourceModel(refId)) : Problem<ResourceModel>();
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink2, refId), Problem<PageStream<ResourceChangeModel>>()];

        var lease = new FakeLease { Data = new ResourceRegistryLease { ResourceChangesNextPageLink = NextLink1 } };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // The first page is checkpointed, and the failing page stops the sync before phase 2.
        Assert.Equal(["changes"], Registry.StreamCalls);
        Assert.Equal(NextLink2, lease.Data.ResourceChangesNextPageLink);
        Assert.Equal(default, lease.Data.Since);
        await AssertResourceName(refId, "Resource " + refId);
    }

    [Fact]
    public async Task SyncResources_SkipsUpdatedFeed_WhenOpeningChangeFeedThrows()
    {
        Registry.StreamCalls.Clear();
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ChangePagesFactory = _ => throw new HttpRequestException("transport failure");

        var lease = new FakeLease { Data = new ResourceRegistryLease { ResourceChangesNextPageLink = NextLink1 } };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        Assert.Equal(["changes"], Registry.StreamCalls);
        Assert.Equal(NextLink1, lease.Data.ResourceChangesNextPageLink);
    }

    [Fact]
    public async Task SyncResources_StopsWithoutMovingCursor_WhenGetResourceFails()
    {
        var refId = "rsst-change-fail-" + Guid.NewGuid().ToString("N");
        Registry.StreamCalls.Clear();
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ResourceFactory = _ => Problem<ResourceModel>();
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink2, refId)];

        var lease = new FakeLease { Data = new ResourceRegistryLease { ResourceChangesNextPageLink = NextLink1 } };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // A failed lookup is retried on the next run instead of losing the change.
        Assert.Equal(["changes"], Registry.StreamCalls);
        Assert.Equal(NextLink1, lease.Data.ResourceChangesNextPageLink);
        await Fixture.QueryDb(async db =>
        {
            Assert.False(await db.Resources.AnyAsync(r => r.RefId == refId, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task SyncResources_SkipsChangedResource_WhenItNoLongerExists()
    {
        var goneRefId = "rsst-change-gone-" + Guid.NewGuid().ToString("N");
        var refId = "rsst-change-kept-" + Guid.NewGuid().ToString("N");
        Registry.StreamCalls.Clear();
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ResourceFactory = id => id == refId
            ? Success(NewResourceModel(refId))
            : new PlatformResponse<ResourceModel> { IsSuccessful = false, StatusCode = HttpStatusCode.NotFound };
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink2, goneRefId, refId)];

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        Assert.Equal(["changes", "updated"], Registry.StreamCalls);
        Assert.Equal(NextLink2, lease.Data.ResourceChangesNextPageLink);
        await AssertResourceName(refId, "Resource " + refId);
    }

    [Fact]
    public async Task SyncResources_FetchesAndLinksResource_PublishedAfterChangeFeedDrained()
    {
        // The resource and its policy were published after phase 1 drained, so only the updated feed has it,
        // and a resource that phase 1 did sync follows it on the same page.
        var lateRefId = "rsst-change-late-" + Guid.NewGuid().ToString("N");
        var syncedRefId = "rsst-change-synced-" + Guid.NewGuid().ToString("N");
        var syncedAt = DateTime.UtcNow;
        Registry.ResourceFactory = id => id == lateRefId || id == syncedRefId ? Success(NewResourceModel(id)) : Problem<ResourceModel>();
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, syncedRefId)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(RoleSubject(lateRefId, syncedAt.AddSeconds(-1)), RoleSubject(syncedRefId, syncedAt))];
        Registry.GetResourceCalls.Clear();

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // Phase 1 fetches the synced resource; phase 2 fetches only the one it was missing.
        Assert.Equal([syncedRefId, lateRefId], Registry.GetResourceCalls);
        await AssertRoleLinked(lateRefId, true);
        await AssertRoleLinked(syncedRefId, true);
        Assert.Equal(syncedAt, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_DoesNotCheckpointPage_WhenFetchingMissingResourceFails()
    {
        var lateRefId = "rsst-change-latefail-" + Guid.NewGuid().ToString("N");
        var syncedRefId = "rsst-change-syncedfail-" + Guid.NewGuid().ToString("N");
        var storedSince = DateTime.UtcNow.AddDays(-1);
        Registry.ResourceFactory = id => id == syncedRefId ? Success(NewResourceModel(id)) : Problem<ResourceModel>();
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, syncedRefId)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(RoleSubject(lateRefId, DateTime.UtcNow), RoleSubject(syncedRefId, DateTime.UtcNow))];

        var lease = new FakeLease { Data = new ResourceRegistryLease { Since = storedSince } };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // The failed fetch stops the page before the cursor moves past the relation.
        Assert.Equal(storedSince, lease.Data.Since);
        Assert.Null(lease.Data.ResourceNextPageLink);
        await AssertRoleLinked(lateRefId, false);
        await AssertRoleLinked(syncedRefId, false);

        // The next run replays the page once the resource registry answers.
        Registry.ResourceFactory = id => id == lateRefId || id == syncedRefId ? Success(NewResourceModel(id)) : Problem<ResourceModel>();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertRoleLinked(lateRefId, true);
        await AssertRoleLinked(syncedRefId, true);
        Assert.NotEqual(storedSince, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_SkipsSubject_WhenMissingResourceNoLongerExists()
    {
        var goneRefId = "rsst-change-subjectgone-" + Guid.NewGuid().ToString("N");
        var syncedRefId = "rsst-change-subjectkept-" + Guid.NewGuid().ToString("N");
        var syncedAt = DateTime.UtcNow;
        Registry.ResourceFactory = id => id == syncedRefId
            ? Success(NewResourceModel(id))
            : new PlatformResponse<ResourceModel> { IsSuccessful = false, StatusCode = HttpStatusCode.NotFound };
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, syncedRefId)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(RoleSubject(goneRefId, syncedAt.AddSeconds(-1)), RoleSubject(syncedRefId, syncedAt))];

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertRoleLinked(goneRefId, false);
        await AssertRoleLinked(syncedRefId, true);
        Assert.Equal(syncedAt, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_SkipsSubject_WhenMissingResourceHasUnknownServiceOwner()
    {
        var unknownRefId = "rsst-change-unknownorg-" + Guid.NewGuid().ToString("N");
        var syncedRefId = "rsst-change-knownorg-" + Guid.NewGuid().ToString("N");
        var syncedAt = DateTime.UtcNow;
        Registry.ResourceFactory = id =>
        {
            var model = NewResourceModel(id);
            if (id == unknownRefId)
            {
                model.HasCompetentAuthority.Orgcode = "rsst-unknown-org";
            }

            return Success(model);
        };
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, syncedRefId)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(RoleSubject(unknownRefId, syncedAt.AddSeconds(-1)), RoleSubject(syncedRefId, syncedAt))];

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            Assert.False(await db.Resources.AnyAsync(r => r.RefId == unknownRefId, TestContext.Current.CancellationToken));
        });
        await AssertRoleLinked(syncedRefId, true);
        Assert.Equal(syncedAt, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_DoesNotFetchMissingResource_ForDeletedSubject()
    {
        var refId = "rsst-change-deletemissing-" + Guid.NewGuid().ToString("N");
        var deletedAt = DateTime.UtcNow;
        Registry.GetResourceCalls.Clear();
        Registry.ChangePagesFactory = _ => [];
        Registry.ResourceFactory = id => Success(NewResourceModel(id));
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(new ResourceUpdatedModel
        {
            SubjectUrn = "urn:altinn:rolecode:priv",
            ResourceUrn = "urn:altinn:resource:" + refId,
            UpdatedAt = deletedAt,
            Deleted = true,
        })];

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // No link can exist for a resource that was never synced, so there is nothing to fetch or delete.
        Assert.Empty(Registry.GetResourceCalls);
        await Fixture.QueryDb(async db =>
        {
            Assert.False(await db.Resources.AnyAsync(r => r.RefId == refId, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task SyncResources_DeletesSubjectLink_WithoutFetchingResource()
    {
        var refId = "rsst-change-delete-" + Guid.NewGuid().ToString("N");
        var subject = new ResourceUpdatedModel
        {
            SubjectUrn = "urn:altinn:rolecode:priv",
            ResourceUrn = "urn:altinn:resource:" + refId,
            UpdatedAt = DateTime.UtcNow,
        };

        // First run: phase 1 syncs the resource, phase 2 links the role.
        Registry.ResourceFactory = id => id == refId ? Success(NewResourceModel(refId)) : Problem<ResourceModel>();
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, refId)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(subject)];
        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        // Second run: no metadata change, and the role is removed from the policy.
        Registry.GetResourceCalls.Clear();
        Registry.ChangePagesFactory = _ => [ChangesPage(null)];
        Registry.StreamPagesFactory = (_, _) => [UpdatedPage(new ResourceUpdatedModel
        {
            SubjectUrn = subject.SubjectUrn,
            ResourceUrn = subject.ResourceUrn,
            UpdatedAt = DateTime.UtcNow,
            Deleted = true,
        })];
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        Assert.Empty(Registry.GetResourceCalls);
        Assert.Equal(NextLink1, lease.Data.ResourceChangesNextPageLink);
        await Fixture.QueryDb(async db =>
        {
            Assert.True(await db.Resources.AnyAsync(r => r.RefId == refId, TestContext.Current.CancellationToken));
            Assert.False(await db.RoleResources.AnyAsync(r => r.Resource.RefId == refId, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task SyncResources_ImportsDelegableAccessListModeAndScopes_ForMaskinportenSchema()
    {
        var refId = "rsst-change-scopes-" + Guid.NewGuid().ToString("N");
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, refId)];
        Registry.ResourceFactory = id => Success(MaskinportenResourceModel(id, delegable: false, "Enabled", "test:scope.a", "test:scope.b"));

        await ResolveService().SyncResources(new FakeLease(), TestContext.Current.CancellationToken);

        await AssertResourceFields(refId, delegable: false, ResourceAccessListMode.Enabled, "test:scope.a", "test:scope.b");
    }

    [Fact]
    public async Task SyncResources_UpdatesDelegableAccessListModeAndScopes_OnLaterChange()
    {
        var refId = "rsst-change-scopesupdate-" + Guid.NewGuid().ToString("N");
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, refId)];
        Registry.ResourceFactory = id => Success(MaskinportenResourceModel(id, delegable: true, "Disabled", "test:scope.a", "test:scope.b"));

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);
        await AssertResourceFields(refId, delegable: true, ResourceAccessListMode.Disabled, "test:scope.a", "test:scope.b");

        // One scope is removed and one added, and the other fields change.
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink2, refId)];
        Registry.ResourceFactory = id => Success(MaskinportenResourceModel(id, delegable: false, "Enabled", "test:scope.b", "test:scope.c"));
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertResourceFields(refId, delegable: false, ResourceAccessListMode.Enabled, "test:scope.b", "test:scope.c");
    }

    [Fact]
    public async Task SyncResources_ImportsNoScopes_ForOtherResourceTypes_AndRemovesThemWhenTypeChanges()
    {
        var otherRefId = "rsst-change-noscopes-" + Guid.NewGuid().ToString("N");
        var changedRefId = "rsst-change-typechange-" + Guid.NewGuid().ToString("N");
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, otherRefId, changedRefId)];
        Registry.ResourceFactory = id =>
        {
            var model = MaskinportenResourceModel(id, delegable: true, null, "test:scope.a");
            if (id == otherRefId)
            {
                model.ResourceType = "GenericAccessResource";
            }

            return Success(model);
        };

        var lease = new FakeLease();
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertResourceFields(otherRefId, delegable: true, ResourceAccessListMode.Disabled);
        await AssertResourceFields(changedRefId, delegable: true, ResourceAccessListMode.Disabled, "test:scope.a");

        // A resource that is no longer a Maskinporten schema keeps no scopes, even with scope references.
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink2, changedRefId)];
        Registry.ResourceFactory = id =>
        {
            var model = MaskinportenResourceModel(id, delegable: true, null, "test:scope.a");
            model.ResourceType = "GenericAccessResource";
            return Success(model);
        };
        await ResolveService().SyncResources(lease, TestContext.Current.CancellationToken);

        await AssertResourceFields(changedRefId, delegable: true, ResourceAccessListMode.Disabled);
    }

    [Theory]
    [InlineData("Enabled", ResourceAccessListMode.Enabled)]
    [InlineData("enabled", ResourceAccessListMode.Enabled)]
    [InlineData("Disabled", ResourceAccessListMode.Disabled)]
    [InlineData(null, ResourceAccessListMode.Disabled)]
    [InlineData("SomethingNew", ResourceAccessListMode.Disabled)]
    [InlineData("7", ResourceAccessListMode.Disabled)]
    public async Task SyncResources_MapsAccessListMode(string? registryValue, ResourceAccessListMode expected)
    {
        var refId = "rsst-change-accesslistmode-" + Guid.NewGuid().ToString("N");
        Registry.StreamPagesFactory = (_, _) => [];
        Registry.ChangePagesFactory = _ => [ChangesPage(NextLink1, refId)];
        Registry.ResourceFactory = id => Success(MaskinportenResourceModel(id, delegable: true, registryValue));

        await ResolveService().SyncResources(new FakeLease(), TestContext.Current.CancellationToken);

        await AssertResourceFields(refId, delegable: true, expected);
    }

    private async Task AssertResourceFields(string refId, bool delegable, ResourceAccessListMode accessListMode, params string[] scopes)
    {
        await Fixture.QueryDb(async db =>
        {
            var resource = await db.Resources
                .AsNoTracking()
                .Include(r => r.Scopes)
                .SingleOrDefaultAsync(r => r.RefId == refId, TestContext.Current.CancellationToken);
            Assert.NotNull(resource);
            Assert.Equal(delegable, resource.Delegable);
            Assert.Equal(accessListMode, resource.AccessListMode);
            Assert.Equal(scopes.Order(), resource.Scopes.Select(s => s.Scope).Order());
        });
    }

    private async Task AssertResourceName(string refId, string expectedName)
    {
        await Fixture.QueryDb(async db =>
        {
            var resource = await db.Resources.AsNoTracking().SingleOrDefaultAsync(r => r.RefId == refId, TestContext.Current.CancellationToken);
            Assert.NotNull(resource);
            Assert.Equal(expectedName, resource.Name);
        });
    }

    private async Task AssertRoleLinked(string refId, bool expected)
    {
        await Fixture.QueryDb(async db =>
        {
            var linked = await db.RoleResources
                .AsNoTracking()
                .AnyAsync(r => r.Resource.RefId == refId && r.Role.LegacyUrn == "urn:altinn:rolecode:priv", TestContext.Current.CancellationToken);
            Assert.Equal(expected, linked);
        });
    }

    private static ResourceUpdatedModel RoleSubject(string refId, DateTime updatedAt) => new()
    {
        SubjectUrn = "urn:altinn:rolecode:priv",
        ResourceUrn = "urn:altinn:resource:" + refId,
        UpdatedAt = updatedAt,
    };

    private IResourceSyncService ResolveService()
    {
        using var scope = Fixture.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceSyncService>();
    }

    private static PlatformResponse<PageStream<ResourceChangeModel>> ChangesPage(string? next, params string[] resourceIds) => Success(new PageStream<ResourceChangeModel>
    {
        Stats = new PageStream<ResourceChangeModel>.StatsStream(),
        Links = new PageStream<ResourceChangeModel>.LinksStream { Next = next },
        Data = [.. resourceIds.Select(id => new ResourceChangeModel { ResourceId = id, ChangedAt = DateTimeOffset.UtcNow })],
    });

    private static PlatformResponse<PageStream<ResourceUpdatedModel>> UpdatedPage(params ResourceUpdatedModel[] items) => Success(new PageStream<ResourceUpdatedModel>
    {
        Stats = new PageStream<ResourceUpdatedModel>.StatsStream(),
        Links = new PageStream<ResourceUpdatedModel>.LinksStream { Next = null },
        Data = items,
    });
}
