using Altinn.AccessManagement.Api.Internal.Controllers;
using Altinn.AccessManagement.Api.Internal.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ConnectionOptions = Altinn.AccessMgmt.Core.Services.ConnectionOptions;
using ValidationErrors = Altinn.AccessMgmt.Core.Utils.Models.ValidationErrors;

namespace Altinn.AccessManagement.Api.Tests.Controllers;

public class InternalConnectionsControllerTest
{
    private static readonly Guid Party = Guid.NewGuid();
    private static readonly Guid To = Guid.NewGuid();
    private static readonly ConnectionInput Connection = new() { Party = Party, To = To };

    private static InternalConnectionsController CreateSut(IConnectionService svc)
    {
        var controller = new InternalConnectionsController(svc);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ValidationProblemInstance MakeValidationProblem() =>
        ValidationComposer.Validate(() =>
            (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidPartyUrn, "QUERY/test"));

    #region GetConnections

    [Fact]
    public async Task GetConnections_Success_ReturnsOk()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.Get(Party, Party, To, true, true, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ConnectionDto>>([]));

        var result = await CreateSut(svc.Object).GetConnections(Connection, new PagingInput());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConnections_Problem_ReturnsActionResult()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.Get(Party, Party, To, true, true, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetConnections(Connection, new PagingInput());

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region AddAssignment

    [Fact]
    public async Task AddAssignment_Success_ReturnsOk()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.AddRightholder(Party, To, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AssignmentDto>(new AssignmentDto()));

        var result = await CreateSut(svc.Object).AddAssignment(Connection);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddAssignment_Problem_ReturnsActionResult()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.AddRightholder(Party, To, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).AddAssignment(Connection);

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region RemoveAssignment

    [Fact]
    public async Task RemoveAssignment_Success_ReturnsNoContent()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.RemoveAssignment(Party, To, false, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemoveAssignment(Connection);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveAssignment_Problem_ReturnsActionResult()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.RemoveAssignment(Party, To, false, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveAssignment(Connection);

        Assert.IsNotType<NoContentResult>(result);
    }

    #endregion

    #region GetPackages

    [Fact]
    public async Task GetPackages_Success_ReturnsOk()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.GetPackages(Party, Party, To, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<PackagePermissionDto>>([]));

        var result = await CreateSut(svc.Object).GetPackages(Connection, new PagingInput());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPackages_Problem_ReturnsActionResult()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.GetPackages(Party, Party, To, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetPackages(Connection, new PagingInput());

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region AddPackages

    [Fact]
    public async Task AddPackages_ByGuid_Success_ReturnsOk()
    {
        var packageId = Guid.NewGuid();
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.AddPackage(Party, To, packageId, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AssignmentPackageDto>(new AssignmentPackageDto()));

        var result = await CreateSut(svc.Object).AddPackages(Connection, packageId, null);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddPackages_ByString_Success_ReturnsOk()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.AddPackage(Party, To, "urn:some:package", It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AssignmentPackageDto>(new AssignmentPackageDto()));

        var result = await CreateSut(svc.Object).AddPackages(Connection, null, "urn:some:package");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddPackages_Problem_ReturnsActionResult()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.AddPackage(Party, To, "urn:some:package", It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).AddPackages(Connection, null, "urn:some:package");

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region RemovePackages

    [Fact]
    public async Task RemovePackages_ByGuid_Success_ReturnsNoContent()
    {
        var packageId = Guid.NewGuid();
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.RemovePackage(Party, To, packageId, It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemovePackages(Connection, packageId, null);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemovePackages_ByString_Success_ReturnsNoContent()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.RemovePackage(Party, To, "urn:some:package", It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemovePackages(Connection, null, "urn:some:package");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemovePackages_Problem_ReturnsActionResult()
    {
        var svc = new Mock<IConnectionService>();
        svc.Setup(s => s.RemovePackage(Party, To, "urn:some:package", It.IsAny<Action<ConnectionOptions>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemovePackages(Connection, null, "urn:some:package");

        Assert.IsNotType<NoContentResult>(result);
    }

    #endregion
}
