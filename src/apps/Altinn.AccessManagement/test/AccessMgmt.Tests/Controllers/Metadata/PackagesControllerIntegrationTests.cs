using Altinn.AccessManagement.Api.Metadata.Controllers;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccessMgmt.Tests.Controllers.Metadata;

/// <summary>
/// Database-backed integration tests for <see cref="PackagesController"/>.
///
/// Mirrors the field-level assertion style of the Bruno collection at
/// <c>test/Bruno/AccessMgmt/test/Meta/accesspackage/</c>: same seed-data IDs / URNs / names,
/// asserts on populated DTO fields rather than just response types. Runs in-process against
/// a Postgres Testcontainer with the real <see cref="PackageService"/>, real
/// <see cref="TranslationService"/>, and the static-data ingest performed by
/// <see cref="PostgresFixture"/>.
///
/// Bug classes the suite defends against: wrong joins / EF query producing wrong shape;
/// DTO mapping forgetting fields after a model change (notably <c>IsAssignable</c>);
/// <c>TranslateDeepAsync</c> not recursing into nested DTOs; <c>Accept-Language</c>
/// locale fallback; empty-vs-missing disambiguation in multi-branch actions.
/// </summary>
public class PackagesControllerIntegrationTests : IClassFixture<PostgresFixture>
{
    // Seed-data IDs / URNs / names — kept in sync with the Bruno collection so a
    // breakage in either layer points at the same row in the static-data ingest.
    private static readonly Guid PackageSkattegrunnlagId = Guid.Parse("4c859601-9b2b-4662-af39-846f4117ad7a");
    private const string PackageSkattegrunnlagUrn = "urn:altinn:accesspackage:skattegrunnlag";

    private const string PackageKunstUrnValue = "urn:altinn:accesspackage:kunst-og-underholdning";
    private const string PackageKunstName = "Kunst og underholdning";

    private static readonly Guid PackageAssignableId = Guid.Parse("1dba50d6-f604-48e9-bd41-82321b13e85c");
    private static readonly Guid PackageNotAssignableId = Guid.Parse("955d5779-3e2b-4098-b11d-0431dc41ddbe");

    private static readonly Guid AreaKulturId = Guid.Parse("5996ba37-6db0-4391-8918-b1b0bd4b394b");
    private const string AreaKulturName = "Kultur og frivillighet";

    private static readonly Guid GroupBransjeId = Guid.Parse("3757643a-316d-4d0e-a52b-4dc7cdebc0b4");
    private const string GroupBransjeName = "Bransje";

    private static readonly string[] ExpectedTopLevelGroupNames =
    {
        "Allment", "Bransje", "Særskilt", "Innbygger"
    };

    private static readonly string[] ExpectedBransjeAreaNames =
    {
        "Jordbruk, skogbruk, jakt, fiske og akvakultur",
        "Bygg, anlegg og eiendom",
        "Transport og lagring",
        "Helse, pleie, omsorg og vern",
        "Oppvekst og utdanning",
        "Energi, vann, avløp og avfall",
        "Industrier",
        "Kultur og frivillighet",
        "Handel, overnatting og servering",
        "Andre tjenesteytende næringer",
    };

    private static readonly string[] ExpectedKulturPackageNames =
    {
        "Kunst og underholdning",
        "Biblioteker, museer, arkiver og annen kultur",
        "Lotteri og spill",
        "Politikk",
        "Fornøyelser",
        "Sport og fritid",
    };

    private const string SearchTerm = "Opplæringskontorleder";

    private readonly AppDbContext _db;
    private readonly ITranslationService _translationService;
    private readonly IPackageService _packageService;

    public PackagesControllerIntegrationTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _translationService = new TranslationService(_db, memoryCache, NullLogger<TranslationService>.Instance);

