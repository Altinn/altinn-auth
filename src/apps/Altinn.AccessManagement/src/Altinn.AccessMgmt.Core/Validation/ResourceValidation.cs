using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class ResourceValidation
{
    internal static RuleExpression ResourceExists(Resource resource, string resourceName, string paramName = "resource") => () =>
    {
        if (resource is { })
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) =>
            errors.Add(ValidationErrors.ResourceNotExists, $"QUERY/{paramName}", [new("resources", $"No resources with name '{resourceName}' exists.")]
        );
    };

    internal static RuleExpression AuthorizeResourceAssignment(IEnumerable<ResourceDto.ResourceDtoCheck> resources, string paramName = "resource") => () =>
    {
        if (resources.Any(p => !p.Result))
        {
            var resourceKeys = string.Join(", ", resources.Select(p => p.Resource.RefId));
            return (ref ValidationErrorBuilder errors) => errors.Add(ValidationErrors.UserNotAuthorized, $"QUERY/{paramName}", [new("resources", $"User is not allowed to assign the following resource(s) '{resourceKeys}'.")]);
        }

        return null;
    };

    /// <summary>
    /// Checks the list of packages that all are assignable to the recipient entity type.
    /// </summary>
    /// <param name="packageUrns">list of packages</param>
    /// <param name="toEntity">entity the assignment is to be made to</param>
    /// <param name="paramName">name of the query parameter</param>
    /// <returns></returns>
    internal static RuleExpression PackageIsAssignableToRecipient(IEnumerable<string> packageUrns, EntityType toEntity, string paramName = "packageId") => () =>
    {
        ArgumentNullException.ThrowIfNull(packageUrns);
        ArgumentException.ThrowIfNullOrEmpty(paramName);

        if (toEntity.Id == EntityTypeConstants.Organization)
        {
            var packagesNotAssignableToOrg = packageUrns
                .Where(p => p.Equals(PackageConstants.MainAdministrator.Entity.Urn));

            if (packagesNotAssignableToOrg.Any())
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.PackageIsNotAssignableToRecipient, $"QUERY/{paramName}", [new("Packages", $"{string.Join(", ", packagesNotAssignableToOrg)} are not assignable to an organization.")]);
            }
        }

        return null;
    };

    /// <summary>
    /// Used to check if package exists by check URN and resulkt of the DB lookup.
    /// </summary>
    /// <param name="packageLookupResult">Lookup result of packages based on input</param>
    /// <param name="packageName">Name of the package.</param>
    /// <param name="paramName">name of the query URN parameter.</param>
    /// <returns></returns>
    internal static RuleExpression PackageUrnLookup(IEnumerable<Package> packageLookupResult, IEnumerable<string> packageName, string paramName = "package") => () =>
    {
        ArgumentNullException.ThrowIfNull(packageLookupResult);
        ArgumentException.ThrowIfNullOrEmpty(paramName);

        if (!packageLookupResult.Any())
        {
            var msg = string.Join(",", packageName.Select(p => p.ToString()));
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("packages", $"No packages were found with the names '{msg}'.")]
            );
        }

        if (packageLookupResult.Count() != packageName.Count())
        {
            var pkgsNotFound = packageName.Where(n => packageLookupResult.Any(p => p.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase)));
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("packages", $"Packages with name(s) was not found '{pkgsNotFound}'.")]
            );
        }

        return null;
    };
}
