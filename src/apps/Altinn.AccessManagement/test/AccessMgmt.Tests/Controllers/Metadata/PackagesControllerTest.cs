using Altinn.AccessManagement.Api.Metadata.Controllers;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AccessMgmt.Tests.Controllers.Metadata;

public class PackagesControllerTest
{
    private static PackagesController CreateController(
        IPackageService packageService,
        ITranslationService translationService,
        string acceptLanguage = "en")
    {
        var controller = new PackagesController(packageService, translationService);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Language"] = acceptLanguage;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static Mock<ITranslationService> PassThroughTranslation()
    {
        var mock = new Mock<ITranslationService>();
        mock.Setup(x => x.TranslateAsync(It.IsAny<PackageDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((PackageDto dto, string _, bool __) => dto);
        mock.Setup(x => x.TranslateAsync(It.IsAny<AreaGroupDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((AreaGroupDto dto, string _, bool __) => dto);
        return mock;
    }

    // ── Search ──────────────────────────────────────────────────────────────
    //
    // The action branches on the new `simpleSearch` query parameter (default
    // true → SimpleSearch, false → FuzzySearch) and forwards the resolved
    // language code + partial-translation flag from the request to the
    // service. These tests guard the routing branch (calls the right method,
    // never the other) and the parameter-forwarding contract — bug classes
    // that no test covered after the FuzzySearch→SimpleSearch split.
    [Fact]
    public async Task Search_DefaultIsSimpleSearch_RoutesToSimpleSearchAndNotFuzzy()
    {
        var serviceMock = new Mock<IPackageService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.SimpleSearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchObject<PackageDto>>
            {
                new() { Object = new PackageDto { Id = Guid.NewGuid(), Name = "Tax" }, Score = 100 },
            });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.Search("Tax");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<SearchObject<PackageDto>>>(ok.Value);
        Assert.NotEmpty(items);

        // Strict mock would have thrown if FuzzySearch had been invoked.
        serviceMock.Verify(
            s => s.SimpleSearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_SimpleSearchFalse_RoutesToFuzzySearchAndNotSimple()
    {
        var serviceMock = new Mock<IPackageService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.FuzzySearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchObject<PackageDto>>
            {
                new() { Object = new PackageDto { Id = Guid.NewGuid(), Name = "Tax" }, Score = 0.9 },
            });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.Search("Tax", simpleSearch: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsAssignableFrom<IEnumerable<SearchObject<PackageDto>>>(ok.Value);
        serviceMock.Verify(
            s => s.FuzzySearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_PassesAcceptLanguageMappedTo3LetterCodeToService()
    {
        // Accept-Language "nb-NO" must reach the service as the mapped 3-letter
        // ISO 639-2 code "nob". A regression that hardcodes the default would
        // silently degrade i18n.
        string capturedLanguageCode = null;
        bool? capturedAllowPartial = null;

        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.SimpleSearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<string, List<string>, bool, Guid?, string, bool, CancellationToken>(
                (_, _, _, _, lang, allowPartial, _) =>
                {
                    capturedLanguageCode = lang;
                    capturedAllowPartial = allowPartial;
                })
            .ReturnsAsync(new List<SearchObject<PackageDto>>
            {
                new() { Object = new PackageDto { Id = Guid.NewGuid() }, Score = 1 },
            });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object, acceptLanguage: "nb-NO");

        await controller.Search("anything");

        Assert.Equal("nob", capturedLanguageCode);
        Assert.True(capturedAllowPartial); // Default when X-Accept-Partial-Translation header absent
    }

    [Fact]
    public async Task Search_FuzzySearchPath_AlsoForwardsLanguageContext()
    {
        // Same forwarding contract on the FuzzySearch branch; if the params get
        // dropped on one branch but not the other, locales diverge silently.
        string capturedLanguageCode = null;
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.FuzzySearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<string, List<string>, bool, Guid?, string, bool, CancellationToken>(
                (_, _, _, _, lang, _, _) => capturedLanguageCode = lang)
            .ReturnsAsync(new List<SearchObject<PackageDto>>
            {
                new() { Object = new PackageDto { Id = Guid.NewGuid() }, Score = 1 },
            });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object, acceptLanguage: "nb-NO");

        await controller.Search("anything", simpleSearch: false);

        Assert.Equal("nob", capturedLanguageCode);
    }

    [Fact]
    public async Task Search_WithValidTypeName_PassesResolvedTypeIdToService()
    {
        // Re-added after the FuzzySearch→SimpleSearch split removed the original test.
        // Bug class: typeName lookup not wired through the new branch — search would
        // silently ignore the type filter and return cross-type results.
        Guid? capturedTypeId = null;
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.SimpleSearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<string, List<string>, bool, Guid?, string, bool, CancellationToken>(
                (_, _, _, id, _, _, _) => capturedTypeId = id)
            .ReturnsAsync(new List<SearchObject<PackageDto>>
            {
                new() { Object = new PackageDto { Id = Guid.NewGuid() }, Score = 1 },
            });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        // "organization" is a known type name in EntityTypeConstants
        await controller.Search("Tax", typeName: "organization");

        Assert.NotNull(capturedTypeId);
        Assert.NotEqual(Guid.Empty, capturedTypeId.Value);
    }

    [Fact]
    public async Task Search_DefaultPath_WhenNoResults_Returns204NoContent()
    {
        // Default (simpleSearch=true) hits SimpleSearch — empty maps to NoContent.
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.SimpleSearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SearchObject<PackageDto>>());

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.Search("nothing");

        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task Search_DefaultPath_WhenServiceReturnsNull_Returns204NoContent()
    {
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.SimpleSearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SearchObject<PackageDto>>)null);

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.Search("x");

        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task Search_FuzzyPath_WhenNoResults_Returns204NoContent()
    {
        // Mirror of the SimpleSearch empty-result test, but explicitly on the
        // FuzzySearch branch — both paths must collapse empty/null to NoContent.
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.FuzzySearch(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SearchObject<PackageDto>>());

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.Search("nothing", simpleSearch: false);

        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithInvalidTypeName_ReturnsProblem()
    {
        var serviceMock = new Mock<IPackageService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.Search("x", typeName: "nonexistent_type_that_does_not_exist");

        Assert.IsType<ObjectResult>(result.Result);
        var objectResult = (ObjectResult)result.Result;
        Assert.Equal(500, objectResult.StatusCode); // Problem() defaults to 500
    }

    // ── GetHierarchy ────────────────────────────────────────────────────────
    [Fact]
    public async Task GetHierarchy_WhenResultsFound_Returns200Ok()
    {
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.GetHierarchy(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AreaGroupDto> { new AreaGroupDto { Id = Guid.NewGuid() } });

        var translationMock = PassThroughTranslation();
        translationMock.Setup(x => x.TranslateAsync(It.IsAny<AreaGroupDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((AreaGroupDto dto, string _, bool __) => dto);

        var controller = CreateController(serviceMock.Object, translationMock.Object);

        var result = await controller.GetHierarchy();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHierarchy_WhenEmpty_Returns204NoContent()
    {
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.GetHierarchy(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AreaGroupDto>());

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetHierarchy();

        Assert.IsType<NoContentResult>(result.Result);
    }

    // ── GetGroups ───────────────────────────────────────────────────────────
    [Fact]
    public async Task GetGroups_WhenResultsFound_Returns200Ok()
    {
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.GetAreaGroups(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AreaGroupDto> { new AreaGroupDto { Id = Guid.NewGuid() } });

        var translationMock = PassThroughTranslation();
        translationMock.Setup(x => x.TranslateAsync(It.IsAny<AreaGroupDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((AreaGroupDto dto, string _, bool __) => dto);

        var controller = CreateController(serviceMock.Object, translationMock.Object);

        var result = await controller.GetGroups();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGroups_WhenEmpty_Returns204NoContent()
    {
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.GetAreaGroups(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AreaGroupDto>());

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetGroups();

        Assert.IsType<NoContentResult>(result.Result);
    }

    // ── GetGroup ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetGroup_WhenFound_Returns200Ok()
    {
        var id = Guid.NewGuid();
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.GetAreaGroup(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AreaGroupDto { Id = id });

        var translationMock = PassThroughTranslation();
        translationMock.Setup(x => x.TranslateAsync(It.IsAny<AreaGroupDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((AreaGroupDto dto, string _, bool __) => dto);

        var controller = CreateController(serviceMock.Object, translationMock.Object);

        var result = await controller.GetGroup(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetGroup_WhenNotFound_Returns404()
    {
        var serviceMock = new Mock<IPackageService>();
        serviceMock.Setup(s => s.GetAreaGroup(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AreaGroupDto)null);

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetGroup(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
