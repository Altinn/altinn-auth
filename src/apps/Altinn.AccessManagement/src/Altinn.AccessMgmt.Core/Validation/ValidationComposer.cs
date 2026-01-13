using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Validation;

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
/// A utility class that provides methods for validating data using a series of rules.
/// </summary>
public static class ValidationComposer
{
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
        if (All(rules)() is var r && r is { })
        {
            r(ref builder);
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
        var failures = new List<ValidationRule>();
        foreach (var func in funcs)
        {
            if (func() is var fn && fn is { })
            {
                failures.Add(fn);
            }
        }

        if (failures.Count == 0)
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) =>
        {
            foreach (var result in failures)
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
}
