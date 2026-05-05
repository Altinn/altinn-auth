using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ValidationErrors = Altinn.AccessMgmt.Core.Utils.Models.ValidationErrors;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

#pragma warning disable SA1649 // file holds both Consumers/Suppliers controller tests
public class MaskinportenConsumersControllerTest
{
    private static readonly Guid Party = Guid.NewGuid();
    private static readonly Guid EntityId = Guid.NewGuid();
    private static readonly Entity SampleEntity = new() { Id = EntityId };
    private static readonly Resource SampleResource = new();
    private static readonly Guid ResourceId = SampleResource.Id;

    private static MaskinportenConsumersController CreateSut(IMaskinportenSupplierService svc)
    {
        var controller = new MaskinportenConsumersController(svc);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ValidationProblemInstance MakeValidationProblem() =>
        ValidationComposer.Validate(() =>
            (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidPartyUrn, "QUERY/test"));

    #region GetConsumers

    [Fact]
    public async Task GetConsumers_NoFilter_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetConsumers(Party, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ConnectionDto>>([]));

        var result = await CreateSut(svc.Object).GetConsumers(Party);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConsumers_NoFilter_Problem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetConsumers(Party, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetConsumers(Party);

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConsumers_ConsumerFilter_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("123456789", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetConsumers(Party, consumer: "123456789");

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConsumers_ConsumerFilter_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("123456789", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.GetConsumers(Party, EntityId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ConnectionDto>>([]));

        var result = await CreateSut(svc.Object).GetConsumers(Party, consumer: "123456789");

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RemoveConsumer

    [Fact]
    public async Task RemoveConsumer_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveConsumer(Party, "org1");

        Assert.IsNotType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveConsumer_Success_ReturnsNoContent()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveSupplier(EntityId, Party, false, It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemoveConsumer(Party, "org1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveConsumer_RemoveSupplierProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveSupplier(EntityId, Party, false, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveConsumer(Party, "org1");

        Assert.IsNotType<NoContentResult>(result);
    }

    #endregion

    #region GetResources

    [Fact]
    public async Task GetResources_NoFilters_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetConsumerResources(Party, null, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ResourcePermissionDto>>([]));

        var result = await CreateSut(svc.Object).GetResources(Party);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_NoFilters_Problem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetConsumerResources(Party, null, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetResources(Party);

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_ConsumerFilter_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetResources(Party, consumer: "org1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_ResourceFilter_GetResourceByRefIdProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetResourceByRefId("res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetResources(Party, resource: "res1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_AllFilters_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.GetResourceByRefId("res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Resource>(SampleResource));
        svc.Setup(s => s.GetConsumerResources(Party, EntityId, ResourceId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ResourcePermissionDto>>([]));

        var result = await CreateSut(svc.Object).GetResources(Party, consumer: "org1", resource: "res1");

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RemoveResource

    [Fact]
    public async Task RemoveResource_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveResource(Party, "org1", "res1");

        Assert.IsNotType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveResource_Success_ReturnsNoContent()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveResource(EntityId, Party, "res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemoveResource(Party, "org1", "res1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveResource_RemoveResourceProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveResource(EntityId, Party, "res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveResource(Party, "org1", "res1");

        Assert.IsNotType<NoContentResult>(result);
    }

    #endregion
}

public class MaskinportenSuppliersControllerTest
{
    private static readonly Guid Party = Guid.NewGuid();
    private static readonly Guid EntityId = Guid.NewGuid();
    private static readonly Guid UserUuid = Guid.NewGuid();
    private static readonly Entity SampleEntity = new() { Id = EntityId };
    private static readonly Resource SampleResource = new();
    private static readonly Guid ResourceId = SampleResource.Id;

    private static MaskinportenSuppliersController CreateSut(IMaskinportenSupplierService svc)
    {
        var controller = new MaskinportenSuppliersController(svc);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static MaskinportenSuppliersController CreateSutWithClaim(IMaskinportenSupplierService svc, Guid partyUuid)
    {
        var controller = new MaskinportenSuppliersController(svc);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString())
        ]));
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private static ValidationProblemInstance MakeValidationProblem() =>
        ValidationComposer.Validate(() =>
            (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidPartyUrn, "QUERY/test"));

    #region AddSupplier

    [Fact]
    public async Task AddSupplier_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).AddSupplier(Party, "org1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddSupplier_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.AddSupplier(Party, EntityId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AssignmentDto>(new AssignmentDto()));

        var result = await CreateSut(svc.Object).AddSupplier(Party, "org1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddSupplier_AddSupplierProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.AddSupplier(Party, EntityId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).AddSupplier(Party, "org1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region GetSuppliers

    [Fact]
    public async Task GetSuppliers_NoFilter_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetSuppliers(Party, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ConnectionDto>>([]));

        var result = await CreateSut(svc.Object).GetSuppliers(Party);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetSuppliers_NoFilter_Problem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetSuppliers(Party, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetSuppliers(Party);

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetSuppliers_SupplierFilter_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetSuppliers(Party, supplier: "org1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region RemoveSupplier

    [Fact]
    public async Task RemoveSupplier_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveSupplier(Party, "org1");

        Assert.IsNotType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveSupplier_Success_ReturnsNoContent()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveSupplier(Party, EntityId, false, It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemoveSupplier(Party, "org1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveSupplier_RemoveSupplierProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveSupplier(Party, EntityId, false, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveSupplier(Party, "org1");

        Assert.IsNotType<NoContentResult>(result);
    }

    #endregion

    #region DelegationCheck

    [Fact]
    public async Task DelegationCheck_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.ResourceDelegationCheck(UserUuid, Party, "res1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<ResourceCheckDto>(new ResourceCheckDto { Resource = new ResourceDto(), Rights = [] }));

        var result = await CreateSutWithClaim(svc.Object, UserUuid).DelegationCheck(Party, "res1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DelegationCheck_Problem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.ResourceDelegationCheck(UserUuid, Party, "res1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSutWithClaim(svc.Object, UserUuid).DelegationCheck(Party, "res1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region AddResource

    [Fact]
    public async Task AddResource_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).AddResource(Party, "org1", "res1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddResource_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.AddResource(Party, EntityId, "res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<bool>(true));

        var result = await CreateSut(svc.Object).AddResource(Party, "org1", "res1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddResource_AddResourceProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.AddResource(Party, EntityId, "res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).AddResource(Party, "org1", "res1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region GetResources

    [Fact]
    public async Task GetResources_NoFilters_Success_ReturnsOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetSupplierResources(Party, null, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<IEnumerable<ResourcePermissionDto>>([]));

        var result = await CreateSut(svc.Object).GetResources(Party);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_NoFilters_Problem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetSupplierResources(Party, null, null, It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetResources(Party);

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_SupplierFilter_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetResources(Party, supplier: "org1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_ResourceFilter_GetResourceByRefIdProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetResourceByRefId("res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).GetResources(Party, resource: "res1");

        Assert.IsNotType<OkObjectResult>(result);
    }

    #endregion

    #region RemoveResource

    [Fact]
    public async Task RemoveResource_GetEntityProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveResource(Party, "org1", "res1");

        Assert.IsNotType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveResource_Success_ReturnsNoContent()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveResource(Party, EntityId, "res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync((ValidationProblemInstance)null);

        var result = await CreateSut(svc.Object).RemoveResource(Party, "org1", "res1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveResource_RemoveResourceProblem_ReturnsNonOk()
    {
        var svc = new Mock<IMaskinportenSupplierService>();
        svc.Setup(s => s.GetEntity("org1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<Entity>(SampleEntity));
        svc.Setup(s => s.RemoveResource(Party, EntityId, "res1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(MakeValidationProblem());

        var result = await CreateSut(svc.Object).RemoveResource(Party, "org1", "res1");

        Assert.IsNotType<NoContentResult>(result);
    }

    #endregion
}
