using System.Collections.Immutable;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Validation;

/// <summary>
/// Unit tests for <see cref="ConnectionValidation"/>.
///
/// Each method composes already-tested atomic rules
/// (see <see cref="ConnectionCombinationRulesTest"/>). Here we exercise the
/// happy path plus at least one failing path to cover the composition.
/// </summary>
public class ConnectionValidationTest
{
    private static readonly string PartyA = Guid.NewGuid().ToString();
    private static readonly string PartyB = Guid.NewGuid().ToString();

    private static ImmutableArray<ValidationErrorInstance> Errors(RuleExpression rule) =>
        ValidationComposer.Validate(rule)?.Errors ?? ImmutableArray<ValidationErrorInstance>.Empty;

    [Fact]
    public void ValidateReadConnection_PartyMatchesFrom_NoErrors()
    {
        Errors(ConnectionValidation.ValidateReadConnection(PartyA, PartyA, string.Empty))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateReadConnection_PartyMatchesNeither_ReturnsError()
    {
        var errors = Errors(ConnectionValidation.ValidateReadConnection(PartyA, PartyB, PartyB));

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateReadConnection_InvalidParty_ReturnsError()
    {
        var errors = Errors(ConnectionValidation.ValidateReadConnection("not-a-guid", PartyA, PartyB));

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddAssignmentWithConnectionInput_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddAssignmentWithConnectionInput(PartyA, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddAssignmentWithConnectionInput_SelfDelegation_ReturnsErrors()
    {
        Errors(ConnectionValidation.ValidateAddAssignmentWithConnectionInput(PartyA, PartyA, PartyA))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddAssignmentWithPersonInput_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddAssignmentWithPersonInput("01039012345", "Hansen"))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddAssignmentWithPersonInput_MissingIdentifier_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateAddAssignmentWithPersonInput(string.Empty, "Hansen"))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddAssignmentWithPersonInput_WithParty_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddAssignmentWithPersonInput(PartyA, PartyA, "01039012345", "Hansen"))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddAssignmentWithPersonInput_WithParty_PartyMismatch_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateAddAssignmentWithPersonInput(PartyA, PartyB, "01039012345", "Hansen"))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddPackageToConnectionWithConnectionInput_ValidWithUrn_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddPackageToConnectionWithConnectionInput(
                PartyA, PartyA, PartyB, packageId: null, packageUrn: "urn:altinn:package:foo"))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddPackageToConnectionWithConnectionInput_BothPackageRefs_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateAddPackageToConnectionWithConnectionInput(
                PartyA, PartyA, PartyB, packageId: Guid.NewGuid(), packageUrn: "urn:altinn:package:foo"))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddPackageToConnectionWithPersonInput_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddPackageToConnectionWithPersonInput(
                PartyA, PartyA, "01039012345", "Hansen", packageId: Guid.NewGuid(), packageUrn: null))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddPackageToConnectionWithPersonInput_MissingPackageRef_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateAddPackageToConnectionWithPersonInput(
                PartyA, PartyA, "01039012345", "Hansen", packageId: null, packageUrn: null))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateRemoveConnection_PartyMatchesFrom_NoErrors()
    {
        Errors(ConnectionValidation.ValidateRemoveConnection(PartyA, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateRemoveConnection_PartyMatchesNeither_ReturnsError()
    {
        var thirdParty = Guid.NewGuid().ToString();
        Errors(ConnectionValidation.ValidateRemoveConnection(PartyA, PartyB, thirdParty))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateRemovePackageFromConnection_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateRemovePackageFromConnection(
                PartyA, PartyA, PartyB, packageId: Guid.NewGuid(), packageUrn: null))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateRemovePackageFromConnection_EmptyPackageId_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateRemovePackageFromConnection(
                PartyA, PartyA, PartyB, packageId: Guid.Empty, packageUrn: null))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddResourceToConnectionWithConnectionInput_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddResourceToConnectionWithConnectionInput(PartyA, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddResourceToConnectionWithConnectionInput_SelfDelegation_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateAddResourceToConnectionWithConnectionInput(PartyA, PartyA, PartyA))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateAddResourceToConnectionWithPersonInput_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateAddResourceToConnectionWithPersonInput(
                PartyA, PartyA, "01039012345", "Hansen"))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAddResourceToConnectionWithPersonInput_PartyMismatch_ReturnsError()
    {
        Errors(ConnectionValidation.ValidateAddResourceToConnectionWithPersonInput(
                PartyA, PartyB, "01039012345", "Hansen"))
            .Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateRemoveResourceFromConnection_Valid_NoErrors()
    {
        Errors(ConnectionValidation.ValidateRemoveResourceFromConnection(PartyA, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateRemoveResourceFromConnection_PartyMatchesNeither_ReturnsError()
    {
        var thirdParty = Guid.NewGuid().ToString();
        Errors(ConnectionValidation.ValidateRemoveResourceFromConnection(PartyA, PartyB, thirdParty))
            .Should().NotBeEmpty();
    }
}
