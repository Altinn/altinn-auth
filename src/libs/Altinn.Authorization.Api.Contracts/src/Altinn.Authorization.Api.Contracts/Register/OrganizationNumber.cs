#nullable enable
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Swashbuckle.Examples;
using Altinn.Swashbuckle.Filters;

namespace Altinn.Authorization.Api.Contracts.Register;

/// <summary>
/// A organization number (a string of 9 digits).
/// </summary>
[SwaggerString(Format = "org-no", Pattern = "^[0-9]{9}$")]
[JsonConverter(typeof(JsonConverter))]
public sealed class OrganizationNumber
    : IParsable<OrganizationNumber>
    , ISpanParsable<OrganizationNumber>
    , IFormattable
    , ISpanFormattable
    , IExampleDataProvider<OrganizationNumber>
    , IEquatable<OrganizationNumber>
    , IEquatable<string>
    , IEqualityOperators<OrganizationNumber, OrganizationNumber, bool>
    , IEqualityOperators<OrganizationNumber, string, bool>
{
    private const int LENGTH = 9;
    private static readonly SearchValues<char> NUMBERS = SearchValues.Create(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);

    private readonly string _value;

    private OrganizationNumber(string value)
    {
        _value = value;
    }

    /// <inheritdoc/>
    public static IEnumerable<OrganizationNumber>? GetExamples(ExampleDataOptions options)
    {
        yield return Parse("937884117");
        yield return Parse("923609016");
    }

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/>
    public static OrganizationNumber Parse(string s)
        => Parse(s, provider: null);

    /// <inheritdoc/>
    public static OrganizationNumber Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
        ? result
        : throw new FormatException("Invalid organization number");

    /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)"/>
    public static OrganizationNumber Parse(ReadOnlySpan<char> s)
        => Parse(s, provider: null);

    /// <inheritdoc/>
    public static OrganizationNumber Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
        ? result
        : throw new FormatException("Invalid organization number");

    /// <inheritdoc/>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out OrganizationNumber result)
        => TryParse(s.AsSpan(), s, out result);

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out OrganizationNumber result)
        => TryParse(s, original: null, out result);

    private static bool TryParse(ReadOnlySpan<char> s, string? original, [MaybeNullWhen(false)] out OrganizationNumber result)
    {
        if (s.Length != LENGTH)
        {
            result = null;
            return false;
        }

        if (s.ContainsAnyExcept(NUMBERS))
        {
            result = null;
            return false;
        }

        if (!IsValidOrganizationIdentifier(s))
        {
            result = null;
            return false;
        }

        result = new OrganizationNumber(original ?? new string(s));
        return true;

        static bool IsValidOrganizationIdentifier(ReadOnlySpan<char> s)
        {
            ReadOnlySpan<ushort> chars = MemoryMarshal.Cast<char, ushort>(s);
            Vector128<ushort> weights = Vector128.Create((ushort)3, 2, 7, 6, 5, 4, 3, 2);

            Vector128<ushort> zeroDigit = Vector128.Create('0', '0', '0', '0', '0', '0', '0', '0');
            Vector128<ushort> charsVec = Vector128.Create(chars);

            var sum = Vector128.Sum((charsVec - zeroDigit) * weights);

            var ctrlDigit = (11 - (sum % 11)) % 11;

            var currentDigit = chars[8] - '0';
            return currentDigit == ctrlDigit;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
        => _value;

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    public string ToString(string? format)
        => _value;

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
        => _value;

    /// <inheritdoc/>
    public bool Equals(OrganizationNumber? other)
        => ReferenceEquals(this, other) || (other is not null && _value == other._value);

    /// <inheritdoc/>
    public bool Equals(string? other)
        => other is not null && _value == other;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj switch
        {
            OrganizationNumber other => Equals(other),
            string other => Equals(other),
            _ => false,
        };

    /// <inheritdoc/>
    public override int GetHashCode()
        => _value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < _value.Length)
        {
            charsWritten = 0;
            return false;
        }

        _value.AsSpan().CopyTo(destination);
        charsWritten = _value.Length;
        return true;
    }

    /// <inheritdoc/>
    public static bool operator ==(OrganizationNumber? left, OrganizationNumber? right)
        => ReferenceEquals(left, right) || (left?.Equals(right) ?? right is null);

    /// <inheritdoc/>
    public static bool operator !=(OrganizationNumber? left, OrganizationNumber? right)
        => !(left == right);

    /// <inheritdoc/>
    public static bool operator ==(OrganizationNumber? left, string? right)
        => left?.Equals(right) ?? right is null;

    /// <inheritdoc/>
    public static bool operator !=(OrganizationNumber? left, string? right)
        => !(left == right);

    /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality"/>
    public static bool operator ==(string? left, OrganizationNumber? right)
        => right?.Equals(left) ?? left is null;

    /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality"/>
    public static bool operator !=(string? left, OrganizationNumber? right)
        => !(left == right);

    private sealed class JsonConverter : JsonConverter<OrganizationNumber>
    {
        public override OrganizationNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (!TryParse(str, null, out var result))
            {
                throw new JsonException("Invalid organization number");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, OrganizationNumber value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._value);
        }
    }
}
