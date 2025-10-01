using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Errors;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
[Obsolete("New Validation in Access.Mgmt.Core")]
public static class EnduserValidationRules
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
    /// Validates a series of rules against the provided parameters.
    /// It executes each rule in sequence and returns a <see cref="ValidationProblemInstance"/>
    /// if any validation errors are found, or <c>null</c> if no errors exist.
    /// </summary>
    /// <param name="rules">An array of validation rules to apply.</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> containing validation errors, if any,
    /// or <c>null</c> if the validation passed without any errors.
    /// </returns>
    public static ValidationProblemInstance? Validate(params ValidationRule[] rules)
    {
        var builder = default(ValidationErrorBuilder);
        foreach (var rule in rules)
        {
            rule(ref builder);
        }

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
    /// Combines multiple validation rules where at least one must pass.
    /// </summary>
    /// <param name="funcs">The validation functions to combine.</param>
    /// <returns>A combined validation rule that applies any of the specified rules.</returns>
    public static RuleExpression Any(params RuleExpression[] funcs) => () =>
    {
        var results = new List<ValidationRule>();
        foreach (var func in funcs)
        {
            if (func() is var fn && fn is { })
            {
                results.Add(fn);
            }
        }

        if (results.Count == funcs.Length)
        {
            return (ref ValidationErrorBuilder errors) =>
            {
                foreach (var result in results)
                {
                    result(ref errors);
                }
            };
        }

        return null;
    };

    /// <summary>
    /// Validates the default query parameters for an end user.
    /// </summary>
    /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
    /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
    /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> with validation errors if any.
    /// </returns>
    public static ValidationProblemInstance EnduserAddConnection(string party, string from, string to) => Validate(
        QueryParameters.Party(party),
        QueryParameters.PartyFrom(from),
        QueryParameters.PartyTo(to),
        QueryParameters.EnduserAddCombination(party, from, to)
    );

    /// <summary>
    /// Validates the default query parameters for an end user.
    /// </summary>
    /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
    /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
    /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
    /// <param name="packageId">The UUID of the package</param>
    /// <param name="packageUrn">The URN of the package</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> with validation errors if any.
    /// </returns>
    public static ValidationProblemInstance EnduserAddConnectionPackage(string party, string from, string to, Guid? packageId, string packageUrn) => Validate(
        QueryParameters.Party(party),
        QueryParameters.PartyFrom(from),
        QueryParameters.PartyTo(to),
        QueryParameters.PackageReference(packageId, packageUrn),
        QueryParameters.EnduserAddCombination(party, from, to)
    );

    /// <summary>
    /// Validates the default query parameters for an end user.
    /// </summary>
    /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
    /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
    /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> with validation errors if any.
    /// </returns>
    public static ValidationProblemInstance EnduserReadConnection(string party, string from, string to) => Validate(
        QueryParameters.Party(party),
        QueryParameters.EnduserReadInputCombination(party, from, to),
        Any(
            QueryParameters.PartyFrom(from),
            QueryParameters.PartyTo(to)
        )
    );

    /// <summary>
    /// Validates the default query parameters for an end user.
    /// </summary>
    /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
    /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
    /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> with validation errors if any.
    /// </returns>
    public static ValidationProblemInstance EnduserRemoveConnection(string party, string from, string to) => Validate(
        QueryParameters.Party(party),
        QueryParameters.PartyFrom(from),
        QueryParameters.PartyTo(to),
        QueryParameters.EnduserRemoveCombination(party, from, to)
    );

    /// <summary>
    /// Validates the default query parameters for an end user.
    /// </summary>
    /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
    /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
    /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
    /// <param name="packageId">The UUID of the package</param>
    /// <param name="packageUrn">The URN of the package</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> with validation errors if any.
    /// </returns>
    public static ValidationProblemInstance EnduserRemoveConnectionPacakge(string party, string from, string to, Guid? packageId, string packageUrn) => Validate(
        QueryParameters.Party(party),
        QueryParameters.PartyFrom(from),
        QueryParameters.PartyTo(to),
        QueryParameters.PackageReference(packageId, packageUrn),
        QueryParameters.EnduserRemoveCombination(party, from, to)
    );

    /// <summary>
    /// Provides validation rules for query parameters.
    /// </summary>
    public static class QueryParameters
    {
        private static IEnumerable<string> ParamKeywords { get; } = ["me", "all"];

        private static string ParamKeywordsToString { get; } = "<me, all | blank, uuid>";

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
        /// Checks the list of packages all <see cref="BasePackage.IsAssignable"/> is set to true.
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
                    errors.Add(ValidationErrors.AssignmentHasActiveConnections, $"QUERY/{paramName}", [new("packages", $"following packages has active assignments [{string.Join(",", packages.Select(p => p.Id.ToString()))}].")]);
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

        /// <summary>
        /// Validates combination of input parameters
        /// </summary>
        internal static RuleExpression EnduserAddCombination(string party, string from, string to) => () =>
        {
            if (!Guid.TryParse(party, out var partyUuid) || partyUuid == Guid.Empty)
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("party", $"Paramater is not a valid a UUID.")]);
            }

            if (!Guid.TryParse(from, out var fromUuid) || fromUuid == Guid.Empty)
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("from", $"Parameter is not a valid UUID.")]);
            }

            if (!Guid.TryParse(to, out var toUuid) || toUuid == Guid.Empty)
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("to", $"Parameter is not a valid UUID.")]);
            }

            if (partyUuid != fromUuid)
            {
                return (ref ValidationErrorBuilder errors) =>
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/from", [new("from", "must match the 'party' UUID.")]);
                };
            }

            return null;
        };

        /// <summary>
        /// Validates that the acting party matches either the 'from' or 'to' party,
        /// and ensures that none of the provided UUIDs are invalid or empty. If any UUID is 
        /// set to "me", it will be replaced by the user's UUID.
        /// </summary>
        /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
        /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
        /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
        /// <returns>
        /// A validation rule that adds an error if any UUID is invalid or empty, 
        /// or if the party does not match either the 'from' or 'to' UUID.
        /// </returns>
        internal static RuleExpression EnduserReadInputCombination(string party, string from, string to) => () =>
        {
            if (!Guid.TryParse(party, out var partyUuid))
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("party", $"The 'party' parameter is not valid.")]);
            }

            if (!string.IsNullOrEmpty(from) && Guid.TryParse(from, out var fromUuid) && fromUuid == partyUuid)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(to) && Guid.TryParse(to, out var toUuid) && toUuid == partyUuid)
            {
                return null;
            }

            return (ref ValidationErrorBuilder errors) =>
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/from", [new("from", "Either the 'from' UUID or the 'to' UUID must match the 'party' UUID. Neither matches the 'party' UUID.")]);
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/to", [new("to", "Either the 'to' UUID or the 'from' UUID must match the 'party' UUID. Neither matches the 'party' UUID.")]);
            };
        };

        /// <summary>
        /// Delete combination
        /// </summary>
        /// <param name="party">The UUID of the acting party (the user acting on behalf). Can also be "me".</param>
        /// <param name="from">The UUID of the 'from' party (could be a person or an organization).</param>
        /// <param name="to">The UUID of the 'to' party (could be a person or an organization).</param>
        /// <returns></returns>
        internal static RuleExpression EnduserRemoveCombination(string party, string from, string to) => () =>
        {
            if (!Guid.TryParse(party, out var partyUuid))
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/party", [new("party", $"The 'party' parameter is not valid.")]);
            }

            if (!Guid.TryParse(from, out var fromUuid) || fromUuid == Guid.Empty)
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/from", [new("from", $"Parameter is not a valid UUID.")]);
            }

            if (!Guid.TryParse(to, out var toUuid) || toUuid == Guid.Empty)
            {
                return (ref ValidationErrorBuilder errors) =>
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/to", [new("to", $"Parameter is not a valid UUID.")]);
            }

            if (partyUuid != fromUuid && partyUuid != toUuid)
            {
                return (ref ValidationErrorBuilder errors) =>
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/from", [new("from", "Either the 'from' UUID or the 'to' UUID must match the 'party' UUID. Neither matches the 'party' UUID.")]);
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/to", [new("to", "Either the 'to' UUID or the 'from' UUID must match the 'party' UUID. Neither matches the 'party' UUID.")]);
                };
            }

            return null;
        };

        /// <summary>
        /// Validates that a party query parameter is either a valid UUID
        /// or matches one of the predefined keywords (e.g., "me", "all") <see cref="ParamKeywords"/>.
        /// </summary>
        /// <param name="party">The value of the party query parameter to validate.</param>
        /// <param name="paramName">The name of the query parameter (used for error reporting).</param>
        /// <param name="values">values to assert</param>
        /// <returns>
        /// A validation rule that adds an error if the party value is not a valid UUID
        /// and does not match any of the predefined keywords <see cref="ParamKeywords"/>.
        /// </returns>
        internal static RuleExpression PartyIs(string party, string paramName = "party", params string[] values) => () =>
        {
            if (Guid.TryParse(party, out var _))
            {
                return null;
            }

            foreach (var value in values)
            {
                if (string.Equals(party, value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
            }

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("party", $"Party '{party}' must be a valid UUID or one of <{ParamKeywordsToString}>.")]);
        };

        /// <summary>
        /// Validates that a party query parameter is either a valid UUID
        /// or matches one of the predefined keywords (e.g., "me", "all") <see cref="ParamKeywords"/>.
        /// </summary>
        /// <param name="party">The value of the party query parameter to validate.</param>
        /// <param name="paramName">The name of the query parameter (used for error reporting).</param>
        /// <returns>
        /// A validation rule that adds an error if the party value is not a valid UUID
        /// and does not match any of the predefined keywords <see cref="ParamKeywords"/>.
        /// </returns>
        internal static RuleExpression Party(string party, string paramName = "party") => () =>
        {
            if (Guid.TryParse(party, out var _))
            {
                return null;
            }

            foreach (var keyword in ParamKeywords)
            {
                if (string.Equals(party, keyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
            }

            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramName}", [new("party", $"Given party of combination of parties must be a valid UUID or one of {ParamKeywordsToString}.")]);
        };

        /// <summary>
        /// Validates the 'from' party query parameter.
        /// The value must be a valid UUID or one of the predefined keywords ("me", "all") <see cref="ParamKeywords"/>.
        /// </summary>
        /// <param name="party">The value of the 'from' query parameter to validate.</param>
        /// <returns>A validation rule that adds an error if validation fails.</returns>
        internal static RuleExpression PartyFrom(string party) => Party(party, "from");

        /// <summary>
        /// Validates the 'to' party query parameter.
        /// The value must be a valid UUID or one of the predefined keywords ("me", "all") <see cref="ParamKeywords"/>.
        /// </summary>
        /// <param name="party">The value of the 'to' query parameter to validate.</param>
        /// <returns>A validation rule that adds an error if validation fails.</returns>
        internal static RuleExpression PartyTo(string party) => Party(party, "to");

        /// <summary>
        /// Validates packages ID
        /// </summary>
        /// <param name="packageId">Package ID.</param>
        /// <param name="packageUrn">Package URN.</param>
        /// <param name="paramNamePackageId">query param name for package UUID.</param>
        /// <param name="paramNamePackage">query param name for package URN.</param>
        internal static RuleExpression PackageReference(Guid? packageId, string packageUrn, string paramNamePackageId = "packageId", string paramNamePackage = "package") => () =>
        {
            if ((packageId.HasValue && packageId != Guid.Empty) != !string.IsNullOrEmpty(packageUrn))
            {
                return null;
            }

            if (packageId.HasValue && !string.IsNullOrEmpty(packageUrn))
            {
                return (ref ValidationErrorBuilder errors) =>
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackageId}", [new("package", "Provide either a package URN or a package ID, not both.")]);
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackage}", [new("package", "Provide either a package URN or a package ID, not both.")]);
                };
            }

            if (packageId.HasValue && packageId.Value == Guid.Empty)
            {
                return (ref ValidationErrorBuilder errors) =>
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackageId}", [new("package", $"The provided package ID '{packageId}' is an empty UUID, which is invalid.")]);
                };
            }

            return (ref ValidationErrorBuilder errors) =>
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackageId}", [new("package", "Either a package URN or a package ID must be provided.")]);
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackage}", [new("package", "Either a package URN or a package ID must be provided.")]);
            };
        };

        /// <summary>
        /// Validates packages ID
        /// </summary>
        /// <param name="packageIds">Package ID.</param>
        /// <param name="packageUrns">Package URN.</param>
        /// <param name="paramNamePackageId">query param name for package UUID.</param>
        /// <param name="paramNamePackage">query param name for package URN.</param>
        internal static RuleExpression PackageReferences(IEnumerable<Guid>? packageIds, IEnumerable<string> packageUrns, string paramNamePackageId = "packageIds", string paramNamePackage = "packages") => () =>
        {
            bool hasIds = packageIds?.Any() == true;
            bool hasUrns = packageUrns?.Any() == true;

            // Early success if only one of them is present and valid
            if ((hasIds ^ hasUrns) && (!hasIds || packageIds!.All(g => g != Guid.Empty)))
            {
                return null;
            }

            return (ref ValidationErrorBuilder errors) =>
            {
                // Both present
                if (hasIds && hasUrns)
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackageId}", [new("package", "Either list of package URNs or a package IDs must be provided, not both.")]);
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackage}", [new("package", "Either list of package URNs or a package IDs must be provided, not both.")]);
                }

                // Both missing
                if (!hasIds && !hasUrns)
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackageId}", [new("package", "Either a package URN or a package ID must be provided.")]);
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackage}", [new("package", "Either a package URN or a package ID must be provided.")]);
                }

                // Invalid Guids
                if (hasIds && packageIds!.Any(g => g == Guid.Empty))
                {
                    errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{paramNamePackageId}", [new("package", "Package IDs must be non-empty GUIDs.")]);
                }
            };
        };
    }
}
