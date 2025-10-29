using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Errors;

/// <summary>
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
[Obsolete("New Validation in Altinn.AccessMgmt.Core/Services/Validation")]
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
    }
}
