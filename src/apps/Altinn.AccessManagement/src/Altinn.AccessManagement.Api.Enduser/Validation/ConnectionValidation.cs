using Altinn.AccessManagement.Api.Enduser.Models;
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
            ParameterValidation.Party(party),
            ValidationComposer.Any(ParameterValidation.PartyFrom(from), ParameterValidation.PartyTo(to)),
            ConnectionCombinationRules.PartyMatchesFromOrTo(party, from, to)
        );

    /// <summary>
    /// Validation rule for adding an assignment with <see cref="ConnectionInput"/>.
    /// </summary>
    internal static RuleExpression ValidateAddAssignmentWithConnectionInput(string party, string from, string to) =>
        ValidationComposer.All(
            ParameterValidation.Party(party),
            ParameterValidation.PartyFrom(from),
            ParameterValidation.PartyTo(to),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    /// <summary>
    /// Validation rule for adding an assignment with <see cref="PersonInput"/>.
    /// </summary>
    internal static RuleExpression ValidateAddAssignmentWithPersonInput(string personIdentifier, string personLastName) =>
        ValidationComposer.All(
            ParameterValidation.PersonInput(personIdentifier, personLastName)
        );

    /// <summary>
    /// Validation rule for adding an assignment with <see cref="PersonInput"/>.
    /// </summary>
    internal static RuleExpression ValidateAddAssignmentWithPersonInput(string party, string from, string personIdentifier, string personLastName) =>
        ValidationComposer.All(
            ParameterValidation.Party(party),
            ParameterValidation.PartyFrom(from),
            ParameterValidation.PersonInput(personIdentifier, personLastName),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    /// <summary>
    /// Validation rule for adding an access package to an existing rightholder connection with <see cref="ConnectionInput"/>.
    /// </summary>
    internal static RuleExpression ValidateAddPackageToConnectionWithConnectionInput(string party, string from, string to, Guid? packageId, string packageUrn) =>
        ValidationComposer.All(
            ParameterValidation.Party(party),
            ParameterValidation.PartyFrom(from),
            ParameterValidation.PartyTo(to),
            ConnectionCombinationRules.ExclusivePackageReference(packageId, packageUrn),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    /// <summary>
    /// Validation rule for adding an access package to an existing rightholder connection with  <see cref="PersonInput"/>.
    /// </summary>
    internal static RuleExpression ValidateAddPackageToConnectionWithPersonInput(string party, string from, string personIdentifier, string personLastName, Guid? packageId, string packageUrn) =>
        ValidationComposer.All(
            ParameterValidation.Party(party),
            ParameterValidation.PartyFrom(from),
            ParameterValidation.PersonInput(personIdentifier, personLastName),
            ConnectionCombinationRules.ExclusivePackageReference(packageId, packageUrn),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    /// <summary>
    /// Validation rule for removing an existing rightholder connection.
    /// </summary>
    internal static RuleExpression ValidateRemoveConnection(string party, string from, string to) =>
        ValidationComposer.All(
            ParameterValidation.Party(party),
            ParameterValidation.PartyFrom(from),
            ParameterValidation.PartyTo(to),
            ConnectionCombinationRules.RemovePartyMatchesFromOrTo(party, from, to)
        );

    /// <summary>
    /// Validation rule for removing package from existing rightholder connection.
    /// </summary>
    internal static RuleExpression ValidateRemovePackageFromConnection(string party, string from, string to, Guid? packageId, string packageUrn) =>
        ValidationComposer.All(
            ParameterValidation.Party(party),
            ParameterValidation.PartyFrom(from),
            ParameterValidation.PartyTo(to),
            ConnectionCombinationRules.ExclusivePackageReference(packageId, packageUrn),
            ConnectionCombinationRules.RemovePartyMatchesFromOrTo(party, from, to)
        );
}
