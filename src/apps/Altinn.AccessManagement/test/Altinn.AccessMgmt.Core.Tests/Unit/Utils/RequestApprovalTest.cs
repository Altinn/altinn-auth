using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Utils;

/// <summary>
/// Pure unit tests for <see cref="RequestApproval"/>.
/// </summary>
[UnitTest]
public class RequestApprovalTest
{
    // ── DetermineStatus ─────────────────────────────────────────────────────
    [Fact]
    public void DetermineStatus_NoSelection_ForRequestedRights_ReturnsApproved()
    {
        RequestApproval.DetermineStatus(["read", "write"], null, []).Should().Be(RequestStatusV2.Approved);
    }

    [Fact]
    public void DetermineStatus_NoSelection_ForWholeResource_ReturnsApproved()
    {
        RequestApproval.DetermineStatus(null, null, []).Should().Be(RequestStatusV2.Approved);
    }

    [Fact]
    public void DetermineStatus_EmptySelection_IsTreatedAsNoSelection()
    {
        RequestApproval.DetermineStatus(["read"], [], []).Should().Be(RequestStatusV2.Approved);
        RequestApproval.DetermineStatus(null, [], []).Should().Be(RequestStatusV2.Approved);
    }

    [Fact]
    public void DetermineStatus_SelectionMatchingRequest_InOtherOrderAndWithDuplicates_ReturnsApproved()
    {
        RequestApproval.DetermineStatus(["read", "write"], ["write", "read", "write"], []).Should().Be(RequestStatusV2.Approved);
    }

    [Fact]
    public void DetermineStatus_SelectionWithFewerRights_ReturnsPartialApprove()
    {
        RequestApproval.DetermineStatus(["read", "write"], ["read"], []).Should().Be(RequestStatusV2.PartialApprove);
    }

    [Fact]
    public void DetermineStatus_SelectionWithMoreRights_ReturnsPartialApprove()
    {
        RequestApproval.DetermineStatus(["read"], ["read", "write"], []).Should().Be(RequestStatusV2.PartialApprove);
    }

    [Fact]
    public void DetermineStatus_SelectionWithOtherRights_ReturnsPartialApprove()
    {
        RequestApproval.DetermineStatus(["read"], ["write"], []).Should().Be(RequestStatusV2.PartialApprove);
    }

    [Fact]
    public void DetermineStatus_SelectionForWholeResource_WithSomeDelegableRights_ReturnsPartialApprove()
    {
        RequestApproval.DetermineStatus(null, ["read"], ["read", "write"]).Should().Be(RequestStatusV2.PartialApprove);
        RequestApproval.DetermineStatus([], ["read"], ["read", "write"]).Should().Be(RequestStatusV2.PartialApprove);
    }

    [Fact]
    public void DetermineStatus_SelectionForWholeResource_WithEveryDelegableRight_InOtherOrderAndWithDuplicates_ReturnsApproved()
    {
        RequestApproval.DetermineStatus(null, ["write", "read", "write"], ["read", "write"]).Should().Be(RequestStatusV2.Approved);
        RequestApproval.DetermineStatus([], ["write", "read"], ["read", "write"]).Should().Be(RequestStatusV2.Approved);
    }

    [Fact]
    public void DetermineStatus_SelectionForRequestedRights_IgnoresDelegableRights()
    {
        // Every delegable right selected, but more than was requested
        RequestApproval.DetermineStatus(["read"], ["read", "write"], ["read", "write"]).Should().Be(RequestStatusV2.PartialApprove);

        // Exactly what was requested, but fewer than the approver could give
        RequestApproval.DetermineStatus(["read"], ["read"], ["read", "write"]).Should().Be(RequestStatusV2.Approved);
    }

    [Fact]
    public void DetermineStatus_ComparesKeysCaseSensitively()
    {
        RequestApproval.DetermineStatus(["read"], ["READ"], []).Should().Be(RequestStatusV2.PartialApprove);
    }

    // ── ValidateUserCanDelegate ─────────────────────────────────────────────
    private static RightCheckDto Check(string key, bool result) => new() { Right = new RightDto { Key = key }, Result = result };

    [Fact]
    public void ValidateUserCanDelegate_AllKeysDelegable_ReturnsNull()
    {
        var problem = RequestApproval.ValidateUserCanDelegate(["read", "write"], [Check("read", true), Check("write", true)], "/rightKeys");

        problem.Should().BeNull();
    }

    [Fact]
    public void ValidateUserCanDelegate_KeyCheckedAsNotDelegable_ReturnsErrorAtItsIndex()
    {
        var problem = RequestApproval.ValidateUserCanDelegate(["read", "write"], [Check("read", true), Check("write", false)], "/rightKeys");

        problem.Should().NotBeNull();
        var error = problem!.Errors.Should().ContainSingle().Subject;
        error.ErrorCode.Should().Be(ValidationErrors.RightKeyNotDelegableByUser.ErrorCode);
        error.Paths.Should().ContainSingle(p => p.EndsWith("/rightKeys/1"));
    }

    [Fact]
    public void ValidateUserCanDelegate_KeyMissingFromCheck_ReturnsError()
    {
        var problem = RequestApproval.ValidateUserCanDelegate(["read"], [Check("write", true)], "/requestedRightKeys");

        problem.Should().NotBeNull();
        var error = problem!.Errors.Should().ContainSingle().Subject;
        error.ErrorCode.Should().Be(ValidationErrors.RightKeyNotDelegableByUser.ErrorCode);
        error.Paths.Should().ContainSingle(p => p.EndsWith("/requestedRightKeys/0"));
    }

    [Fact]
    public void ValidateUserCanDelegate_KeyCheckedBothWays_IsDelegable()
    {
        // The delegation check can list a key once per rule; one positive result is enough.
        var problem = RequestApproval.ValidateUserCanDelegate(["read"], [Check("read", false), Check("read", true)], "/rightKeys");

        problem.Should().BeNull();
    }

    [Fact]
    public void ValidateUserCanDelegate_OneErrorPerInvalidKey()
    {
        var problem = RequestApproval.ValidateUserCanDelegate(["read", "write", "sign"], [Check("write", true)], "/rightKeys");

        problem.Should().NotBeNull();
        problem!.Errors.Should().HaveCount(2);
        problem.Errors.Should().Contain(e => e.Paths.Any(p => p.EndsWith("/rightKeys/0")));
        problem.Errors.Should().Contain(e => e.Paths.Any(p => p.EndsWith("/rightKeys/2")));
    }
}
