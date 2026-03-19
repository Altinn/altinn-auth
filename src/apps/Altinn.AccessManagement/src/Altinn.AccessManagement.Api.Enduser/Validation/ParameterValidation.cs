using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// Validation rules for individual connection query parameters.
/// Keywords (e.g. 'me', 'all') are recognized for forward compatibility, but are not yet active.
/// Clients should currently only send GUID values.
/// </summary>
internal static class ParameterValidation
{
    private static readonly string[] PartyKeywords = ["me"];

    private static readonly string[] FromToPartyKeywords = ["me", "all"];

    /// <summary>
    /// party must be a non-empty Guid.
    /// </summary>
    /// <returns>A deferred rule expression that yields an error builder when invalid, otherwise null.</returns>
    internal static RuleExpression Party(string value) => () =>
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (IsKeyword(value, PartyKeywords))
            {
                return null;
            }

            if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
            {
                return null;
            }
        }

        return (ref ValidationErrorBuilder errors) =>
            errors.Add(ValidationErrors.InvalidQueryParameter, "$QUERY/party", [new("party", ValidationErrorMessageTexts.InvalidPartyValue)]);
    };

    /// <summary>
    /// <paramref name="value"/> has to be non-empty Guid if valueRequired is true.
    /// </summary>
    /// <param name="value">Raw query parameter value.</param>
    /// <param name="paramName">Parameter name used in error path.</param>
    /// <returns>A deferred rule expression that yields an error builder when invalid, otherwise null.</returns>
    internal static RuleExpression ToIsGuid(Guid? value, string paramName = "to") => () =>
    {
        if (value is null || value == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"$QUERY/{paramName}", [new(paramName, ValidationErrorMessageTexts.InvalidPartyValue)]);
        }

        return null;
    };

    /// <summary>
    /// <paramref name="value"/> has to be non-empty Guid if valueRequired is true.
    /// </summary>
    /// <param name="value">Raw query parameter value.</param>
    /// <param name="paramName">Parameter name used in error path.</param>
    /// <returns>A deferred rule expression that yields an error builder when invalid, otherwise null.</returns>
    internal static RuleExpression PartyFrom(string value, string paramName = "from") =>
        ValidateFromOrToParty(value, paramName);

    /// <summary>
    /// <paramref name="value"/> has to be non-empty Guid if valueRequired is true.
    /// </summary>
    /// <param name="value">Raw query parameter value.</param>
    /// <param name="paramName">Parameter name used in error path.</param>
    /// <returns>A deferred rule expression that yields an error builder when invalid, otherwise null.</returns>
    internal static RuleExpression PartyTo(string value, string paramName = "to") =>
        ValidateFromOrToParty(value, paramName);

    /// <summary>
    /// personIdentifier must be present and pass <see cref="PersonIdentifier.TryParse(string?, IFormatProvider?, out PersonIdentifier)"/>,
    /// lastName must be non-empty.
    /// </summary>
    /// <param name="personIdentifier">The personIdentifier value of a PersonInput parameter</param>
    /// <param name="personLastName">The lastName value of a PersonInput parameter</param>
    /// <returns>A deferred rule expression that yields an error builder when invalid, otherwise null.</returns>
    internal static RuleExpression PersonInput(string personIdentifier, string personLastName) => () =>
    {
        if (string.IsNullOrWhiteSpace(personIdentifier))
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "/personIdentifier", [new("personIdentifier", ValidationErrorMessageTexts.PersonIdentifierRequired)]);
        }

        if (string.IsNullOrWhiteSpace(personLastName))
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "/lastName", [new("lastName", ValidationErrorMessageTexts.LastNameRequired)]);
        }

        return null;
    };

    /// <summary>
    /// Validates that either 'to' query parameter OR PersonInput in body is provided (mutually exclusive).
    /// Also validates that 'to' is not Guid.Empty when provided, and that DirectRightKeys contains at least one right key.
    /// Used for instance rights delegation to support both existing connections and new rightholder creation.
    /// </summary>
    /// <param name="to">Optional 'to' query parameter for existing connections</param>
    /// <param name="toInput">Optional PersonInput in body for creating new rightholder</param>
    /// <param name="directRightKeys">Right keys to delegate (must contain at least one element)</param>
    /// <returns>A deferred rule expression that yields an error builder when invalid, otherwise null.</returns>
    internal static RuleExpression InstanceRightsDelegationInput(Guid? to, PersonInputDto? toInput, IEnumerable<string>? directRightKeys) => () =>
    {
        if (to.HasValue && toInput != null)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "$QUERY/to", 
                    [new("to", ValidationErrorMessageTexts.ToParameterConflict)]);
        }

        if (!to.HasValue && toInput == null)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "$QUERY/to", 
                    [new("to", ValidationErrorMessageTexts.ToParameterRequired)]);
        }

        // Validate 'to' is not Guid.Empty when provided
        if (to.HasValue && to.Value == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "$QUERY/to", 
                    [new("to", ValidationErrorMessageTexts.InvalidPartyValue)]);
        }

        if (directRightKeys == null || !directRightKeys.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.Required, "/directRightKeys",
                    [new("directRightKeys", ValidationErrorMessageTexts.DirectRightKeysRequired)]);
        }

        return null;
    };

    private static RuleExpression ValidateFromOrToParty(string value, string paramName) => () =>
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            return null;
        }
        else if (IsKeyword(value, FromToPartyKeywords))
        {
            return null;
        }
        else
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"$QUERY/{paramName}", [new(paramName, ValidationErrorMessageTexts.InvalidPartyValue)]);
        }
    };

    private static bool IsKeyword(string? value, string[] keywords) =>
        !string.IsNullOrWhiteSpace(value) &&
        Array.Exists(keywords, k => string.Equals(value, k, StringComparison.OrdinalIgnoreCase));
}
