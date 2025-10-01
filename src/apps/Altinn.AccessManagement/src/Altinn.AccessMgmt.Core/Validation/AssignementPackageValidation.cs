using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class AssignementPackageValidation
{
    internal static RuleExpression HasAssignedPackages(IEnumerable<AssignmentPackage> packages, string paramName = "cascade") => () =>
    {
        if (packages is { } && packages.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.AssignmentHasActiveConnections, $"QUERY/{paramName}", [new("packages", $"following packages has active assignments [{string.Join(",", packages.Select(p => p.Id.ToString()))}].")]);
        }

        return null;
    };
}
