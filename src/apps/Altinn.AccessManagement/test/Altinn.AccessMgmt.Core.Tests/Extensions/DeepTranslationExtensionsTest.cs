using System.Collections.Concurrent;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;

// See: overhaul part-2 step 16
namespace Altinn.AccessMgmt.Core.Tests.Extensions;

/// <summary>
/// Pure-unit tests for <see cref="DeepTranslationExtensions"/>. Pins
/// recursion topology across the nested DTO graph (PackageDto → AreaDto →
/// AreaGroupDto, ResourceDto → ProviderDto → ProviderTypeDto, TypeDto →
/// ProviderDto, RoleDto → ProviderDto) and the *deep-vs-shallow* pattern
/// where some children (PackageDto.Type as TypeDto, TypeDto.Provider) are
/// recursed into deep, while leaf children (ProviderDto.Type as
/// ProviderTypeDto, ResourceDto.Type as ResourceTypeDto) are translated
/// via the singular <see cref="ITranslationService.TranslateAsync"/> only.
/// </summary>
public class DeepTranslationExtensionsTest
{
    private sealed class CountingTranslationService : ITranslationService
    {
        public ConcurrentDictionary<Type, int> Counts { get; } = new();

        public ValueTask<T> TranslateAsync<T>(T source, string languageCode, bool allowPartial = true)
        {
            Counts.AddOrUpdate(typeof(T), 1, (_, n) => n + 1);
            return ValueTask.FromResult(source);
        }

        public T Translate<T>(T source, string languageCode, bool allowPartial = true) => source;

        public ValueTask<(bool Success, T Result)> TryTranslateAsync<T>(T source, string languageCode)
            => ValueTask.FromResult((true, source));

        public ValueTask<IEnumerable<T>> TranslateCollectionAsync<T>(IEnumerable<T> sources, string languageCode, bool allowPartial = true)
        {
            // Collection-level call counts as one call against the element type
            // (recorded under typeof(T) so Of<T>() reflects both singular and
            // collection paths). The deep extensions typically iterate and call
            // the singular method per element instead, so this branch is mostly
            // exercised by direct collection-API tests.
            Counts.AddOrUpdate(typeof(T), 1, (_, n) => n + 1);
            return ValueTask.FromResult(sources);
        }

        public Task UpsertTranslationAsync(TranslationEntry e, Guid c, Guid s, CancellationToken ct = default) => Task.CompletedTask;

        public int Of<T>() => Counts.TryGetValue(typeof(T), out var n) ? n : 0;
    }

