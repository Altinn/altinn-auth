using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class AssignmentResourceValidation
{
    internal static RuleExpression HasAssignedResources(IEnumerable<AssignmentResource> resources, string paramName = "cascade") => () =>
    {
        if (resources is { } && resources.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.AssignmentHasActiveConnections, $"QUERY/{paramName}", [new("resources", $"following resources has active assignments [{string.Join(",", resources.Select(p => p.Id.ToString()))}].")]);
        }

        return null;
    };
}
