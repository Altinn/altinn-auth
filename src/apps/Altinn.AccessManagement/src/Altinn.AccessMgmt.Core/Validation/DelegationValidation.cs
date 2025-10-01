using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class DelegationValidation
{
    /// <summary>
    /// Checks if any delegations are given assigned. Used along with cascade delete
    /// </summary>
    /// <param name="delegations">List of delegations.</param>
    /// <param name="paramName">name of the query parameter.</param>
    internal static RuleExpression HasDelegationsAssigned(IEnumerable<Delegation> delegations, string paramName = "cascade") => () =>
    {
        if (delegations is { } && delegations.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.AssignmentHasActiveConnections, $"QUERY/{paramName}", [new("delegations", string.Join(",", delegations.Select(p => p.Id.ToString())))]);
        }

        return null;
    };
}
