#nullable enable

using System.Diagnostics;
using System.Numerics;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.Models.Versioned;

/// <summary>
/// A <see cref="IVersionedEntityCondition{T}"/> result.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct VersionedEntityConditionResult
    : IEquatable<VersionedEntityConditionResult>
    , IComparable<VersionedEntityConditionResult>
    , IEqualityOperators<VersionedEntityConditionResult, VersionedEntityConditionResult, bool>
    , IComparisonOperators<VersionedEntityConditionResult, VersionedEntityConditionResult, bool>
{
    private readonly int _value;

    /// <summary>
    /// Request condition matched.
    /// </summary>
    public static VersionedEntityConditionResult Succeeded { get; } = new(0);

    /// <summary>
    /// Request condition did not match, but the condition was an If-Modified-Since.
    /// </summary>
    public static VersionedEntityConditionResult Unmodified { get; } = new(1);

    /// <summary>
    /// Request condition did not match.
    /// </summary>
    public static VersionedEntityConditionResult Failed { get; } = new(2);

    private VersionedEntityConditionResult(int value)
    {
        _value = value;
    }

    /// <inheritdoc/>
    public override string ToString()
        => _value switch
        {
            0 => nameof(Succeeded),
            1 => nameof(Unmodified),
            2 => nameof(Failed),
            _ => ThrowHelper.ThrowInvalidOperationException<string>("Invalid result value"),
        };

    /// <inheritdoc/>
    public int CompareTo(VersionedEntityConditionResult other)
    {
        return _value.CompareTo(other._value);
    }

    /// <summary>
    /// Returns the maximum of two <see cref="VersionedEntityConditionResult"/> values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns><paramref name="left"/> if <c><paramref name="left"/> &gt; <paramref name="right"/></c>, else <paramref name="right"/>.</returns>
    public static VersionedEntityConditionResult Max(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
    {
        return left > right ? left : right;
    }

    /// <inheritdoc/>
    public static bool operator >(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator <(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >=(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) >= 0;
}
