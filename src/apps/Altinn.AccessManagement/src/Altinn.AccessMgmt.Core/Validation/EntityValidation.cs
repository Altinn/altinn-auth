using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class EntityValidation
{
    /// <summary>
    /// Validates that the acting party matches either the 'from' or 'to' party,
    /// and ensures that none of the provided UUIDs are invalid or empty. If any UUID is 
    /// set to "me", it will be replaced by the user's UUID.
    /// </summary>
    /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
    /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
    /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
    /// <returns>
    /// A validation rule that adds an error if any UUID is invalid or empty, 
    /// or if the party does not match either the 'from' or 'to' UUID.
    /// </returns>
    public static RuleExpression ReadOp(string party, string from, string to) => () =>
    {
        if (!Guid.TryParse(party, out var partyUuid))
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("party", $"The 'party' parameter is not valid.")]);
        }

        if (!string.IsNullOrEmpty(from) && Guid.TryParse(from, out var fromUuid) && fromUuid == partyUuid)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(to) && Guid.TryParse(to, out var toUuid) && toUuid == partyUuid)
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) =>
        {
            errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/from", [new("from", "Either the 'from' UUID or the 'to' UUID must match the 'party' UUID. Neither matches the 'party' UUID.")]);
            errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/to", [new("to", "Either the 'to' UUID or the 'from' UUID must match the 'party' UUID. Neither matches the 'party' UUID.")]);
        };
    };

    internal static RuleExpression EntityExists(Entity party, string paramName) => () =>
    {
        if (party is { })
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) => errors.Add(ValidationErrors.EntityNotExists, $"QUERY/{paramName}");
    };

    internal static RuleExpression FromExists(Entity party) => () =>
    {
        return EntityExists(party, "from")();
    };

    internal static RuleExpression ToExists(Entity party) => () =>
    {
        return EntityExists(party, "to")();
    };
}
