using System.Collections.Immutable;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Validation;

/// <summary>
/// Unit tests for <see cref="ParameterValidation"/>.
///
/// These cover the atomic per-parameter rules directly, including the branches
/// that are not composed by <see cref="ConnectionValidation"/>
/// (e.g. <c>ToIsGuid</c>, <c>InstanceRightsDelegationInput</c>,
/// <c>InstanceUrn</c>, keyword handling on <c>Party</c>/<c>PartyFrom</c>/<c>PartyTo</c>).
/// Driven via <see cref="ValidationComposer"/> so we exercise exactly the same
/// execution path as controller-level validation.
/// </summary>
public class ParameterValidationTest
{
    private static readonly string PartyA = Guid.NewGuid().ToString();
    private static readonly string EmptyGuid = Guid.Empty.ToString();

    private static ImmutableArray<ValidationErrorInstance> Errors(RuleExpression rule) =>
        ValidationComposer.Validate(rule)?.Errors ?? ImmutableArray<ValidationErrorInstance>.Empty;

    // ---- Party -----------------------------------------------------------
    [Fact]
    public void Party_ValidGuid_NoError()
    {
        Errors(ParameterValidation.Party(PartyA)).Should().BeEmpty();
    }

    [Fact]
    public void Party_KeywordMe_NoError()
    {
        Errors(ParameterValidation.Party("me")).Should().BeEmpty();
    }

    [Fact]
    public void Party_KeywordMe_CaseInsensitive_NoError()
    {
        Errors(ParameterValidation.Party("ME")).Should().BeEmpty();
    }

    [Fact]
    public void Party_KeywordAll_IsNotValidForParty_ReturnsError()
    {
        // 'all' is only valid for from/to, not for the 'party' parameter.
        var errors = Errors(ParameterValidation.Party("all"));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/party");
    }

    [Fact]
    public void Party_Null_ReturnsError()
    {
        Errors(ParameterValidation.Party(null!)).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/party");
    }

    [Fact]
    public void Party_Empty_ReturnsError()
    {
        Errors(ParameterValidation.Party(string.Empty)).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/party");
    }

    [Fact]
    public void Party_EmptyGuid_ReturnsError()
    {
        Errors(ParameterValidation.Party(EmptyGuid)).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/party");
    }

    [Fact]
    public void Party_NotAGuid_ReturnsError()
    {
        Errors(ParameterValidation.Party("not-a-guid")).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/party");
    }

    // ---- ToIsGuid --------------------------------------------------------
    [Fact]
    public void ToIsGuid_ValidGuid_NoError()
    {
        Errors(ParameterValidation.ToIsGuid(Guid.NewGuid())).Should().BeEmpty();
    }

    [Fact]
    public void ToIsGuid_Null_ReturnsError()
    {
        Errors(ParameterValidation.ToIsGuid(null)).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/to");
    }

    [Fact]
    public void ToIsGuid_EmptyGuid_ReturnsError()
    {
        Errors(ParameterValidation.ToIsGuid(Guid.Empty)).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/to");
    }

    [Fact]
    public void ToIsGuid_CustomParamName_UsesCustomNameInPath()
    {
        Errors(ParameterValidation.ToIsGuid(null, "rightholder")).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/rightholder");
    }

    // ---- PartyFrom / PartyTo --------------------------------------------
    [Fact]
    public void PartyFrom_ValidGuid_NoError()
    {
        Errors(ParameterValidation.PartyFrom(PartyA)).Should().BeEmpty();
    }

    [Fact]
    public void PartyFrom_KeywordMe_NoError()
    {
        Errors(ParameterValidation.PartyFrom("me")).Should().BeEmpty();
    }

    [Fact]
    public void PartyFrom_KeywordAll_NoError()
    {
        Errors(ParameterValidation.PartyFrom("all")).Should().BeEmpty();
    }

    [Fact]
    public void PartyFrom_KeywordAll_CaseInsensitive_NoError()
    {
        Errors(ParameterValidation.PartyFrom("ALL")).Should().BeEmpty();
    }

    [Fact]
    public void PartyFrom_EmptyGuid_ReturnsError()
    {
        Errors(ParameterValidation.PartyFrom(EmptyGuid)).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/from");
    }

    [Fact]
    public void PartyFrom_NotAGuid_ReturnsError()
    {
        Errors(ParameterValidation.PartyFrom("bogus")).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/from");
    }

    [Fact]
    public void PartyFrom_CustomParamName_UsesCustomNameInPath()
    {
        Errors(ParameterValidation.PartyFrom("bogus", "supplier")).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/supplier");
    }

    [Fact]
    public void PartyTo_ValidGuid_NoError()
    {
        Errors(ParameterValidation.PartyTo(PartyA)).Should().BeEmpty();
    }

    [Fact]
    public void PartyTo_KeywordAll_NoError()
    {
        Errors(ParameterValidation.PartyTo("all")).Should().BeEmpty();
    }

    [Fact]
    public void PartyTo_Invalid_ReturnsError()
    {
        Errors(ParameterValidation.PartyTo("bogus")).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/to");
    }

