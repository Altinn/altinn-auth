using Altinn.AccessManagement.Api.Metadata.Controllers;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AccessMgmt.Tests.Controllers.Metadata;

public class RolesControllerTest
{
    // A valid role code that exists in RoleConstants (rettighetshaver).
    private const string KnownRoleCode = "rettighetshaver";

    // A valid entity variant name that exists in EntityVariantConstants (UTBG).
    private const string KnownVariantName = "UTBG";

    private static RolesController CreateController(
        IRoleService roleService,
        ITranslationService translationService,
        string acceptLanguage = "en")
    {
        var controller = new RolesController(roleService, translationService);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Language"] = acceptLanguage;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    /// <summary>
    /// Pass-through translation mock: every TranslateAsync call returns the input object unchanged.
    /// </summary>
    private static Mock<ITranslationService> PassThroughTranslation()
    {
        var mock = new Mock<ITranslationService>();
        mock.Setup(x => x.TranslateAsync(It.IsAny<RoleDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((RoleDto dto, string _, bool __) => new ValueTask<RoleDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<ProviderDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((ProviderDto dto, string _, bool __) => new ValueTask<ProviderDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<ProviderTypeDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((ProviderTypeDto dto, string _, bool __) => new ValueTask<ProviderTypeDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<PackageDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((PackageDto dto, string _, bool __) => new ValueTask<PackageDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<AreaDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((AreaDto dto, string _, bool __) => new ValueTask<AreaDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<AreaGroupDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((AreaGroupDto dto, string _, bool __) => new ValueTask<AreaGroupDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<ResourceDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((ResourceDto dto, string _, bool __) => new ValueTask<ResourceDto>(dto));
        mock.Setup(x => x.TranslateAsync(It.IsAny<TypeDto>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((TypeDto dto, string _, bool __) => new ValueTask<TypeDto>(dto));
        return mock;
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WhenResultsFound_Returns200Ok()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleDto> { new() { Id = Guid.NewGuid(), Name = "Rettighetshaver" } });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<RoleDto>>(ok.Value);
        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsNull_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RoleDto>)null);

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetAll();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── GetId ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetId_WhenFound_Returns200Ok()
    {
        var id = Guid.NewGuid();
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetById(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleDto> { new() { Id = id, Name = "Rettighetshaver" } });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetId(id);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetId_WhenServiceReturnsNull_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetById(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RoleDto>)null);

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetId(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── GetPackages (by role code + variant) ────────────────────────────────

    [Fact]
    public async Task GetPackagesByCode_WhenRoleCodeNotFound_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetPackages("nonexistent_role_code_xyz", KnownVariantName);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("nonexistent_role_code_xyz", notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetPackagesByCode_WhenVariantNotFound_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetPackages(KnownRoleCode, "nonexistent_variant_xyz");

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("nonexistent_variant_xyz", notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetPackagesByCode_WhenBothValid_Returns200Ok()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetRolePackages(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PackageDto> { new() { Id = Guid.NewGuid(), Name = "Tax" } });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetPackages(KnownRoleCode, KnownVariantName);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── GetResources (by role code + variant) ───────────────────────────────

    [Fact]
    public async Task GetResourcesByCode_WhenRoleCodeNotFound_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetResources("nonexistent_role_code_xyz", KnownVariantName);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("nonexistent_role_code_xyz", notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetResourcesByCode_WhenVariantNotFound_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetResources(KnownRoleCode, "nonexistent_variant_xyz");

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("nonexistent_variant_xyz", notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetResourcesByCode_WhenBothValid_Returns200Ok()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetRoleResources(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceDto> { new() { Id = Guid.NewGuid(), Name = "Resource1" } });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetResources(KnownRoleCode, KnownVariantName);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── GetPackages (by role id + variant) ──────────────────────────────────

    [Fact]
    public async Task GetPackagesById_WhenVariantNotFound_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetPackages(Guid.NewGuid(), "nonexistent_variant_xyz");

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("nonexistent_variant_xyz", notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetPackagesById_WhenVariantValid_Returns200Ok()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetRolePackages(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PackageDto> { new() { Id = Guid.NewGuid(), Name = "Tax" } });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetPackages(Guid.NewGuid(), KnownVariantName);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── GetResources (by role id + variant) ─────────────────────────────────

    [Fact]
    public async Task GetResourcesById_WhenVariantNotFound_Returns404NotFound()
    {
        var serviceMock = new Mock<IRoleService>();
        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetResources(Guid.NewGuid(), "nonexistent_variant_xyz");

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("nonexistent_variant_xyz", notFound.Value?.ToString());
    }

    [Fact]
    public async Task GetResourcesById_WhenVariantValid_Returns200Ok()
    {
        var serviceMock = new Mock<IRoleService>();
        serviceMock.Setup(s => s.GetRoleResources(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceDto> { new() { Id = Guid.NewGuid(), Name = "Resource1" } });

        var controller = CreateController(serviceMock.Object, PassThroughTranslation().Object);

        var result = await controller.GetResources(Guid.NewGuid(), KnownVariantName);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
