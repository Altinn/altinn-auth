using Altinn.AccessMgmt.Core.Validation;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// Validation rules for Connection-related query parameters (excluding AddAssignment which has specialized rules).
/// Uses atomic parameter rules + combination (semantic) rules.
/// </summary>
internal static class ConnectionValidation
{
    internal static RuleExpression ValidateReadConnection(string party, string from, string to) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ValidationComposer.Any(ConnectionParameterRules.PartyFrom(from), ConnectionParameterRules.PartyTo(to)),
            ConnectionCombinationRules.PartyMatchesFromOrTo(party, from, to)
        );

    internal static RuleExpression ValidateAddConnection(string party, string from, string to) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ConnectionParameterRules.PartyFrom(from),
            ConnectionParameterRules.PartyTo(to),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    internal static RuleExpression ValidateAddConnectionWithPackage(string party, string from, string to, Guid? packageId, string packageUrn) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ConnectionParameterRules.PartyFrom(from),
            ConnectionParameterRules.PartyTo(to),
            ConnectionCombinationRules.ExclusivePackageReference(packageId, packageUrn),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    internal static RuleExpression ValidateRemoveConnection(string party, string from, string to) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ConnectionParameterRules.PartyFrom(from),
            ConnectionParameterRules.PartyTo(to),
            ConnectionCombinationRules.RemovePartyMatchesFromOrTo(party, from, to)
        );

    internal static RuleExpression ValidateRemoveConnectionWithPackage(string party, string from, string to, Guid? packageId, string packageUrn) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ConnectionParameterRules.PartyFrom(from),
            ConnectionParameterRules.PartyTo(to),
            ConnectionCombinationRules.ExclusivePackageReference(packageId, packageUrn),
            ConnectionCombinationRules.RemovePartyMatchesFromOrTo(party, from, to)
        );
}
