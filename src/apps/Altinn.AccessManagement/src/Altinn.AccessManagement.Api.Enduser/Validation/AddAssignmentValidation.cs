using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

internal static class AddAssignmentValidation
{
    internal static RuleExpression ValidateAddAssignmentWithoutPersonInput(string party, string from, string to) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ConnectionParameterRules.PartyFrom(from),
            ConnectionParameterRules.PartyTo(to),
            ConnectionCombinationRules.PartyEqualsFrom(party, from)
        );

    internal static RuleExpression ValidateAddAssignmentWithPersonInput(string party, string from, string to, string personIdentifier, string personLastName) =>
        ValidationComposer.All(
            ConnectionParameterRules.Party(party),
            ConnectionParameterRules.PartyFrom(from),
            ConnectionParameterRules.OptionalPartyTo(to),
            ConnectionCombinationRules.PartyEqualsFrom(party, from),
            ValidatePersonInput(personIdentifier, personLastName)
        );

    internal static RuleExpression ValidatePersonInput(string personIdentifier, string personLastName) => () =>
    {
        var trimmed = personIdentifier?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return (ref ValidationErrorBuilder errors) =>
             errors.Add(ValidationErrors.InvalidQueryParameter, "BODY/personIdentifier", [new("personIdentifier", ValidationMessageTexts.PersonIdentifierRequired)]);
        }

        if (!PersonIdentifier.TryParse(trimmed, null, out _))
        {
            return (ref ValidationErrorBuilder errors) =>
             errors.Add(ValidationErrors.InvalidQueryParameter, "BODY/personIdentifier", [new("personIdentifier", ValidationMessageTexts.PersonIdentifierInvalidNin)]);
        }

        if (string.IsNullOrWhiteSpace(personLastName))
        {
            return (ref ValidationErrorBuilder errors) =>
             errors.Add(ValidationErrors.InvalidQueryParameter, "BODY/lastName", [new("lastName", ValidationMessageTexts.LastNameRequired)]);
        }

        return null;
    };
}