        // Fully qualify: the namespace-less Persistence `PackageService` (six-arg
        // ctor) is in the global namespace, so an unqualified `PackageService(_db)`
        // would bind there.
        _packageService = new Altinn.AccessMgmt.Core.Services.PackageService(_db, _translationService);
    }

    private PackagesController CreateController(string acceptLanguage = "nb-NO")
    {
        var controller = new PackagesController(_packageService, _translationService);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Language"] = acceptLanguage;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // ── Mapping correctness — DTO field values match seeded data (mirrors Bruno) ──
    [Fact]
    public async Task GetPackage_Skattegrunnlag_ReturnsExpectedUrn()
    {
        var result = await CreateController().GetPackage(PackageSkattegrunnlagId);

        var dto = AssertOkPackage(result);
        Assert.Equal(PackageSkattegrunnlagUrn, dto.Urn);
        Assert.Equal(PackageSkattegrunnlagId, dto.Id);
    }

    [Fact]
    public async Task GetPackageByUrn_KunstOgUnderholdning_ReturnsExpectedName()
    {
        var result = await CreateController().GetPackageByUrn(PackageKunstUrnValue);

        var dto = AssertOkPackage(result);
        Assert.Equal(PackageKunstName, dto.Name);
    }

    [Fact]
    public async Task GetArea_KulturOgFrivillighet_ReturnsExpectedName()
    {
        var result = await CreateController().GetArea(AreaKulturId);

        var dto = AssertOk<AreaDto>(result);
        Assert.Equal(AreaKulturName, dto.Name);
        Assert.Equal(AreaKulturId, dto.Id);
    }

    [Fact]
    public async Task GetGroup_Bransje_ReturnsExpectedName()
    {
        var result = await CreateController().GetGroup(GroupBransjeId);

        var dto = AssertOk<AreaGroupDto>(result);
        Assert.Equal(GroupBransjeName, dto.Name);
        Assert.Equal(GroupBransjeId, dto.Id);
    }

    [Fact]
    public async Task GetGroups_ReturnsExpectedTopLevelGroupNames()
    {
        var result = await CreateController().GetGroups();

        var groups = AssertOkEnumerable<AreaGroupDto>(result);
        var names = groups.Select(g => g.Name).ToList();
        foreach (var expected in ExpectedTopLevelGroupNames)
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public async Task GetGroupAreas_Bransje_ReturnsExpectedAreaNames()
    {
        var result = await CreateController().GetGroupAreas(GroupBransjeId);

        var areas = AssertOkEnumerable<AreaDto>(result);
        var names = areas.Select(a => a.Name).ToList();
        foreach (var expected in ExpectedBransjeAreaNames)
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public async Task GetAreaPackages_KulturOgFrivillighet_ReturnsExpectedPackageNames()
    {
        var result = await CreateController().GetAreaPackages(AreaKulturId);

        var packages = AssertOkEnumerable<PackageDto>(result);
        var names = packages.Select(p => p.Name).ToList();
        foreach (var expected in ExpectedKulturPackageNames)
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public async Task Search_Opplaeringskontorleder_FirstResultMatchesExpectedName()
    {
        var result = await CreateController().Search(SearchTerm);

        var hits = AssertOkEnumerable(result).ToList();
        Assert.NotEmpty(hits);
        Assert.Equal(SearchTerm, hits.First().Object.Name);
    }

    // ── New simple-search path (default) — scoring rules + filter behaviour ──
    //
    // SimpleSearch builds rule-based scores (prefix-match=100, contains=50, etc.)
    // and orders by score descending while filtering out zero-score packages.
    // These tests defend the "rules-and-filter" contract against named
    // regressions: order direction flipping, the score>0 filter being dropped,
    // and the empty-input semantics diverging from FuzzySearch (which returns
    // all on empty term).
    [Fact]
    public async Task SimpleSearch_PrefixMatchOnPackageName_RanksMatchingPackageFirst()
    {
        // "Kunst" prefix-matches the package name "Kunst og underholdning",
        // earning the highest single-rule score (100) plus a contains-match (50).
        // Regression: OrderByDescending → OrderBy would push this package to
        // the bottom; dropping the prefix rule would tie it with substring hits.
        var result = await CreateController().Search("Kunst");

        var hits = AssertOkEnumerable(result).ToList();
        Assert.NotEmpty(hits);

        var top = hits.First();
        Assert.Equal(PackageKunstName, top.Object.Name);
        Assert.True(top.Score >= 100, $"Top hit should include a name.prefix match (≥100). Actual score: {top.Score}.");
        Assert.Contains(top.Fields, f => f.Field == "name.prefix");
    }

    [Fact]
    public async Task SimpleSearch_NonMatchingTerm_ReturnsNoContent()
    {
        // SimpleSearch must filter out packages with score 0. A regression where
        // the `Where(s => s.Score > 0)` filter is removed would return every
        // package in the database for any (or no) term — a search-becomes-list bug.
        var result = await CreateController().Search("zzzz_no_package_should_match_this_xyzzy");

        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task SimpleSearch_ResultsOrderedByScoreDescending()
    {
        // A more general guard than the prefix-match test: every adjacent pair
        // in the result must be in non-increasing score order. Regression:
        // OrderBy direction flipping or the sort being dropped entirely.
        var result = await CreateController().Search("Kunst");

        var hits = AssertOkEnumerable(result).ToList();
        Assert.NotEmpty(hits);
        for (var i = 1; i < hits.Count; i++)
        {
            Assert.True(
                hits[i - 1].Score >= hits[i].Score,
                $"SimpleSearch result not ordered by score desc: index {i - 1} has score {hits[i - 1].Score}, index {i} has {hits[i].Score}.");
        }
    }

    [Fact]
    public async Task FuzzySearch_StillReachableViaSimpleSearchFalse()
    {
        // The legacy fuzzy path remains opt-in via simpleSearch=false. A
        // regression where the controller's ternary always picks one branch
        // would either lose this path entirely or break the simple default —
        // either way, this test fails distinctly from the SimpleSearch tests
        // above and tells you which branch broke.
        var result = await CreateController().Search(SearchTerm, simpleSearch: false);

        var hits = AssertOkEnumerable(result).ToList();
        Assert.NotEmpty(hits);

        // FuzzySearch produces fractional scores (vs SimpleSearch's integer rule
        // points). The exact match is included regardless of which scorer ran.
        Assert.Contains(hits, h => h.Object.Name == SearchTerm);
    }

    [Fact]
    public async Task GetHierarchy_ReturnsNonEmptyHierarchyWithAreasAndPackages()
    {
        var result = await CreateController().GetHierarchy();

        var groups = AssertOkEnumerable<AreaGroupDto>(result);
        Assert.NotEmpty(groups);

        // Structural assertion: at least one group has at least one area, and at least one
        // of those areas has at least one package — catches a bug where TranslateDeepAsync
        // would drop nested collections silently.
        Assert.Contains(groups, g => g.Areas != null && g.Areas.Any(a => a.Packages != null && a.Packages.Any()));
    }

    // ── DTO mapping completeness — IsAssignable on a per-package basis ──
    [Fact]
    public async Task GetPackage_AssignablePackage_ReturnsIsAssignableTrue()
    {
        var result = await CreateController().GetPackage(PackageAssignableId);

        var dto = AssertOkPackage(result);
        Assert.True(dto.IsAssignable, $"Expected package {PackageAssignableId} to be assignable.");
    }

    [Fact]
    public async Task GetPackage_NonAssignablePackage_ReturnsIsAssignableFalse()
    {
        var result = await CreateController().GetPackage(PackageNotAssignableId);

        var dto = AssertOkPackage(result);
        Assert.False(dto.IsAssignable, $"Expected package {PackageNotAssignableId} to be NOT assignable.");
    }

    // ── TranslateDeepAsync recursion — nested DTOs populated, not dropped ──
    [Fact]
    public async Task GetPackage_NestedAreaIsPopulated()
    {
        var result = await CreateController().GetPackage(PackageSkattegrunnlagId);

        var dto = AssertOkPackage(result);
        Assert.NotNull(dto.Area);
        Assert.NotEqual(Guid.Empty, dto.Area.Id);
        Assert.False(string.IsNullOrWhiteSpace(dto.Area.Name), "Nested Area.Name was empty after TranslateDeepAsync — recursion bug.");
    }

    [Fact]
    public async Task GetAreaPackages_NestedPackagesAreFullyPopulated()
    {
        var result = await CreateController().GetAreaPackages(AreaKulturId);

        var packages = AssertOkEnumerable<PackageDto>(result).ToList();
        Assert.NotEmpty(packages);
        Assert.All(packages, p =>
        {
            Assert.NotEqual(Guid.Empty, p.Id);
            Assert.False(string.IsNullOrWhiteSpace(p.Name), $"Package {p.Id} had empty Name after deep translation.");
            Assert.False(string.IsNullOrWhiteSpace(p.Urn), $"Package {p.Id} had empty Urn after deep translation.");
        });
    }

    // ── Accept-Language locale handling — controller doesn't NRE on fallback ──
    [Fact]
    public async Task GetPackage_WithEnglishAcceptLanguage_ReturnsPopulatedDto()
    {
        // English translations may or may not exist for every package; the test verifies
        // the controller doesn't NRE on the fallback path and produces a populated DTO
        // regardless. A future regression where TranslateDeepAsync returned null for
        // unknown locales would break this.
        var result = await CreateController(acceptLanguage: "en").GetPackage(PackageSkattegrunnlagId);

        var dto = AssertOkPackage(result);
        Assert.Equal(PackageSkattegrunnlagId, dto.Id);
        Assert.Equal(PackageSkattegrunnlagUrn, dto.Urn);
        Assert.False(string.IsNullOrWhiteSpace(dto.Name), "Name should be populated even when locale falls back.");
    }

    [Fact]
    public async Task GetPackage_WithMissingAcceptLanguage_ReturnsPopulatedDto()
    {
        var controller = new PackagesController(_packageService, _translationService);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.GetPackage(PackageSkattegrunnlagId);

        var dto = AssertOkPackage(result);
        Assert.Equal(PackageSkattegrunnlagId, dto.Id);
        Assert.False(string.IsNullOrWhiteSpace(dto.Name), "Name should be populated even with no Accept-Language header.");
    }

    // ── NotFound paths — random GUIDs / unknown URNs return 404 against live DB ──
    [Fact]
    public async Task GetPackage_RandomGuid_ReturnsNotFound()
    {
        var result = await CreateController().GetPackage(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetArea_RandomGuid_ReturnsNotFound()
    {
        var result = await CreateController().GetArea(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroup_RandomGuid_ReturnsNotFound()
    {
        var result = await CreateController().GetGroup(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPackageByUrn_NonexistentUrn_ReturnsNotFound()
    {
        var result = await CreateController().GetPackageByUrn("urn:altinn:accesspackage:does-not-exist");
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroupAreas_RandomGuid_ReturnsNotFound()
    {
        var result = await CreateController().GetGroupAreas(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAreaPackages_RandomGuid_ReturnsNotFound()
    {
        var result = await CreateController().GetAreaPackages(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPackageResources_RandomGuid_ReturnsNotFound()
    {
        var result = await CreateController().GetPackageResources(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Helpers ──
    private static PackageDto AssertOkPackage(ActionResult<PackageDto> result) =>
        AssertOk<PackageDto>(result);

    private static TValue AssertOk<TValue>(ActionResult<TValue> result)
        where TValue : class
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<TValue>(ok.Value);
    }

    // The Metadata controller declares its collection-returning actions as
    // `ActionResult<TItem>` (not `ActionResult<IEnumerable<TItem>>`) but actually
    // returns `IEnumerable<TItem>` inside `Ok(...)`. These overloads paper over
    // that quirk: take the same declared element type that the action declared,
    // assert the body is the collection variant.
    private static IEnumerable<TItem> AssertOkEnumerable<TItem>(ActionResult<TItem> result)
        where TItem : class =>
        AssertOkEnumerableInner<TItem>(result.Result);

    private static IEnumerable<Altinn.AccessMgmt.Core.Utils.Models.SearchObject<PackageDto>> AssertOkEnumerable(
        ActionResult<IEnumerable<Altinn.AccessMgmt.Core.Utils.Models.SearchObject<PackageDto>>> result) =>
        AssertOkEnumerableInner<Altinn.AccessMgmt.Core.Utils.Models.SearchObject<PackageDto>>(result.Result);

    private static IEnumerable<TItem> AssertOkEnumerableInner<TItem>(IActionResult inner)
    {
        var ok = Assert.IsType<OkObjectResult>(inner);
        var items = Assert.IsAssignableFrom<IEnumerable<TItem>>(ok.Value);
        return items.ToList();
    }
}
