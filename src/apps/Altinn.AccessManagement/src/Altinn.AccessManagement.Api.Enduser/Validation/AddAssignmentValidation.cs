using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// Internal implementation detail of the Enduser API.
/// </summary>
internal static class AddAssignmentValidation
{
    /// <summary>
    /// Validates combination of input parameters when PersonInput has not been provided
    /// ConnectionInput 'party', 'from' and 'to' are all required and 'party' must equal 'from'.
    /// </summary>
    internal static RuleExpression ValidateConnectionInputIfPersonInputNotPresent(string party, string from, string to) => () =>
    {
        if (!Guid.TryParse(party, out var partyUuid) || partyUuid == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("party", $"Paramater is not a valid a UUID.")]);
        }

        if (!Guid.TryParse(from, out var fromUuid) || fromUuid == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("from", $"Parameter is not a valid UUID.")]);
        }

        if (!Guid.TryParse(to, out var toUuid) || toUuid == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("to", $"Parameter is not a valid UUID.")]);
        }

        if (partyUuid != fromUuid)
        {
            return (ref ValidationErrorBuilder errors) =>
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/from", [new("from", "must match the 'party' UUID.")]);
            };
        }

        return null;
    };

    /// <summary>
    /// Validates combination of input parameters when PersonInput has been provided
    /// ConnectionInput 'to' is optional (validate if supplied) and 'party' must still equal 'from'.
    /// </summary>
    internal static RuleExpression ValidateConnectionInputIfPersonInputPresent(string party, string from, string to) => () =>
    {
        if (!Guid.TryParse(party, out var partyUuid) || partyUuid == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/party", [new("party", "Parameter is not a valid UUID.")]);
        }

        if (!Guid.TryParse(from, out var fromUuid) || fromUuid == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/from", [new("from", "Parameter is not a valid UUID.")]);
        }

        if (!string.IsNullOrWhiteSpace(to) && (!Guid.TryParse(to, out var toUuid) || toUuid == Guid.Empty))
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/to", [new("to", "Optional 'to' parameter must be a valid UUID when supplied.")]);
        }

        if (partyUuid != fromUuid)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/from", [new("from", "must match the 'party' UUID.")]);
        }

        return null;
    };

    /// <summary>
    /// Validates person identifier field (presence + basic format) and person last name (presence).
    /// <param name="personIdentifier">Username or SSN of the person a new assignment is given to.</param>
    /// <param name="personLastName">Last name of the person a new assignment is given to.</param>
    /// </summary>
    internal static RuleExpression ValidatePersonInput(string personIdentifier, string personLastName) => () =>
    {
        var trimmed = personIdentifier?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return (ref ValidationErrorBuilder errors) =>
             errors.Add(ValidationErrors.InvalidQueryParameter, "BODY/personIdentifier", [new("personIdentifier", "PersonIdentifier is required when providing PersonInput details.")]);
        }

        if (trimmed.Length == 11 && trimmed.All(char.IsDigit))
        {
            if (!ValidSSNUtil.IsValidSSN(trimmed))
            {
                return (ref ValidationErrorBuilder errors) =>
                 errors.Add(ValidationErrors.InvalidQueryParameter, "BODY/personIdentifier", [new("personIdentifier", "PersonIdentifier with national identity number format is not a valid national identity number.")]);
            }
        }

        if (string.IsNullOrWhiteSpace(personLastName))
        {
            return (ref ValidationErrorBuilder errors) =>
             errors.Add(ValidationErrors.InvalidQueryParameter, "BODY/lastName", [new("lastName", "LastName is required when providing PersonInput details.")]);
        }

        return null;
    };
}
