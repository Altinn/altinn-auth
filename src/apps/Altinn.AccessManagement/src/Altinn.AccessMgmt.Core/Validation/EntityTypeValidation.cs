using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class EntityTypeValidation
{
    internal static RuleExpression IsOfType(Guid entityType, IEnumerable<Guid> entityTypeIds, string paramName) => () =>
    {
        ArgumentNullException.ThrowIfNull(entityTypeIds);
        if (entityTypeIds.Contains(entityType))
        {
            return null;
        }

        var allowedEntityTypeNames = EntityTypeConstants.AllEntities()
            .Where(e => entityTypeIds.Contains(e))
            .Select(e => e.Entity.Name);
        var allowedEntityTypes = string.Join(",", allowedEntityTypeNames);

        var entityTypeName = "Unkown";
        if (EntityTypeConstants.TryGetById(entityType, out var e))
        {
            entityTypeName = e.Entity.Name;
        }
        
        return (ref ValidationErrorBuilder errors) => errors.Add(ValidationErrors.DisallowedEntityType, $"QUERY/{paramName}", [new("type", $"Got '{entityTypeName}', expected one of: '{allowedEntityTypes}'.")]);
    };

    internal static RuleExpression FromIsOfType(Guid party, params Guid[] entityTypeNames) => () =>
    {
        return IsOfType(party, entityTypeNames, "from")();
    };

    internal static RuleExpression ToIsOfType(Guid party, params Guid[] entityTypeNames) => () =>
    {
        return IsOfType(party, entityTypeNames, "to")();
    };
}
