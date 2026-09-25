using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Altinn.ResourceRegistry.Core.Aggregates;

/// <summary>
/// A event id that is either a db-defined id, or the special value unset.
/// </summary>
/// <param name="id">The db id</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct EventId(ulong id)
    : IEquatable<EventId>
{
    /// <summary>
    /// Get's the nullable value for the event id.
    /// </summary>
    public readonly ulong? Value
        => id == 0 ? null : id;

    /// <summary>
    /// Get's a value for the event id, assuming it is not unset.
    /// </summary>
    public readonly ulong UnsafeValue
        => id;

    /// <summary>
    /// Get's the DB value for the event id.
    /// </summary>
    public readonly long DbValue
    {
        get
        {
            Debug.Assert(id != 0);

            return checked((long)id);
        }
    }

    /// <summary>
    /// Gets whether the value is set.
    /// </summary>
    public bool IsSet => id != 0;

    /// <summary>
    /// Gets the special unset event id.
    /// </summary>
    public static EventId Unset => default;

    /// <inheritdoc/>
    public override string ToString()
        => IsSet ? id.ToString(CultureInfo.InvariantCulture) : "unset";

    [DebuggerHidden]
    private string DebuggerDisplay
        => IsSet ? id.ToString(CultureInfo.InvariantCulture) : "unset";

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is EventId other && Equals(other);
    }

    /// <inheritdoc/>
    public bool Equals(EventId other)
    {
        return UnsafeValue == other.UnsafeValue;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public static bool operator ==(EventId left, EventId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EventId left, EventId right)
    {
        return !(left == right);
    }
}