    [Fact]
    public void PartyTo_CustomParamName_UsesCustomNameInPath()
    {
        Errors(ParameterValidation.PartyTo(EmptyGuid, "consumer")).Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/consumer");
    }

    // ---- PersonInput -----------------------------------------------------
    [Fact]
    public void PersonInput_Valid_NoError()
    {
        Errors(ParameterValidation.PersonInput("01039012345", "Hansen"))
            .Should().BeEmpty();
    }

    [Fact]
    public void PersonInput_MissingIdentifier_ReturnsError()
    {
        Errors(ParameterValidation.PersonInput(string.Empty, "Hansen"))
            .Should().ContainSingle();
    }

    [Fact]
    public void PersonInput_NullIdentifier_ReturnsError()
    {
        Errors(ParameterValidation.PersonInput(null!, "Hansen"))
            .Should().ContainSingle();
    }

    [Fact]
    public void PersonInput_MissingLastName_ReturnsError()
    {
        Errors(ParameterValidation.PersonInput("01039012345", string.Empty))
            .Should().ContainSingle();
    }

    [Fact]
    public void PersonInput_WhitespaceLastName_ReturnsError()
    {
        Errors(ParameterValidation.PersonInput("01039012345", "   "))
            .Should().ContainSingle();
    }

    // ---- InstanceRightsDelegationInput -----------------------------------
    [Fact]
    public void InstanceRightsDelegationInput_OnlyTo_Valid_NoError()
    {
        Errors(ParameterValidation.InstanceRightsDelegationInput(
            Guid.NewGuid(),
            toInput: null,
            directRightKeys: new[] { "key1" }))
            .Should().BeEmpty();
    }

    [Fact]
    public void InstanceRightsDelegationInput_OnlyToInput_Valid_NoError()
    {
        Errors(ParameterValidation.InstanceRightsDelegationInput(
            to: null,
            toInput: new PersonInputDto { PersonIdentifier = "01039012345", LastName = "Hansen" },
            directRightKeys: new[] { "key1" }))
            .Should().BeEmpty();
    }

    [Fact]
    public void InstanceRightsDelegationInput_BothToAndToInput_ReturnsError()
    {
        var errors = Errors(ParameterValidation.InstanceRightsDelegationInput(
            Guid.NewGuid(),
            new PersonInputDto { PersonIdentifier = "01039012345", LastName = "Hansen" },
            new[] { "key1" }));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/to");
    }

    [Fact]
    public void InstanceRightsDelegationInput_NeitherToNorToInput_ReturnsError()
    {
        var errors = Errors(ParameterValidation.InstanceRightsDelegationInput(
            to: null,
            toInput: null,
            directRightKeys: new[] { "key1" }));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/to");
    }

    [Fact]
    public void InstanceRightsDelegationInput_ToIsEmptyGuid_ReturnsError()
    {
        var errors = Errors(ParameterValidation.InstanceRightsDelegationInput(
            Guid.Empty,
            toInput: null,
            directRightKeys: new[] { "key1" }));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/to");
    }

    [Fact]
    public void InstanceRightsDelegationInput_NullDirectRightKeys_ReturnsError()
    {
        Errors(ParameterValidation.InstanceRightsDelegationInput(
            Guid.NewGuid(),
            toInput: null,
            directRightKeys: null))
            .Should().ContainSingle();
    }

    [Fact]
    public void InstanceRightsDelegationInput_EmptyDirectRightKeys_ReturnsError()
    {
        Errors(ParameterValidation.InstanceRightsDelegationInput(
            Guid.NewGuid(),
            toInput: null,
            directRightKeys: Array.Empty<string>()))
            .Should().ContainSingle();
    }

    // ---- InstanceUrn -----------------------------------------------------
    [Fact]
    public void InstanceUrn_Null_NoError()
    {
        Errors(ParameterValidation.InstanceUrn(null!)).Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_Empty_NoError()
    {
        Errors(ParameterValidation.InstanceUrn(string.Empty)).Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_Whitespace_NoError()
    {
        Errors(ParameterValidation.InstanceUrn("   ")).Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_AppsPrefix_NoError()
    {
        Errors(ParameterValidation.InstanceUrn("urn:altinn:instance-id:abc-123"))
            .Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_AppsPrefix_CaseInsensitive_NoError()
    {
        Errors(ParameterValidation.InstanceUrn("URN:ALTINN:INSTANCE-ID:abc-123"))
            .Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_CorrespondencePrefix_NoError()
    {
        Errors(ParameterValidation.InstanceUrn("urn:altinn:correspondence-id:abc-123"))
            .Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_DialogPrefix_NoError()
    {
        Errors(ParameterValidation.InstanceUrn("urn:altinn:dialog-id:abc-123"))
            .Should().BeEmpty();
    }

    [Fact]
    public void InstanceUrn_InvalidPrefix_ReturnsError()
    {
        Errors(ParameterValidation.InstanceUrn("urn:altinn:other:abc"))
            .Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/instance");
    }

    [Fact]
    public void InstanceUrn_ArbitraryString_ReturnsError()
    {
        Errors(ParameterValidation.InstanceUrn("not-a-urn"))
            .Should().ContainSingle()
            .Which.Paths[0].Should().Be("$QUERY/instance");
    }
}
