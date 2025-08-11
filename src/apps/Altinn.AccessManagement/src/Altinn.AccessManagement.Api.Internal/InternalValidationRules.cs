using Altinn.AccessMgmt.Core.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Errors;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class InternalValidationRules
{
    /// <summary>
    /// A delegate representing a validation rule that accepts a reference to a <see cref="ValidationErrorBuilder"/>
    /// and adds errors if validation fails.
    /// </summary>
    /// <param name="errors">The reference to the <see cref="ValidationErrorBuilder"/> where validation errors are added.</param>
    public delegate void ValidationRule(ref ValidationErrorBuilder errors);

    /// <summary>
    /// A delegate that returns a validation rule that accepts a reference to a <see cref="ValidationErrorBuilder"/>
    /// and adds errors if validation fails.
    /// </summary>
    public delegate ValidationRule RuleExpression();

    /// <summary>
    /// Validates a series of rules against the provided parameters.
    /// It executes each rule in sequence and returns a <see cref="ValidationProblemInstance"/>
    /// if any validation errors are found, or <c>null</c> if no errors exist.
    /// </summary>
    /// <param name="rules">An array of validation rules to apply.</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> containing validation errors, if any,
    /// or <c>null</c> if the validation passed without any errors.
    /// </returns>
    public static ValidationProblemInstance? Validate(params RuleExpression[] rules)
    {
        var builder = default(ValidationErrorBuilder);
        All(rules)()(ref builder);
        builder.TryBuild(out var result);
        return result;
    }

    /// <summary>
    /// Combines multiple validation rules that must all pass.
    /// </summary>
    /// <param name="funcs">The validation functions to combine.</param>
    /// <returns>A combined validation rule that applies all the specified rules.</returns>
    public static RuleExpression All(params RuleExpression[] funcs) => () =>
    {
        var results = new List<ValidationRule>();
        foreach (var func in funcs)
        {
            if (func() is var fn && fn is { })
            {
                results.Add(fn);
            }
        }

        return (ref ValidationErrorBuilder errors) =>
        {
            foreach (var result in results)
            {
                result(ref errors);
            }
        };
    };

    /// <summary>
    /// Provides validation rules for query parameters.
    /// </summary>
    public static class QueryParameters
    {
        /// <summary>
        /// Checks if any packages exist with given ID.
        /// </summary>
        /// <param name="packages">Lists of packages</param>
        /// <param name="paramName">name of query parameter</param>
        /// <returns></returns>
        internal static RuleExpression AnyPackages(IEnumerable<ConnectionPackage> packages, string paramName = "packageId") => () =>
        {
            ArgumentNullException.ThrowIfNull(packages);
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            if (packages.Any())
            {
                return null;
            }

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidResource, $"QUERY/{paramName}", [new("Packages", $"No packages found.")]);
        };

        /// <summary>
        /// Checks the list of packages all <see cref="Package.IsAssignable"/> is set to true.
        /// </summary>
        /// <param name="packages">list of packages</param>
        /// <param name="paramName">name of the query parameter</param>
        /// <returns></returns>
        internal static RuleExpression PackageIsAssignableByDefinition(IEnumerable<ExtConnectionPackage> packages, string paramName = "packageId") => () =>
        {
            ArgumentNullException.ThrowIfNull(packages);
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            if (packages.All(t => t.Package.IsAssignable))
            {
                return null;
            }

            var packagesNotAssignable = packages
                .Where(p => p.Package.IsAssignable)
                .Select(p => p.Id);

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("Packages", $"{string.Join(",", packagesNotAssignable)} are not assignable.")]);
        };

        /// <summary>
        /// Checks the list of packages that all are assignable to the recipient entity type.
        /// </summary>
        /// <param name="packageUrns">list of packages</param>
        /// <param name="toEntity">entity the assignment is to be made to</param>
        /// <param name="paramName">name of the query parameter</param>
        /// <returns></returns>
        internal static RuleExpression PackageIsAssignableToRecipient(IEnumerable<string> packageUrns, ExtEntity toEntity, string paramName = "packageId") => () =>
        {
            ArgumentNullException.ThrowIfNull(packageUrns);
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            if (toEntity.Type.Id == EntityTypeId.Organization)
            {
                var packagesNotAssignableToOrg = packageUrns
                    .Where(p => p.Equals("urn:altinn:accesspackage:hovedadministrator"))
                    .Select(p => p);

                if (packagesNotAssignableToOrg.Any())
                {
                    return (ref ValidationErrorBuilder errors) =>
                        errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("Packages", $"{string.Join(", ", packagesNotAssignableToOrg)} are not assignable to an organization.")]);
                }
            }

            return null;
        };

        /// <summary>
        /// Checks is packages can be asigned by checking <see cref="ConnectionPackage.CanAssign"/> is set to to true.
        /// </summary>
        /// <param name="packages">List of packages.</param>
        /// <param name="paramName">name of the query parameter.</param>
        /// <returns></returns>
        internal static RuleExpression PackageIsAssignableByUser(IEnumerable<ConnectionPackage> packages, string paramName = "packageId") => () =>
        {
            ArgumentNullException.ThrowIfNull(packages);
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            if (packages.All(t => t.CanAssign))
            {
                return null;
            }

            var packagesNotAssignableByUser = packages
                .Where(p => p.CanAssign)
                .Select(p => p.Id);

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("Packages", $"Packages with IDs {string.Join(",", packagesNotAssignableByUser)} can't be assigned by user.")]);
        };

        /// <summary>
        /// Used to check if package exists by check URN and resulkt of the DB lookup.
        /// </summary>
        /// <param name="packages">List of packags</param>
        /// <param name="packageName">Name of the package.</param>
        /// <param name="paramName">name of the query URN parameter.</param>
        /// <returns></returns>
        internal static RuleExpression PackageUrnLookup(IEnumerable<Package> packages, string packageName, string paramName = "package") => () =>
        {
            ArgumentNullException.ThrowIfNull(packages);
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            if (packages?.Count() == 0)
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("packages", $"No packages were found with the name '{packageName}'.")]
                    );
            }

            if (packages?.Count() == 1)
            {
                return null;
            }

            var msg = string.Join(",", packages.Select(p => p.Id.ToString()));
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("packages", $"Multiple packages were found with the name '{packageName}'.")]
                );
        };

        /// <summary>
        /// Checks if any packages are assigned. Used along with cascade delete
        /// </summary>
        /// <param name="packages">List of packages.</param>
        /// <param name="paramName">name of the query parameter.</param>
        internal static RuleExpression HasPackagesAssigned(IEnumerable<AssignmentPackage> packages, string paramName = "cascade") => () =>
        {
            ArgumentNullException.ThrowIfNull(packages);
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            if (packages.Any())
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, $"QUERY/{paramName}", [new("packages", $"following packages has active assignments [{string.Join(",", packages.Select(p => p.Id.ToString()))}].")]);
            }

            return null;
        };

        internal static RuleExpression AuthorizePackageAssignment(IEnumerable<AccessPackageDto.Check> packages, string paramName = "packageId") => () =>
        {
            if (packages.Any(p => !p.Result))
            {
                var packageUrns = string.Join(", ", packages.Select(p => p.Package.Urn));
                return (ref ValidationErrorBuilder errors) => errors.Add(ValidationErrors.UserNotAuthorized, $"QUERY/{paramName}", [new("packages", $"User is not allowed to assign the following package(s) '{packageUrns}'.")]);
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

        /// <summary>
        /// Checks whether any packages are assigned. Used in conjunction with cascade delete.
        /// </summary>
        /// <param name="packages">List of role assignments.</param>
        /// <param name="roleCode">The name of the role to verify.</param>
        /// <param name="paramNameFrom">The name of the source query parameter.</param>
        /// <param name="paramNameTo">The name of the target query parameter.</param>
        internal static RuleExpression VerifyAssignmentRoleExists(IEnumerable<Assignment> packages, string roleCode, string paramNameFrom = "from", string paramNameTo = "to") => () =>
        {
            if (packages is { } && packages.Any())
            {
                return null;
            }

            return (ref ValidationErrorBuilder errors) =>
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNameFrom}", [new("packages", $"No active role assignments of type '{roleCode}' were found from the value in query parameter '{paramNameFrom}' to the value in query parameter '{paramNameTo}'.")]);
            };
        };

        /// <summary>
        /// Checks if any delegations are given assigned. Used along with cascade delete
        /// </summary>
        /// <param name="delegations">List of delegations.</param>
        /// <param name="paramName">name of the query parameter.</param>
        internal static RuleExpression HasDelegationsAssigned(IEnumerable<Delegation> delegations, string paramName = "cascade") => () =>
        {
            ArgumentNullException.ThrowIfNull(delegations);
            if (delegations.Any())
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.EntityNotExists, $"QUERY/{paramName}", [new("packages", string.Join(",", delegations.Select(p => p.Id.ToString())))]);
            }

            return null;
        };

        /// <summary>
        /// Checks if party exists
        /// </summary>
        /// <returns></returns>
        internal static RuleExpression PartyExists(Entity party, string paramName = "party") => () =>
        {
            if (party is { })
            {
                return null;
            }

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.EntityNotExists, $"QUERY/{paramName}", [new("party", $"Entity do not exists.")]);
        };

        internal static RuleExpression PartyIsEntityType(ExtEntity party, string entityType, string paramName = "party") => () =>
        {
            if (party is { })
            {
                if (party.Type.Name.Equals(entityType, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
            }

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.EntityNotExists, $"QUERY/{paramName}", [new("party", $"Entity do not exists or entity is not of type '{entityType}'.")]);
        };
    }
}
