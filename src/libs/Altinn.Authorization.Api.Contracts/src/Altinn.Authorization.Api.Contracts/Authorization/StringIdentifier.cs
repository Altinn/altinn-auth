#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// Abstract base class for string-based identifier types that implement
/// <see cref="ISpanParsable{TSelf}"/> and <see cref="ISpanFormattable"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete identifier type, which must also implement <see cref="IStringIdentifierFactory{TSelf}"/>.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class StringIdentifier<TSelf>
    : ISpanParsable<TSelf>,
    ISpanFormattable
    where TSelf : StringIdentifier<TSelf>, IStringIdentifierFactory<TSelf>
{
    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringIdentifier{TSelf}"/> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    protected StringIdentifier(string value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the underlying string value.
    /// </summary>
    protected internal string Value => _value;

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/>
    public static TSelf Parse(string s)
        => Parse(s, provider: null);

    /// <inheritdoc/>
    public static TSelf Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
        ? result
        : throw new FormatException($"Invalid {typeof(TSelf).Name}");

    /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)"/>
    public static TSelf Parse(ReadOnlySpan<char> s)
        => Parse(s, provider: null);

    /// <inheritdoc/>
    public static TSelf Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
        ? result
        : throw new FormatException($"Invalid {typeof(TSelf).Name}");

    /// <inheritdoc/>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result)
        => TryParse(s.AsSpan(), s, out result);

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result)
        => TryParse(s, original: null, out result);

    private static bool TryParse(ReadOnlySpan<char> s, string? original, [MaybeNullWhen(false)] out TSelf result)
    {
        result = TSelf.CreateUnchecked(original ?? new string(s));
        return true;
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

    /// <summary>
    /// A reusable JSON converter for <see cref="StringIdentifier{TSelf}"/> derived types.
    /// </summary>
    public sealed class StringIdentifierJsonConverter : JsonConverter<TSelf>
    {
        /// <inheritdoc/>
        public override TSelf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (!TryParse(str, null, out var result))
            {
                throw new JsonException($"Invalid {typeof(TSelf).Name}");
            }

            return result;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._value);
        }
    }
}

/// <summary>
/// Interface that provides a factory method for creating instances from a string value.
/// </summary>
/// <typeparam name="TSelf">The concrete type.</typeparam>
public interface IStringIdentifierFactory<TSelf>
    where TSelf : StringIdentifier<TSelf>, IStringIdentifierFactory<TSelf>
{
    /// <summary>
    /// Creates a new instance of <typeparamref name="TSelf"/> from the given string.
    /// </summary>
    static abstract TSelf CreateUnchecked(string value);
}
