using System.Collections.Immutable;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Validation;

/// <summary>
/// Unit tests for <see cref="ConnectionCombinationRules"/>.
///
/// These cover the cross-field (semantic) rules directly so they are not only
/// exercised through controller integration tests. See
/// <c>docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/Coverage_Enduser_Api.md</c> for the coverage rationale.
/// </summary>
public class ConnectionCombinationRulesTest
{
    private static readonly string PartyA = Guid.NewGuid().ToString();
    private static readonly string PartyB = Guid.NewGuid().ToString();
    private static readonly string EmptyGuid = Guid.Empty.ToString();

    private static ImmutableArray<ValidationErrorInstance> Errors(RuleExpression rule) =>
        ValidationComposer.Validate(rule)?.Errors ?? ImmutableArray<ValidationErrorInstance>.Empty;

    // ---- PartyEqualsFrom --------------------------------------------------
    [Fact]
    public void PartyEqualsFrom_PartyNotGuid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyEqualsFrom("not-a-guid", PartyA))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyEqualsFrom_PartyEmptyGuid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyEqualsFrom(EmptyGuid, PartyA))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyEqualsFrom_FromNotGuid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyEqualsFrom(PartyA, "not-a-guid"))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyEqualsFrom_FromEmptyGuid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyEqualsFrom(PartyA, EmptyGuid))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyEqualsFrom_Matching_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.PartyEqualsFrom(PartyA, PartyA))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyEqualsFrom_Mismatch_ReturnsError()
    {
        var errors = Errors(ConnectionCombinationRules.PartyEqualsFrom(PartyA, PartyB));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("QUERY/from");
    }

    // ---- PartyMatchesFromOrTo --------------------------------------------
    [Fact]
    public void PartyMatchesFromOrTo_PartyInvalid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyMatchesFromOrTo("not-a-guid", PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyMatchesFromOrTo_PartyEmpty_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(EmptyGuid, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyMatchesFromOrTo_NeitherFromNorToValid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(PartyA, string.Empty, EmptyGuid))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyMatchesFromOrTo_PartyMatchesFrom_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(PartyA, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyMatchesFromOrTo_PartyMatchesTo_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(PartyA, PartyB, PartyA))
            .Should().BeEmpty();
    }

    [Fact]
    public void PartyMatchesFromOrTo_NeitherMatchesBothValid_ReturnsTwoErrors()
    {
        var errors = Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(PartyA, PartyB, PartyB));

        errors.Select(e => e.Paths[0]).Should().BeEquivalentTo(["QUERY/from", "QUERY/to"]);
    }

    [Fact]
    public void PartyMatchesFromOrTo_NeitherMatchesOnlyFromValid_ReturnsFromError()
    {
        var errors = Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(PartyA, PartyB, "not-a-guid"));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("QUERY/from");
    }

    [Fact]
    public void PartyMatchesFromOrTo_NeitherMatchesOnlyToValid_ReturnsToError()
    {
        var errors = Errors(ConnectionCombinationRules.PartyMatchesFromOrTo(PartyA, "not-a-guid", PartyB));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("QUERY/to");
    }

    // ---- RemovePartyMatchesFromOrTo --------------------------------------
    [Fact]
    public void RemovePartyMatchesFromOrTo_PartyInvalid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.RemovePartyMatchesFromOrTo("bad", PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void RemovePartyMatchesFromOrTo_FromInvalid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.RemovePartyMatchesFromOrTo(PartyA, "bad", PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void RemovePartyMatchesFromOrTo_ToInvalid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.RemovePartyMatchesFromOrTo(PartyA, PartyA, EmptyGuid))
            .Should().BeEmpty();
    }

    [Fact]
    public void RemovePartyMatchesFromOrTo_PartyMatchesFrom_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.RemovePartyMatchesFromOrTo(PartyA, PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void RemovePartyMatchesFromOrTo_PartyMatchesTo_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.RemovePartyMatchesFromOrTo(PartyA, PartyB, PartyA))
            .Should().BeEmpty();
    }

    [Fact]
    public void RemovePartyMatchesFromOrTo_NoMatch_ReturnsTwoErrors()
    {
        var thirdParty = Guid.NewGuid().ToString();
        var errors = Errors(ConnectionCombinationRules.RemovePartyMatchesFromOrTo(PartyA, PartyB, thirdParty));

        errors.Select(e => e.Paths[0]).Should().BeEquivalentTo(["QUERY/from", "QUERY/to"]);
    }

    // ---- FromAndToMustBeDifferent ----------------------------------------
    [Fact]
    public void FromAndToMustBeDifferent_FromInvalid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.FromAndToMustBeDifferent("bad", PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void FromAndToMustBeDifferent_ToInvalid_ReturnsNullRule()
    {
        Errors(ConnectionCombinationRules.FromAndToMustBeDifferent(PartyA, EmptyGuid))
            .Should().BeEmpty();
    }

    [Fact]
    public void FromAndToMustBeDifferent_Different_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.FromAndToMustBeDifferent(PartyA, PartyB))
            .Should().BeEmpty();
    }

    [Fact]
    public void FromAndToMustBeDifferent_Same_ReturnsTwoErrors()
    {
        var errors = Errors(ConnectionCombinationRules.FromAndToMustBeDifferent(PartyA, PartyA));

        errors.Select(e => e.Paths[0]).Should().BeEquivalentTo(["QUERY/from", "QUERY/to"]);
    }

    // ---- ExclusivePackageReference ---------------------------------------
    [Fact]
    public void ExclusivePackageReference_OnlyUrn_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.ExclusivePackageReference(null, "urn:altinn:package:foo"))
            .Should().BeEmpty();
    }

    [Fact]
    public void ExclusivePackageReference_OnlyId_ReturnsNoError()
    {
        Errors(ConnectionCombinationRules.ExclusivePackageReference(Guid.NewGuid(), null))
            .Should().BeEmpty();
    }

    [Fact]
    public void ExclusivePackageReference_Both_ReturnsTwoErrors()
    {
        var errors = Errors(ConnectionCombinationRules.ExclusivePackageReference(Guid.NewGuid(), "urn:altinn:package:foo"));

        errors.Select(e => e.Paths[0]).Should().BeEquivalentTo(["QUERY/packageId", "QUERY/package"]);
    }

    [Fact]
    public void ExclusivePackageReference_IdIsEmptyGuid_ReturnsPackageIdMustNotBeEmpty()
    {
        var errors = Errors(ConnectionCombinationRules.ExclusivePackageReference(Guid.Empty, null));

        errors.Should().ContainSingle()
            .Which.Paths[0].Should().Be("QUERY/packageId");
    }

    [Fact]
    public void ExclusivePackageReference_Neither_ReturnsRequireOne()
    {
        var errors = Errors(ConnectionCombinationRules.ExclusivePackageReference(null, null));

        errors.Select(e => e.Paths[0]).Should().BeEquivalentTo(["QUERY/packageId", "QUERY/package"]);
    }

    [Fact]
    public void ExclusivePackageReference_CustomParameterNames_UsesProvidedNames()
    {
        var errors = Errors(ConnectionCombinationRules.ExclusivePackageReference(
            Guid.NewGuid(), "urn:altinn:package:foo", idName: "myId", urnName: "myUrn"));

        errors.Should().HaveCount(2);
        errors.Select(e => e.Paths[0]).Should().BeEquivalentTo(["QUERY/myId", "QUERY/myUrn"]);
    }
}