    // ── Null short-circuits (parametric across the public surface) ────────────
    [Fact]
    public async Task TranslateDeepAsync_Package_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((PackageDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_Area_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((AreaDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_AreaGroup_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((AreaGroupDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_Resource_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((ResourceDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_Provider_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((ProviderDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_Type_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((TypeDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_Role_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((RoleDto?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    [Fact]
    public async Task TranslateDeepAsync_PackageCollection_Null_NoServiceCalls()
    {
        var svc = new CountingTranslationService();
        Assert.Null(await ((IEnumerable<PackageDto>?)null).TranslateDeepAsync(svc, "nob"));
        Assert.Empty(svc.Counts);
    }

    // ── Topology: PackageDto recurses into Area, Type, Resources ──────────────
    [Fact]
    public async Task TranslateDeepAsync_Package_NoNested_TranslatesPackageOnly()
    {
        var svc = new CountingTranslationService();
        var pkg = new PackageDto { Name = "p" };

        await pkg.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<PackageDto>());
        Assert.Equal(0, svc.Of<AreaDto>());
        Assert.Equal(0, svc.Of<TypeDto>());
        Assert.Equal(0, svc.Of<ResourceDto>());
    }

    [Fact]
    public async Task TranslateDeepAsync_Package_WithNestedArea_RecursesIntoArea()
    {
        var svc = new CountingTranslationService();
        var pkg = new PackageDto { Area = new AreaDto { Group = new AreaGroupDto() } };

        await pkg.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<PackageDto>());
        Assert.Equal(1, svc.Of<AreaDto>());
        Assert.Equal(1, svc.Of<AreaGroupDto>()); // Area recurses into its Group
    }

    [Fact]
    public async Task TranslateDeepAsync_Package_WithNestedType_RecursesIntoType()
    {
        // PackageDto → TypeDto.TranslateDeepAsync → recurses into Type.Provider (deep)
        var svc = new CountingTranslationService();
        var pkg = new PackageDto { Type = new TypeDto { Provider = new ProviderDto() } };

        await pkg.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<PackageDto>());
        Assert.Equal(1, svc.Of<TypeDto>());
        Assert.Equal(1, svc.Of<ProviderDto>()); // Type recurses into its Provider
    }

    [Fact]
    public async Task TranslateDeepAsync_Package_WithResources_RecursesIntoEachResource()
    {
        var svc = new CountingTranslationService();
        var pkg = new PackageDto
        {
            Resources = new[]
            {
                new ResourceDto(),
                new ResourceDto { Provider = new ProviderDto() },
            },
        };

        await pkg.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<PackageDto>());
        Assert.Equal(2, svc.Of<ResourceDto>());     // 2 resources
        Assert.Equal(1, svc.Of<ProviderDto>());     // 2nd resource has provider
    }

    // ── Topology: AreaDto cycle avoidance — does NOT re-translate Package.Area ──
    [Fact]
    public async Task TranslateDeepAsync_Area_WithNestedPackages_DoesNotReRecurseIntoPackagesArea()
    {
        // Cycle hazard: Area → Packages → Package.Area → Packages → ...
        // The deep extension translates each child package's Type and Resources
        // but explicitly does NOT recurse into the package's Area (which would
        // be the parent). Pinning that early-stop keeps the recursion finite.
        var svc = new CountingTranslationService();
        var inner = new PackageDto
        {
            Area = new AreaDto { Group = new AreaGroupDto() }, // would loop if recursed
            Type = new TypeDto(),
        };
        var area = new AreaDto { Packages = [inner] };

        await area.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<AreaDto>());            // outer area only — NOT the inner package's area
        Assert.Equal(1, svc.Of<PackageDto>());         // the inner package is translated
        Assert.Equal(1, svc.Of<TypeDto>());            // its Type is recursed into
        Assert.Equal(0, svc.Of<AreaGroupDto>());       // BUT the inner package's Area.Group is not — cycle avoided
    }

    [Fact]
    public async Task TranslateDeepAsync_AreaGroup_WithAreas_TranslatesEach()
    {
        var svc = new CountingTranslationService();
        var grp = new AreaGroupDto
        {
            Areas =
            [
                new AreaDto(),
                new AreaDto { Group = new AreaGroupDto() },
            ],
        };

        await grp.TranslateDeepAsync(svc, "nob");

        Assert.Equal(2, svc.Of<AreaGroupDto>()); // root group + 2nd area's group
        Assert.Equal(2, svc.Of<AreaDto>());      // both areas
    }

    // ── Provider→Type (ProviderTypeDto leaf) is shallow; Type→Provider is deep ─
    [Fact]
    public async Task TranslateDeepAsync_Provider_WithType_TranslatesProviderTypeShallow()
    {
        // ProviderDto.TranslateDeepAsync uses TranslateAsync (shallow) for its
        // Type, which is ProviderTypeDto — a leaf with no nested children. So
        // exactly one ProviderTypeDto translation is recorded and no further
        // recursion occurs through this path.
        var svc = new CountingTranslationService();
        var provider = new ProviderDto
        {
            Type = new ProviderTypeDto(),
        };

        await provider.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<ProviderDto>());     // the outer provider
        Assert.Equal(1, svc.Of<ProviderTypeDto>()); // shallow translation of Type
        Assert.Equal(0, svc.Of<TypeDto>());         // ProviderDto.Type is NOT TypeDto
    }

    [Fact]
    public async Task TranslateDeepAsync_Type_WithProvider_RecursesIntoProvider()
    {
        var svc = new CountingTranslationService();
        var type = new TypeDto { Provider = new ProviderDto() };

        await type.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<TypeDto>());
        Assert.Equal(1, svc.Of<ProviderDto>()); // deep recursion
    }

    [Fact]
    public async Task TranslateDeepAsync_Resource_TypeIsResourceTypeDto_TranslatedShallowOnly()
    {
        // ResourceDto.Type is ResourceTypeDto (a leaf — no nested children),
        // translated via the singular TranslateAsync.
        var svc = new CountingTranslationService();
        var resource = new ResourceDto
        {
            Provider = new ProviderDto { Type = new ProviderTypeDto() },
            Type = new ResourceTypeDto(),
        };

        await resource.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<ResourceDto>());
        Assert.Equal(1, svc.Of<ProviderDto>());      // deep into provider
        Assert.Equal(1, svc.Of<ProviderTypeDto>());  // shallow via Provider.Type
        Assert.Equal(1, svc.Of<ResourceTypeDto>());  // Resource.Type translated
    }

    // ── RoleDto recurses into Provider (deep) ────────────────────────────────
    [Fact]
    public async Task TranslateDeepAsync_Role_WithProvider_RecursesIntoProvider()
    {
        var svc = new CountingTranslationService();
        var role = new RoleDto
        {
            Provider = new ProviderDto { Type = new ProviderTypeDto() },
        };

        await role.TranslateDeepAsync(svc, "nob");

        Assert.Equal(1, svc.Of<RoleDto>());
        Assert.Equal(1, svc.Of<ProviderDto>());     // deep into role's provider
        Assert.Equal(1, svc.Of<ProviderTypeDto>()); // shallow via Provider.Type
    }

    // ── Collection variants ───────────────────────────────────────────────────
    [Fact]
    public async Task TranslateDeepAsync_PackageCollection_TranslatesEachItem()
    {
        var svc = new CountingTranslationService();
        IEnumerable<PackageDto> packages = [new PackageDto(), new PackageDto(), new PackageDto()];

        var result = (await packages.TranslateDeepAsync(svc, "nob")).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(3, svc.Of<PackageDto>());
    }

    [Fact]
    public async Task TranslateDeepAsync_AreaCollection_TranslatesEachItem()
    {
        var svc = new CountingTranslationService();
        IEnumerable<AreaDto> areas = [new AreaDto(), new AreaDto()];

        var result = (await areas.TranslateDeepAsync(svc, "nob")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, svc.Of<AreaDto>());
    }

    [Fact]
    public async Task TranslateDeepAsync_RoleCollection_TranslatesEachItem()
    {
        var svc = new CountingTranslationService();
        IEnumerable<RoleDto> roles = [new RoleDto(), new RoleDto()];

        var result = (await roles.TranslateDeepAsync(svc, "nob")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, svc.Of<RoleDto>());
    }
}
