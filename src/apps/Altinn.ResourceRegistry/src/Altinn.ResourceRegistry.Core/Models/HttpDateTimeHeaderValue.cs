using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Altinn.ResourceRegistry.Core.Extensions;

namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// The value of a http date-time header like <c>Last-Modified</c>
/// or <c>If-Modified-Since</c>. Consists of a <see cref="DateTimeOffset"/>
/// that is rounded down to the nearest second and converted to UTC.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct HttpDateTimeHeaderValue
    : IEquatable<HttpDateTimeHeaderValue>
    , IEquatable<DateTimeOffset>
    , IComparable<HttpDateTimeHeaderValue>
    , IComparable<DateTimeOffset>
    , IEqualityOperators<HttpDateTimeHeaderValue, HttpDateTimeHeaderValue, bool>
    , IEqualityOperators<HttpDateTimeHeaderValue, DateTimeOffset, bool>
    , IComparisonOperators<HttpDateTimeHeaderValue, HttpDateTimeHeaderValue, bool>
    , IComparisonOperators<HttpDateTimeHeaderValue, DateTimeOffset, bool>
{
    private readonly static TimeSpan Precision = TimeSpan.FromSeconds(1);

    private readonly DateTimeOffset _value;

    /// <summary>
    /// Gets the value as a <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset Value => _value;

    /// <summary>
    /// Constructs a new <see cref="HttpDateTimeHeaderValue"/>
    /// from a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public HttpDateTimeHeaderValue(DateTimeOffset value)
    {
        _value = value.ToOffset(TimeSpan.Zero).RoundDown(Precision);
    }

    /// <inheritdoc/>
    public int CompareTo(HttpDateTimeHeaderValue other)
        => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(DateTimeOffset other)
        => CompareTo(new HttpDateTimeHeaderValue(other));

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object obj)
        => obj switch
        {
            HttpDateTimeHeaderValue other => Equals(other),
            DateTimeOffset other => Equals(other),
            _ => false,
        };

    /// <inheritdoc/>
    public bool Equals(HttpDateTimeHeaderValue other)
        => _value.Equals(other._value);

    /// <inheritdoc/>
    public bool Equals(DateTimeOffset other)
        => Equals(new HttpDateTimeHeaderValue(other));

    /// <inheritdoc/>
    public override int GetHashCode()
        => _value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString()
        => _value.ToString("r", CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public static bool operator ==(HttpDateTimeHeaderValue left, HttpDateTimeHeaderValue right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(HttpDateTimeHeaderValue left, HttpDateTimeHeaderValue right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public static bool operator ==(HttpDateTimeHeaderValue left, DateTimeOffset right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(HttpDateTimeHeaderValue left, DateTimeOffset right)
        => !left.Equals(right);

    public static bool operator ==(DateTimeOffset left, HttpDateTimeHeaderValue right)
        => right.Equals(left);

    public static bool operator !=(DateTimeOffset left, HttpDateTimeHeaderValue right)
        => !right.Equals(left);

    /// <inheritdoc/>
    public static bool operator <(HttpDateTimeHeaderValue left, HttpDateTimeHeaderValue right)
        => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator >(HttpDateTimeHeaderValue left, HttpDateTimeHeaderValue right)
        => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator <=(HttpDateTimeHeaderValue left, HttpDateTimeHeaderValue right)
        => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >=(HttpDateTimeHeaderValue left, HttpDateTimeHeaderValue right)
        => left.CompareTo(right) >= 0;

    /// <inheritdoc/>
    public static bool operator <(HttpDateTimeHeaderValue left, DateTimeOffset right)
        => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator >(HttpDateTimeHeaderValue left, DateTimeOffset right)
        => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator <=(HttpDateTimeHeaderValue left, DateTimeOffset right)
        => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >=(HttpDateTimeHeaderValue left, DateTimeOffset right)
        => left.CompareTo(right) >= 0;

    public static bool operator <(DateTimeOffset left, HttpDateTimeHeaderValue right)
        => right.CompareTo(left) > 0;

    public static bool operator >(DateTimeOffset left, HttpDateTimeHeaderValue right)
        => right.CompareTo(left) < 0;

    public static bool operator <=(DateTimeOffset left, HttpDateTimeHeaderValue right)
        => right.CompareTo(left) >= 0;

    public static bool operator >=(DateTimeOffset left, HttpDateTimeHeaderValue right)
        => right.CompareTo(left) <= 0;
}
