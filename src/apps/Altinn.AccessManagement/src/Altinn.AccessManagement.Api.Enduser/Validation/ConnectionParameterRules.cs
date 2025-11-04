using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// Atomic (single-parameter) validation rules for connection-related query parameters.
/// Used by both ConnectionValidation and AddAssignmentValidation.
/// </summary>
internal static class ConnectionParameterRules
{
    /// <summary>
    /// Validates a party-like parameter (GUID or allowed keyword). Rejects Guid.Empty. Trims input before checks.
    /// </summary>
    internal static RuleExpression Party(string value, string paramName = "party") => () =>
    {
        var trimmed = value?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            if (Guid.TryParse(trimmed, out var parsed) && parsed != Guid.Empty)
            {
                return null;
            }
        }

        return (ref ValidationErrorBuilder errors) =>
            errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new(paramName, ValidationMessageTexts.InvalidPartyValue)]);
    };

    internal static RuleExpression PartyFrom(string value) => Party(value, "from");

    internal static RuleExpression PartyTo(string value) => Party(value, "to");

    /// <summary>
    /// Optional 'to' parameter: validate only if supplied (non-null/non-whitespace).
    /// </summary>
    internal static RuleExpression OptionalPartyTo(string value) => () => 
        string.IsNullOrWhiteSpace(value) ? null : PartyTo(value)();
}
