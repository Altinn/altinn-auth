using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class AssignmentA2RoleValidation
{
    internal static RuleExpression HasAssignedA2Roles(IEnumerable<Assignment> a2Assignments, string paramName = "cascade") => () =>
    {
        if (a2Assignments is { } && a2Assignments.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.AssignmentHasActiveConnections, $"QUERY/{paramName}", [new("roles", $"following roles have active assignments [{string.Join(",", a2Assignments.Select(r => r.Role?.Name ?? r.RoleId.ToString()))}].")]);
        }

        return null;
    };
}
