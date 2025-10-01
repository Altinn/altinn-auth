using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class RoleValidation
{
    internal static RuleExpression RoleExists(Role party, string paramName) => () =>
    {
        if (party is { })
        { 
            return null;
        }

        return (ref ValidationErrorBuilder errors) => errors.Add(ValidationErrors.RoleNotExists, $"QUERY/{paramName}");
    };
}
