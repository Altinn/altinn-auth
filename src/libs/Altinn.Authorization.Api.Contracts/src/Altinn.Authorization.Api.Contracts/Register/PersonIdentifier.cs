using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
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
[SwaggerString(Format = "ssn", Pattern = "^[0-9]{11}$")]
[JsonConverter(typeof(JsonConverter))]
public sealed class PersonIdentifier
    : IParsable<PersonIdentifier>
    , ISpanParsable<PersonIdentifier>
    , IFormattable
    , ISpanFormattable
    , IExampleDataProvider<PersonIdentifier>
    , IEquatable<PersonIdentifier>
    , IEquatable<string>
    , IEqualityOperators<PersonIdentifier, PersonIdentifier, bool>
    , IEqualityOperators<PersonIdentifier, string, bool>
{
    private const int LENGTH = 11;
    private static readonly SearchValues<char> NUMBERS = SearchValues.Create(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);

    private readonly string _value;

    private PersonIdentifier(string value)
    {
        _value = value;
    }

    /// <inheritdoc/>
    public static IEnumerable<PersonIdentifier>? GetExamples(ExampleDataOptions options)
    {
        yield return Parse("02013299997");
        yield return Parse("30108299939");
        yield return Parse("42013299980");
    }

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/>
    public static PersonIdentifier Parse(string s)
        => Parse(s, provider: null);

    /// <inheritdoc/>
    public static PersonIdentifier Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
        ? result
        : throw new FormatException("Invalid SSN");

    /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)"/>
    public static PersonIdentifier Parse(ReadOnlySpan<char> s)
        => Parse(s, provider: null);

    /// <inheritdoc/>
    public static PersonIdentifier Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
        ? result
        : throw new FormatException("Invalid SSN");

    /// <inheritdoc/>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out PersonIdentifier result)
        => TryParse(s.AsSpan(), s, out result);

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out PersonIdentifier result)
        => TryParse(s, original: null, out result);

    private static bool TryParse(ReadOnlySpan<char> s, string? original, [MaybeNullWhen(false)] out PersonIdentifier result)
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

        if (!IsValidPersonIdentifier(s))
        {
            result = null;
            return false;
        }

        result = new PersonIdentifier(original ?? new string(s));
        return true;

        // Note: this is using the new algorithm for validating person identifiers
        // the new will only be used to generate new person identifiers starting
        // in 2032, but it can still validate old person identifiers.
        static bool IsValidPersonIdentifier(ReadOnlySpan<char> s)
        {
            Vector256<ushort> k1weights = Vector256.Create((ushort)3, 7, 6, 1, 8, 9, 4, 5, 2, 0, 0, 0, 0, 0, 0, 0);
            Vector256<ushort> k2weights = Vector256.Create((ushort)5, 4, 3, 2, 7, 6, 5, 4, 3, 2, 0, 0, 0, 0, 0, 0);
            Vector256<ushort> digits = CreateVector(s);

            var k1c_base = (ushort)(Vector256.Sum(digits * k1weights) % 11);
            var k1c_1 = (ushort)((11 - k1c_base) % 11);
            var k1c_2 = (ushort)((12 - k1c_base) % 11);
            var k1c_3 = (ushort)((13 - k1c_base) % 11);
            var k1c_4 = (ushort)((14 - k1c_base) % 11);
            var k2c = (ushort)((11 - Vector256.Sum(digits * k2weights) % 11) % 11);

            var k1 = digits[9];
            var k2 = digits[10];

            return (k1 == k1c_1 | k1 == k1c_2 | k1 == k1c_3 | k1 == k1c_4) & k2 == k2c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<ushort> CreateVector(ReadOnlySpan<char> s)
        {
            Debug.Assert(s.Length == 11);

            Span<ushort> c = stackalloc ushort[16];
            c.Clear(); // zero out the vector
            MemoryMarshal.Cast<char, ushort>(s).CopyTo(c);

            var chars = Vector256.Create<ushort>(c);
            var zeros = Vector256.Create('0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', 0, 0, 0, 0, 0);

            return chars - zeros;
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
    public bool Equals(PersonIdentifier? other)
        => ReferenceEquals(this, other) || other is not null && _value == other._value;

    /// <inheritdoc/>
    public bool Equals(string? other)
        => other is not null && _value == other;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj switch
        {
            PersonIdentifier other => Equals(other),
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
    public static bool operator ==(PersonIdentifier? left, PersonIdentifier? right)
        => ReferenceEquals(left, right) || (left?.Equals(right) ?? right is null);

    /// <inheritdoc/>
    public static bool operator !=(PersonIdentifier? left, PersonIdentifier? right)
        => !(left == right);

    /// <inheritdoc/>
    public static bool operator ==(PersonIdentifier? left, string? right)
        => left?.Equals(right) ?? right is null;

    /// <inheritdoc/>
    public static bool operator !=(PersonIdentifier? left, string? right)
        => !(left == right);

    /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality"/>
    public static bool operator ==(string? left, PersonIdentifier? right)
        => right?.Equals(left) ?? left is null;

    /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality"/>
    public static bool operator !=(string? left, PersonIdentifier? right)
        => !(left == right);

    private sealed class JsonConverter : JsonConverter<PersonIdentifier>
    {
        public override PersonIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (!TryParse(str, null, out var result))
            {
                throw new JsonException("Invalid SSN");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, PersonIdentifier value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._value);
        }
    }
}
