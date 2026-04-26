using Altinn.AccessManagement.Api.Internal.Controllers;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ValidationErrors = Altinn.AccessMgmt.Core.Utils.Models.ValidationErrors;
using CreateSystemDelegationRequestDto = Altinn.Authorization.Api.Contracts.AccessManagement.CreateSystemDelegationRequestDto;

namespace Altinn.AccessManagement.Api.Tests.Controllers;

public class SystemUserClientDelegationControllerTest
{
    private static ProblemInstance MakeProblem() =>
        ValidationComposer.Validate(() =>
            (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidPartyUrn, "QUERY/test"));

    private static readonly Guid Party = Guid.NewGuid();
    private static readonly Guid SystemUser = Guid.NewGuid();
    private static readonly Guid Client = Guid.NewGuid();
    private static readonly Guid DelegationId = Guid.NewGuid();
    private static readonly Guid AssignmentId = Guid.NewGuid();

    private static SystemUserClientDelegationController CreateSut(
        IAssignmentService assignmentSvc = null,
        IConnectionService connectionSvc = null,
        IDelegationService delegationSvc = null)
    {
        var controller = new SystemUserClientDelegationController(
            assignmentSvc ?? new Mock<IAssignmentService>().Object,
            connectionSvc ?? new Mock<IConnectionService>().Object,
            delegationSvc ?? new Mock<IDelegationService>().Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    #region GetClients

    [Fact]
    public async Task GetClients_InvalidRole_ReturnsBadRequest()
    {
        var result = await CreateSut().GetClients(Party, roles: ["not-a-valid-role"]);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetClients_ValidRoles_ReturnsOk()
    {
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetClients(Party, It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object)
            .GetClients(Party, roles: [RoleConstants.Accountant.Entity.Code]);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetClients_NoRoles_UsesDefaultsAndReturnsOk()
    {
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetClients(Party, It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object).GetClients(Party);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region GetClientDelegations

    [Fact]
    public async Task GetClientDelegations_ReturnsOk()
    {
        var connectionSvc = new Mock<IConnectionService>();
        connectionSvc.Setup(s => s.GetConnectionsToAgent(Party, SystemUser, Client, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        var result = await CreateSut(connectionSvc: connectionSvc.Object)
            .GetClientDelegations(Party, SystemUser, Client);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region PostClientDelegation

    [Fact]
    public async Task PostClientDelegation_ReturnsOk()
    {
        var delegationSvc = new Mock<IDelegationService>();
        delegationSvc.Setup(s => s.CreateClientDelegation(It.IsAny<CreateSystemDelegationRequestDto>(), Party, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        var result = await CreateSut(delegationSvc: delegationSvc.Object)
            .PostClientDelegation(Party, new CreateSystemDelegationRequestDto());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region DeleteDelegation

    [Fact]
    public async Task DeleteDelegation_NotFound_ReturnsBadRequest()
    {
        var delegationSvc = new Mock<IDelegationService>();
        delegationSvc.Setup(s => s.GetDelegation(DelegationId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<Delegation>(null));

        var result = await CreateSut(delegationSvc: delegationSvc.Object)
            .DeleteDelegation(Party, DelegationId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDelegation_WrongFacilitator_ReturnsBadRequest()
    {
        var delegation = new Delegation { FacilitatorId = Guid.NewGuid(), FromId = Guid.NewGuid() };
        var delegationSvc = new Mock<IDelegationService>();
        delegationSvc.Setup(s => s.GetDelegation(DelegationId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(delegation));

        var result = await CreateSut(delegationSvc: delegationSvc.Object)
            .DeleteDelegation(Party, DelegationId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDelegation_AssignmentToIdMismatch_ReturnsBadRequest()
    {
        var delegation = new Delegation { FacilitatorId = Party, FromId = Guid.NewGuid() };
        var assignment = new Assignment { FromId = Guid.NewGuid(), ToId = Guid.NewGuid() };

        var delegationSvc = new Mock<IDelegationService>();
        delegationSvc.Setup(s => s.GetDelegation(DelegationId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(delegation));

        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(delegation.FromId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(assignment));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object, delegationSvc: delegationSvc.Object)
            .DeleteDelegation(Party, DelegationId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDelegation_Success_ReturnsOk()
    {
        var delegation = new Delegation { FacilitatorId = Party, FromId = Guid.NewGuid() };
        var assignment = new Assignment { FromId = Guid.NewGuid(), ToId = Party };

        var delegationSvc = new Mock<IDelegationService>();
        delegationSvc.Setup(s => s.GetDelegation(DelegationId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(delegation));
        delegationSvc.Setup(s => s.DeleteDelegation(delegation.Id, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<ProblemInstance>(null));

        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(delegation.FromId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(assignment));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object, delegationSvc: delegationSvc.Object)
            .DeleteDelegation(Party, DelegationId);

        Assert.IsType<OkResult>(result);
    }

    #endregion

    #region DeleteAssignment

    [Fact]
    public async Task DeleteAssignment_NotFound_ReturnsBadRequest()
    {
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(AssignmentId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<Assignment>(null));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object)
            .DeleteAssignment(Party, AssignmentId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAssignment_WrongFromParty_ReturnsBadRequest()
    {
        var assignment = new Assignment { FromId = Guid.NewGuid(), ToId = Guid.NewGuid() };
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(AssignmentId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult(assignment));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object)
            .DeleteAssignment(Party, AssignmentId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAssignment_WrongRole_ReturnsProblem()
    {
        var assignment = new Assignment
        {
            FromId = Party,
            ToId = Guid.NewGuid(),
            Role = new Role { Code = "not-agent" }
        };
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(AssignmentId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<Assignment>(assignment));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object)
            .DeleteAssignment(Party, AssignmentId);

        Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAssignment_Success_ReturnsOk()
    {
        var assignment = new Assignment
        {
            FromId = Party,
            ToId = Guid.NewGuid(),
            Role = new Role { Code = RoleConstants.Agent.Entity.Code }
        };
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(AssignmentId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<Assignment>(assignment));
        assignmentSvc.Setup(s => s.DeleteAssignment(assignment.Id, false, null, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<ProblemInstance>(null));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object)
            .DeleteAssignment(Party, AssignmentId);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteAssignment_HasActiveDelegations_ReturnsProblem()
    {
        var assignment = new Assignment
        {
            FromId = Party,
            ToId = Guid.NewGuid(),
            Role = new Role { Code = RoleConstants.Agent.Entity.Code }
        };
        var assignmentSvc = new Mock<IAssignmentService>();
        assignmentSvc.Setup(s => s.GetAssignment(AssignmentId, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<Assignment>(assignment));
        assignmentSvc.Setup(s => s.DeleteAssignment(assignment.Id, false, null, It.IsAny<CancellationToken>()))
                     .Returns(Task.FromResult<ProblemInstance>(MakeProblem()));

        var result = await CreateSut(assignmentSvc: assignmentSvc.Object)
            .DeleteAssignment(Party, AssignmentId);

        Assert.IsType<ObjectResult>(result);
    }

    #endregion
}
