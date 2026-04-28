using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class AssignmentInstanceValidation
{
    internal static RuleExpression HasAssignedInstances(IEnumerable<AssignmentInstance> instances, string paramName = "cascade") => () =>
    {
        if (instances is { } && instances.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.AssignmentHasActiveConnections, $"QUERY/{paramName}", [new("instances", $"following instances has active assignments [{string.Join(",", instances.Select(p => p.Id.ToString()))}].")]);
        }

        return null;
    };
}
