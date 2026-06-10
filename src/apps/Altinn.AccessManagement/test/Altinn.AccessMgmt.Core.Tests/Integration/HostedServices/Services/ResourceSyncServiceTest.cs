using System.Net;
using System.Runtime.CompilerServices;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.AccessMgmt.Core.Tests.Integration.HostedServices.Services;

/// <summary>
/// <see cref="Altinn.AccessMgmt.Core.HostedServices.Services.ResourceSyncService"/>
/// </summary>
[IntegrationTest]
public class ResourceSyncServiceTest : IClassFixture<ApiFixture>
{
    private static readonly FakeResourceRegistry Registry = new();

    public ApiFixture Fixture { get; }

    public ResourceSyncServiceTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.ConfigureServices(services =>
        {
            services.RemoveAll<IAltinnResourceRegistry>();
            services.AddSingleton<IAltinnResourceRegistry>(Registry);
        });
    }

    [Fact]
    public async Task SyncResourceOwners_WhenRegistryFails_ReturnsFalse()
    {
        Registry.ServiceOwnersResponse = Problem<ServiceOwners>();

        var svc = ResolveService();
        var result = await svc.SyncResourceOwners(TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task SyncResourceOwners_InsertsAndUpdatesProviders()
    {
        var newId = Guid.NewGuid();
        var existingId = Guid.NewGuid();

        await Fixture.QueryDb(async db =>
        {
            db.Providers.Add(new Provider
            {
                Id = existingId,
                Name = "Existing before",
                RefId = "100000001",
                Code = "rsst-existing",
                LogoUrl = "http://old-logo",
                TypeId = ProviderTypeConstants.ServiceOwner,
            });
            await db.SaveChangesAsync(new AuditValues(SystemEntityConstants.StaticDataIngest), TestContext.Current.CancellationToken);
        });

        Registry.ServiceOwnersResponse = Success(new ServiceOwners
        {
            Orgs = new Dictionary<string, ServiceOwner>
            {
                ["rsst-new"] = new()
                {
                    Id = newId,
                    Name = new ServiceOwnerName { Nb = "Brand new owner" },
                    Logo = "http://new-logo",
                    Orgnr = "100000002",
                },
                ["rsst-existing"] = new()
                {
                    Id = existingId,
                    Name = new ServiceOwnerName { Nb = "Existing renamed" },
                    Logo = "http://new-logo-2",
                    Orgnr = "100000003",
                },
            },
        });

        var svc = ResolveService();
        var result = await svc.SyncResourceOwners(TestContext.Current.CancellationToken);

        Assert.True(result);
        await Fixture.QueryDb(async db =>
        {
            var inserted = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == newId, TestContext.Current.CancellationToken);
            Assert.NotNull(inserted);
            Assert.Equal("Brand new owner", inserted.Name);

            var updated = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == existingId, TestContext.Current.CancellationToken);
            Assert.NotNull(updated);
            Assert.Equal("Existing renamed", updated.Name);
            Assert.Equal("http://new-logo-2", updated.LogoUrl);
        });
    }

    [Fact]
    public async Task SyncResourceOwners_LeavesProviderUntouched_WhenAllFieldsMatch()
    {
        var providerId = Guid.NewGuid();
        var name = "Stable provider";

        await Fixture.QueryDb(async db =>
        {
            db.Providers.Add(new Provider
            {
                Id = providerId,
                Name = name,
                RefId = "100000010",
                Code = "rsst-stable",
                LogoUrl = "http://stable-logo",
                TypeId = ProviderTypeConstants.ServiceOwner,
            });
            await db.SaveChangesAsync(new AuditValues(SystemEntityConstants.StaticDataIngest), TestContext.Current.CancellationToken);
        });

        Registry.ServiceOwnersResponse = Success(new ServiceOwners
        {
            Orgs = new Dictionary<string, ServiceOwner>
            {
                ["rsst-stable"] = new()
                {
                    Id = providerId,
                    Name = new ServiceOwnerName { Nb = name },
                    Logo = "http://stable-logo",
                    Orgnr = "100000010",
                },
            },
        });

        var svc = ResolveService();
        var result = await svc.SyncResourceOwners(TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task SyncResources_WhenStreamPageIsProblem_ReturnsEarly()
    {
        Registry.StreamPagesFactory = (_, _) => [Problem<PageStream<ResourceUpdatedModel>>()];

        var lease = new FakeLease();
        var svc = ResolveService();
        await svc.SyncResources(lease, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SyncResources_UpsertsRoleCodeAndAccessPackageSubjects()
    {
        var roleRefId = "rsst-resource-role-" + Guid.NewGuid().ToString("N");
        var packageRefId = "rsst-resource-pkg-" + Guid.NewGuid().ToString("N");

        Registry.ResourceFactory = identifier => identifier switch
        {
            string s when s == roleRefId => Success(NewResourceModel(roleRefId)),
            string s when s == packageRefId => Success(NewResourceModel(packageRefId)),
            _ => Problem<ResourceModel>(),
        };

        Registry.StreamPagesFactory = (_, _) => [Success(new PageStream<ResourceUpdatedModel>
        {
            Stats = new PageStream<ResourceUpdatedModel>.StatsStream(),
            Links = new PageStream<ResourceUpdatedModel>.LinksStream { Next = null },
            Data =
            [
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:rolecode:priv",
                    ResourceUrn = "urn:altinn:resource:" + roleRefId,
                    UpdatedAt = DateTime.UtcNow,
                },
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:accesspackage:jordbruk",
                    ResourceUrn = "urn:altinn:resource:" + packageRefId,
                    UpdatedAt = DateTime.UtcNow,
                },
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:unknown:something",
                    ResourceUrn = "urn:altinn:resource:" + roleRefId,
                    UpdatedAt = DateTime.UtcNow,
                },
            ],
        })];

        var lease = new FakeLease();
        var svc = ResolveService();
        await svc.SyncResources(lease, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var roleResource = await db.RoleResources
                .AsNoTracking()
                .Include(r => r.Resource)
                .Include(r => r.Role)
                .FirstOrDefaultAsync(r => r.Resource.RefId == roleRefId && r.Role.LegacyUrn == "urn:altinn:rolecode:priv", TestContext.Current.CancellationToken);
            Assert.NotNull(roleResource);

            var pkgResource = await db.PackageResources
                .AsNoTracking()
                .Include(r => r.Resource)
                .Include(r => r.Package)
                .FirstOrDefaultAsync(r => r.Resource.RefId == packageRefId && r.Package.Urn == "urn:altinn:accesspackage:jordbruk", TestContext.Current.CancellationToken);
            Assert.NotNull(pkgResource);
        });

        Assert.NotEqual(default, lease.Data.Since);
    }

    [Fact]
    public async Task SyncResources_DeletesRoleCodeAndAccessPackageLinks_WhenDeletedTrue()
    {
        var roleRefId = "rsst-del-role-" + Guid.NewGuid().ToString("N");
        var packageRefId = "rsst-del-pkg-" + Guid.NewGuid().ToString("N");

        // First create resources + links via a normal upsert pass.
        Registry.ResourceFactory = identifier => identifier == roleRefId
            ? Success(NewResourceModel(roleRefId))
            : identifier == packageRefId ? Success(NewResourceModel(packageRefId)) : Problem<ResourceModel>();

        Registry.StreamPagesFactory = (_, _) => [Success(new PageStream<ResourceUpdatedModel>
        {
            Stats = new PageStream<ResourceUpdatedModel>.StatsStream(),
            Links = new PageStream<ResourceUpdatedModel>.LinksStream { Next = null },
            Data =
            [
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:rolecode:priv",
                    ResourceUrn = "urn:altinn:resource:" + roleRefId,
                    UpdatedAt = DateTime.UtcNow,
                },
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:accesspackage:jordbruk",
                    ResourceUrn = "urn:altinn:resource:" + packageRefId,
                    UpdatedAt = DateTime.UtcNow,
                },
            ],
        })];

        await ResolveService().SyncResources(new FakeLease(), TestContext.Current.CancellationToken);

        // Then run a second pass with Deleted=true.
        Registry.StreamPagesFactory = (_, _) => [Success(new PageStream<ResourceUpdatedModel>
        {
            Stats = new PageStream<ResourceUpdatedModel>.StatsStream(),
            Links = new PageStream<ResourceUpdatedModel>.LinksStream { Next = null },
            Data =
            [
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:rolecode:priv",
                    ResourceUrn = "urn:altinn:resource:" + roleRefId,
                    UpdatedAt = DateTime.UtcNow,
                    Deleted = true,
                },
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:accesspackage:jordbruk",
                    ResourceUrn = "urn:altinn:resource:" + packageRefId,
                    UpdatedAt = DateTime.UtcNow,
                    Deleted = true,
                },
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:unknown:x",
                    ResourceUrn = "urn:altinn:resource:" + roleRefId,
                    UpdatedAt = DateTime.UtcNow,
                    Deleted = true,
                },
            ],
        })];

        await ResolveService().SyncResources(new FakeLease(), TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var roleResource = await db.RoleResources
                .AsNoTracking()
                .Include(r => r.Resource)
                .FirstOrDefaultAsync(r => r.Resource.RefId == roleRefId, TestContext.Current.CancellationToken);
            Assert.Null(roleResource);

            var pkgResource = await db.PackageResources
                .AsNoTracking()
                .Include(r => r.Resource)
                .FirstOrDefaultAsync(r => r.Resource.RefId == packageRefId, TestContext.Current.CancellationToken);
            Assert.Null(pkgResource);
        });
    }

    [Fact]
    public async Task SyncResources_SkipsResource_WhenResourceUrnHasUnknownPrefix()
    {
        var refId = "rsst-noprefix-" + Guid.NewGuid().ToString("N");
        Registry.ResourceFactory = _ => Problem<ResourceModel>();
        Registry.StreamPagesFactory = (_, _) => [Success(new PageStream<ResourceUpdatedModel>
        {
            Stats = new PageStream<ResourceUpdatedModel>.StatsStream(),
            Links = new PageStream<ResourceUpdatedModel>.LinksStream { Next = null },
            Data =
            [
                new ResourceUpdatedModel
                {
                    SubjectUrn = "urn:altinn:rolecode:priv",
                    ResourceUrn = "totally-not-a-urn:" + refId,
                    UpdatedAt = DateTime.UtcNow,
                },
            ],
        })];

        await ResolveService().SyncResources(new FakeLease(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SyncResources_ReturnsEarly_WhenResourceProcessingThrows()
    {
        var refId = "rsst-throw-" + Guid.NewGuid().ToString("N");
        Registry.ResourceFactory = _ => Success(NewResourceModel(refId));
        Registry.StreamPagesFactory = (_, _) => [Success(new PageStream<ResourceUpdatedModel>
        {
            Stats = new PageStream<ResourceUpdatedModel>.StatsStream(),
            Links = new PageStream<ResourceUpdatedModel>.LinksStream { Next = null },
            Data =
            [
                new ResourceUpdatedModel
                {
                    // No role exists with this LegacyUrn/Urn → UpsertRoleCodeResource throws KeyNotFoundException.
                    SubjectUrn = "urn:altinn:rolecode:does-not-exist-" + Guid.NewGuid().ToString("N"),
                    ResourceUrn = "urn:altinn:resource:" + refId,
                    UpdatedAt = DateTime.UtcNow,
                },
            ],
        })];

        await ResolveService().SyncResources(new FakeLease(), TestContext.Current.CancellationToken);
    }

    private IResourceSyncService ResolveService()
    {
        using var scope = Fixture.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceSyncService>();
    }

    private static ResourceModel NewResourceModel(string identifier) => new()
    {
        Identifier = identifier,
        Title = new ResourceTitle { Nb = "Resource " + identifier },
        Description = new ResourceDescription { Nb = "Description for " + identifier },
        HasCompetentAuthority = new ResourceHasCompetentAuthority
        {
            Orgcode = "sys-altinn3",
            Organization = "974760673",
        },
        ResourceType = "GenericAccessResource",
    };

    private static PlatformResponse<T> Success<T>(T content) => new()
    {
        IsSuccessful = true,
        StatusCode = HttpStatusCode.OK,
        Content = content,
    };

    private static PlatformResponse<T> Problem<T>() => new()
    {
        IsSuccessful = false,
        StatusCode = HttpStatusCode.InternalServerError,
    };

    private sealed class FakeResourceRegistry : IAltinnResourceRegistry
    {
        public PlatformResponse<ServiceOwners> ServiceOwnersResponse { get; set; } = new() { IsSuccessful = true, Content = new ServiceOwners { Orgs = new Dictionary<string, ServiceOwner>() } };

        public Func<string, PlatformResponse<ResourceModel>> ResourceFactory { get; set; } = _ => new() { IsSuccessful = false };

        public Func<DateTime, string?, IEnumerable<PlatformResponse<PageStream<ResourceUpdatedModel>>>> StreamPagesFactory { get; set; } = (_, _) => [];

        public Task<PlatformResponse<ServiceOwners>> GetServiceOwners(CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceOwnersResponse);

        public Task<PlatformResponse<ResourceModel>> GetResource(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ResourceFactory(id));

        public Task<PlatformResponse<List<ResourceModel>>> GetResources(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IAsyncEnumerable<PlatformResponse<PageStream<ResourceUpdatedModel>>>> StreamResources(DateTime since = default, string nextPage = null, CancellationToken cancellationToken = default)
        {
            var pages = StreamPagesFactory(since, nextPage);
            return Task.FromResult(ToAsync(pages, cancellationToken));
        }

        private static async IAsyncEnumerable<PlatformResponse<PageStream<ResourceUpdatedModel>>> ToAsync(
            IEnumerable<PlatformResponse<PageStream<ResourceUpdatedModel>>> pages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var page in pages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return page;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeLease : ILease
    {
        public ResourceRegistryLease Data { get; set; } = new();

        public ValueTask DisposeAsync() => default;

        public Task<T> Get<T>(CancellationToken cancellationToken = default)
            where T : class, new()
        {
            if (Data is T typed)
            {
                return Task.FromResult(typed);
            }

            return Task.FromResult(new T());
        }

        public Task Update<T>(T data, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            if (data is ResourceRegistryLease lease)
            {
                Data = lease;
            }

            return Task.CompletedTask;
        }

        public Task Update<T>(Action<T> configureData, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            if (Data is T typed)
            {
                configureData(typed);
            }

            return Task.CompletedTask;
        }
    }
}
