using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class PackageValidation
{
    internal static RuleExpression PackageExists(Package package, string packageName, string paramName = "package") => () =>
    {
        if (package is { })
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) =>
            errors.Add(ValidationErrors.PackageNotExists, $"QUERY/{paramName}", [new("packages", $"No packages with name '{packageName}' do not exists.")]
        );
    };

    internal static RuleExpression AuthorizePackageAssignment(IEnumerable<AccessPackageDto.AccessPackageDtoCheck> packages, string paramName = "packageId") => () =>
    {
        if (packages.Any(p => !p.Result))
        {
            var packageUrns = string.Join(", ", packages.Select(p => p.Package.Urn));
            return (ref ValidationErrorBuilder errors) => errors.Add(ValidationErrors.UserNotAuthorized, $"QUERY/{paramName}", [new("packages", $"User is not allowed to assign the following package(s) '{packageUrns}'.")]);
        }

        return null;
    };

    /// <summary>
    /// Checks the list of packages that all are assignable to the recipient entity type.
    /// </summary>
    /// <param name="packages">list of packages</param>
    /// <param name="assignedToType">entity the assignment is to be made to</param>
    /// <param name="paramName">name of the query parameter</param>
    /// <returns></returns>
    internal static RuleExpression PackageIsAssignableTo(IEnumerable<string> packages, EntityType assignedToType, string paramName = "packageId") => () =>
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentException.ThrowIfNullOrEmpty(paramName);

        if (assignedToType is null)
        {
            throw new ArgumentNullException(nameof(assignedToType));
        }

        var notAssignablePackages = new List<string>();

        if (assignedToType.Id == EntityTypeConstants.Organization)
        {
            foreach (var package in packages)
            {
                PackageConstants.TryGetByAll(package, out var packageObj);
                if (packageObj.Id == PackageConstants.MainAdministrator.Id)
                {
                    notAssignablePackages.Add(package);
                }
            }
        }

        if (notAssignablePackages.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.PackageIsNotAssignableToRecipient, $"QUERY/{paramName}", [new("Packages", $"{string.Join(", ", notAssignablePackages)} are not assignable to an organization.")]);
        }

        return null;
    };

    /// <summary>
    /// Checks the list of packages that all are assignable by the entity type of the from party.
    /// </summary>
    /// <param name="packages">list of packages</param>
    /// <param name="assignedFromType">entity the assignment is to be made from</param>
    /// <param name="paramName">name of the query parameter</param>
    /// <returns></returns>
    internal static RuleExpression PackageIsAssignableFrom(IEnumerable<string> packages, EntityType assignedFromType, string paramName = "packageId") => () =>
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentException.ThrowIfNullOrEmpty(paramName);

        if (assignedFromType is null)
        {
            throw new ArgumentNullException(nameof(assignedFromType));
        }

        var packagesNotAssignableFromOrg = new List<string>();

        foreach (var p in packages)
        {
            if (PackageConstants.TryGetByAll(p, out var package))
            {
                if (package.Entity.EntityTypeId != assignedFromType.Id)
                {
                    packagesNotAssignableFromOrg.Add(package.Entity.Urn);
                }
            }
        }

        if (packagesNotAssignableFromOrg.Any())
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.PackageIsNotAssignable, $"QUERY/{paramName}", [new("Packages", $"{string.Join(", ", packagesNotAssignableFromOrg)} are not assignable from {assignedFromType.Name}.")]);
        }

        return null;
    };

    /// <summary>
    /// Checks if package is assignable.
    /// </summary>
    /// <param name="package">package</param>
    /// <param name="paramName">name of the query parameter</param>
    /// <returns></returns>
    internal static RuleExpression PackageIsAssignable(string package, string paramName = "packageId") => () =>
    {
        if (PackageConstants.TryGetByAll(package, out var packageObj))
        {
            if (!packageObj.Entity.IsAssignable)
            {
                return (ref ValidationErrorBuilder errors) =>
                        errors.Add(ValidationErrors.PackageIsNotAssignable, paramName, [new(paramName, $"Package '{package}' is not assignable.")]);
            }
        }
        else
        {
            return (ref ValidationErrorBuilder errors) =>
                   errors.Add(ValidationErrors.PackageNotExists, paramName, [new(paramName, $"No package was found with value '{package}'.")]);
        }

        return null;
    };

    /// <summary>
    /// Checks if resource is assignable.
    /// </summary>
    /// <param name="resource">resource</param>
    /// <param name="paramName">name of the query parameter</param>
    /// <returns></returns>
    internal static RuleExpression ResourceIsAssignable(Resource resource, string paramName = "resource") => () =>
    {
        if (resource.Type.Name == "MaskinportenSchema")
        {
            return (ref ValidationErrorBuilder errors) =>
                        errors.Add(ValidationErrors.ResourceIsNotDelegable, paramName, [new(paramName, $"Resource '{resource.Name}' is of type 'MaskinportenSchema' and cannot be requested.")]);
        }

        return null;
    };

    /// <summary>
    /// Checks if request is from self
    /// </summary>
    /// <param name="fromId">From</param>
    /// <param name="toId">To</param>
    /// <param name="paramName">name of the query parameter</param>
    /// <returns></returns>
    internal static RuleExpression SelfAssignmentNotAllowed(Guid fromId, Guid toId, string paramName = "from/to") => () =>
    {
        if (fromId == toId)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.RequestFromSelfNotAllowed, $"QUERY/{paramName}", [new("from/to", $"From and To cannot be the same for assignment.")]);
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
