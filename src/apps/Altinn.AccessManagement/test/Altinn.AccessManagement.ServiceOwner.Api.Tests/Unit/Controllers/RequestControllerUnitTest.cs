using Altinn.AccessManagement.Api.ServiceOwner.Controllers;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.AccessManagement.ServiceOwner.Api.Tests.Unit.Controllers;

/// <summary>
/// Pure unit tests for <see cref="RequestController.WithdrawRequest"/> that bypass the
/// ASP.NET pipeline (auth/feature gates) and exercise the method body directly with
/// mocked <see cref="IRequestService"/> and <see cref="IAuditAccessor"/>. Complements
/// the integration tests in <see cref="RequestControllerTest"/> — these run without
/// the ApiFixture (no Postgres / migrations), so they cover the branches even when
/// the integration fixture is unavailable.
/// </summary>
[UnitTest]
public class RequestControllerUnitTest
{
    private static RequestController CreateController(
        IRequestService requestService,
        IAuditAccessor auditAccessor)
    {
        return new RequestController(
            requestService: requestService,
            entityService: null!,
            resourceService: null!,
            auditAccessor: auditAccessor,
            resourceRegistryClient: null!,
            generalSettings: Options.Create(new GeneralSettings()));
    }

    private static AccessRequest MakeRequest(Guid byId, Guid fromId, RequestStatusV2 status)
    {
        return new AccessRequest
        {
            Id = Guid.NewGuid(),
            Type = "package",
            Status = status,
            From = new PartyEntityDto { Id = fromId },
            To = new PartyEntityDto { Id = Guid.NewGuid() },
            By = new PartyEntityDto { Id = byId },
        };
    }

    private static Mock<IAuditAccessor> AuditAccessorFor(Guid changedBy)
    {
        var mock = new Mock<IAuditAccessor>();
        mock.SetupGet(a => a.AuditValues).Returns(new AuditValues(changedBy));
        return mock;
    }

    [Fact]
    public async Task WithdrawRequest_ByMatchesAndDraft_UpdatesToWithdrawnAndReturnsOk()
    {
        var caller = Guid.NewGuid();
        var request = MakeRequest(byId: caller, fromId: Guid.NewGuid(), status: RequestStatusV2.Draft);
        var updated = new AccessRequest { Id = request.Id, Status = RequestStatusV2.Withdrawn };

        var requestService = new Mock<IRequestService>(MockBehavior.Strict);
        requestService.Setup(s => s.GetRequest(request.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(request));
        requestService.Setup(s => s.UpdateRequest(request.From.Id, request.Id, RequestStatusV2.Withdrawn, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(updated));

        var controller = CreateController(requestService.Object, AuditAccessorFor(caller).Object);

        var result = await controller.WithdrawRequest(request.Id, TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.Withdrawn, ok.Value);
        requestService.VerifyAll();
    }

    [Fact]
    public async Task WithdrawRequest_ByMatchesAndPending_UpdatesToWithdrawnAndReturnsOk()
    {
        var caller = Guid.NewGuid();
        var request = MakeRequest(byId: caller, fromId: Guid.NewGuid(), status: RequestStatusV2.Pending);
        var updated = new AccessRequest { Id = request.Id, Status = RequestStatusV2.Withdrawn };

        var requestService = new Mock<IRequestService>(MockBehavior.Strict);
        requestService.Setup(s => s.GetRequest(request.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(request));
        requestService.Setup(s => s.UpdateRequest(request.From.Id, request.Id, RequestStatusV2.Withdrawn, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(updated));

        var controller = CreateController(requestService.Object, AuditAccessorFor(caller).Object);

        var result = await controller.WithdrawRequest(request.Id, TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.Withdrawn, ok.Value);
    }

    [Fact]
    public async Task WithdrawRequest_ByMatchesAndApproved_ReturnsProblemActionResult()
    {
        var caller = Guid.NewGuid();
        var request = MakeRequest(byId: caller, fromId: Guid.NewGuid(), status: RequestStatusV2.Approved);

        var requestService = new Mock<IRequestService>(MockBehavior.Strict);
        requestService.Setup(s => s.GetRequest(request.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(request));

        var controller = CreateController(requestService.Object, AuditAccessorFor(caller).Object);

        var result = await controller.WithdrawRequest(request.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("Problem", result.GetType().Name);
        requestService.Verify(s => s.UpdateRequest(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RequestStatusV2>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawRequest_ByDoesNotMatchCaller_ReturnsForbid()
    {
        var caller = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var request = MakeRequest(byId: otherUser, fromId: Guid.NewGuid(), status: RequestStatusV2.Draft);

        var requestService = new Mock<IRequestService>(MockBehavior.Strict);
        requestService.Setup(s => s.GetRequest(request.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(request));

        var controller = CreateController(requestService.Object, AuditAccessorFor(caller).Object);

        var result = await controller.WithdrawRequest(request.Id, TestContext.Current.CancellationToken);

        Assert.IsType<ForbidResult>(result);
        requestService.Verify(s => s.UpdateRequest(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RequestStatusV2>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawRequest_GetRequestReturnsProblem_ReturnsProblemActionResult()
    {
        var requestId = Guid.NewGuid();
        Result<AccessRequest> notFound = Problems.RequestNotFound.Create([]);

        var requestService = new Mock<IRequestService>(MockBehavior.Strict);
        requestService.Setup(s => s.GetRequest(requestId, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(notFound));

        var controller = CreateController(requestService.Object, AuditAccessorFor(Guid.NewGuid()).Object);

        var result = await controller.WithdrawRequest(requestId, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("Problem", result.GetType().Name);
        requestService.Verify(s => s.UpdateRequest(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RequestStatusV2>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawRequest_UpdateRequestReturnsProblem_ReturnsProblemActionResult()
    {
        var caller = Guid.NewGuid();
        var request = MakeRequest(byId: caller, fromId: Guid.NewGuid(), status: RequestStatusV2.Draft);
        Result<AccessRequest> failure = Problems.RequestNotFound.Create([]);

        var requestService = new Mock<IRequestService>(MockBehavior.Strict);
        requestService.Setup(s => s.GetRequest(request.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AccessRequest>>(request));
        requestService.Setup(s => s.UpdateRequest(request.From.Id, request.Id, RequestStatusV2.Withdrawn, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(failure));

        var controller = CreateController(requestService.Object, AuditAccessorFor(caller).Object);

        var result = await controller.WithdrawRequest(request.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("Problem", result.GetType().Name);
    }
}
