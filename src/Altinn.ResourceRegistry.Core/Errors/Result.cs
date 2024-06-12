#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.ProblemDetails;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.Errors;

/// <summary>
/// A result of an operation that can either be a value or a problem.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
[DebuggerDisplay("{DebuggerDisplay(),nq}")]
public class Result<T>
{
    private readonly T? _value;
    private readonly ProblemInstance? _problem;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The successful value.</param>
    public Result(T value)
    {
        _value = value;
        _problem = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="problem">The problem descriptor.</param>
    public Result(ProblemInstance problem)
    {
        Guard.IsNotNull(problem);

        _problem = problem;
        _value = default;
    }

    /// <summary>
    /// Checks if the current result is a problem, and returns the problem if it is.
    /// </summary>
    /// <param name="problem"><see cref="ProblemDescriptor"/> if the current result is a problem, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current result is a problem, otherwise <see langword="false"/>.</returns>
    public bool IsProblem([NotNullWhen(true)] out ProblemInstance? problem)
    {
        problem = _problem;
        return problem is not null;
    }

    /// <summary>
    /// Gets the successful value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is a problem, not a value.</exception>
    public T Value
    {
        get
        {
            if (_problem is not null)
            {
                ThrowHelper.ThrowInvalidOperationException("The result is a problem, not a value.");
            }

            return _value!;
        }
    }

    private string DebuggerDisplay()
    {
        if (_problem is not null)
        {
            return $"Problem: {_problem.ErrorCode} {_problem.Detail}";
        }

        return $"Value: {_value}";
    }

    /// <summary>
    /// Implicitly converts a value to a result.
    /// </summary>
    /// <param name="value">The successful value.</param>
    public static implicit operator Result<T>(T value) 
        => new Result<T>(value);

    /// <summary>
    /// Implicitly converts a problem to a result.
    /// </summary>
    /// <param name="problem">The problem descriptor.</param>
    public static implicit operator Result<T>(ProblemInstance problem)
        => new Result<T>(problem);

    /// <summary>
    /// Implicitly converts a problem to a result.
    /// </summary>
    /// <param name="problem">The problem descriptor.</param>
    public static implicit operator Result<T>(ProblemDescriptor problem)
        => new Result<T>(problem);
}
