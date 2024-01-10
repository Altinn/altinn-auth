#nullable enable

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.JsonPatch;

/// <summary>
/// Represents a JSON Pointer as defined in RFC 6901.
/// </summary>
/// <remarks>
/// This type has value semantics.
/// </remarks>
[JsonConverter(typeof(JsonConverter))]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public sealed class JsonPointer
    : IEquatable<JsonPointer>
    , IReadOnlyList<JsonPointer.Segment>
{
    /// <summary>
    /// An empty JSON Pointer.
    /// </summary>
    public static JsonPointer Empty { get; } = new(string.Empty, []);

    private readonly string _raw;

    /// <inheritdoc/>
    public Segment this[int index] => ((IReadOnlyList<Segment>)Segments)[index];

    /// <inheritdoc/>
    public int Count => ((IReadOnlyCollection<Segment>)Segments).Count;

    /// <summary>
    /// Gets the pointer segments as an <see cref="ImmutableArray{T}"/>.
    /// </summary>
    public ImmutableArray<Segment> Segments { get; }

    private JsonPointer(string raw, ImmutableArray<Segment> segments)
    {
        _raw = raw;
        Segments = segments;
    }

    /// <summary>
    /// Parses a JSON Pointer from a string value.
    /// </summary>
    /// <param name="value">The string value</param>
    /// <returns>The parsed <see cref="JsonPointer"/>.</returns>
    public static JsonPointer Parse(string value)
    {
        if (!TryParse(value, out var result))
        {
            ThrowHelper.ThrowArgumentException(nameof(value), "Invalid JSON Pointer");
        }

        return result;
    }
    
    /// <summary>
    /// Tries to parse a JSON Pointer from a string value.
    /// </summary>
    /// <param name="value">The string value</param>
    /// <param name="parsed">The parsed <see cref="JsonPointer"/>, or <see langword="null"/> if parsing failed</param>
    /// <returns><see langword="true"/> if parsing succeeded, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string value, [NotNullWhen(true)] out JsonPointer? parsed)
    {
        // short-circuit for empty strings
        if (value.Length == 0)
        {
            parsed = JsonPointer.Empty;
            return true;
        }

        // The ABNF syntax of a JSON Pointer is:
        //
        //    json - pointer = *("/" reference - token )
        //    reference - token = *(unescaped / escaped)
        //    unescaped = % x00 - 2E / % x30 - 7D / % x7F - 10FFFF
        //       ; % x2F('/') and % x7E('~') are excluded from 'unescaped'
        //    escaped = "~"("0" / "1")
        //       ; representing '~' and '/', respectively
        var segments = ImmutableArray.CreateBuilder<Segment>(value.AsSpan().Count('/'));
        var remaining = value.AsMemory();
        
        while (remaining.Length > 0)
        {
            if (remaining.Span[0] != '/')
            {
                parsed = null;
                return false;
            }

            remaining = remaining[1..];

            if (!TryParseSegment(ref remaining, out var segment))
            {
                parsed = null;
                return false;
            }

            segments.Add(segment);
        }

        parsed = new JsonPointer(value, segments.ToImmutable());
        return true;

        static bool TryParseSegment(ref ReadOnlyMemory<char> remaining, out Segment result)
        {
            var segment = remaining.Span;
            var index = segment.IndexOf('/');

            // last segment
            if (index == -1)
            {
                index = segment.Length;
            }

            segment = segment[..index];
            var value = remaining[..index];
            remaining = remaining[index..];

            var escapedCount = segment.Count('~');
            if (escapedCount > 0)
            {
                // each escaped character is 2 characters before unescaping
                var newLength = segment.Length - escapedCount;

                // validate that all escaped characters are followed by a valid escape
                var remainingEscaped = segment;
                while ((index = remainingEscaped.IndexOf('~')) != -1)
                {
                    // last character is an escape character
                    if (index == remainingEscaped.Length - 1)
                    {
                        result = default;
                        return false;
                    }

                    switch (remainingEscaped[index + 1])
                    {
                        case '0':
                        case '1':
                            break;

                        default:
                            result = default;
                            return false;
                    }

                    remainingEscaped = remainingEscaped[(index + 2)..];
                }

                var unescaped = string.Create(newLength, value, (result, escapedMemory) =>
                {
                    var escaped = escapedMemory.Span;

                    int nextEscaped;
                    while ((nextEscaped = escaped.IndexOf('~')) != -1)
                    {
                        escaped[0..nextEscaped].CopyTo(result);
                        result[nextEscaped] = escaped[nextEscaped + 1] switch
                        {
                            '0' => '~',
                            '1' => '/',

                            // should be unreachable
                            _ => throw new InvalidOperationException("Invalid escaped character in JSON Pointer")
                        };

                        escaped = escaped[(nextEscaped + 2)..];
                        result = result[(nextEscaped + 1)..];
                    }

                    Debug.Assert(escaped.Length == result.Length, $"expected both spans to be same length, but was: escaped={escaped.Length} result={result.Length}");
                    escaped.CopyTo(result);
                });
                
                value = unescaped.AsMemory();
            }

            result = new Segment(value);
            return true;
        }
    }
    
    /// <inheritdoc/>
    public override string ToString()
        => _raw;

    private string DebuggerDisplay
        => $"\"{ToString()}\"";

    /// <inheritdoc/>
    public bool Equals(JsonPointer? other)
        => other is not null && _raw == other._raw;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is JsonPointer other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => _raw.GetHashCode();

    public static bool operator ==(JsonPointer? left, JsonPointer? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(JsonPointer? left, JsonPointer? right)
        => !(left == right);

    /// <summary>
    /// Gets an enumerator for the segments.
    /// </summary>
    /// <returns>An enumerator.</returns>
    public ImmutableArray<Segment>.Enumerator GetEnumerator()
    {
        return Segments.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator<Segment> IEnumerable<Segment>.GetEnumerator()
    {
        return ((IEnumerable<Segment>)Segments).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Segments).GetEnumerator();
    }

    /// <summary>
    /// A segment of a JSON Pointer.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct Segment
        : IEquatable<Segment>
        , IEquatable<string>
    {
        private readonly ReadOnlyMemory<char> _value;

        /// <summary>
        /// Constructs a new <see cref="Segment"/>.
        /// </summary>
        /// <param name="value">The segment value</param>
        internal Segment(ReadOnlyMemory<char> value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets weather the segment is empty.
        /// </summary>
        public bool IsEmpty => _value.IsEmpty;

        /// <inheritdoc/>
        public override string ToString()
            => new string(_value.Span);

        /// <inheritdoc/>
        public bool Equals(Segment other)
            => _value.Span.SequenceEqual(other._value.Span);

        /// <inheritdoc/>
        public bool Equals(string? other)
            => other is not null && _value.Span.SequenceEqual(other.AsSpan());

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj switch 
            { 
                Segment segment => Equals(segment),
                string str => Equals(str),
                _ => false
            };

        /// <inheritdoc/>
        public override int GetHashCode()
            => _value.GetHashCode();

        private string DebuggerDisplay
            => $"\"{ToString()}\"";

        public static bool operator ==(Segment left, Segment right)
            => left.Equals(right);

        public static bool operator !=(Segment left, Segment right)
            => !(left == right);
    }

    private sealed class JsonConverter : JsonConverter<JsonPointer?>
    {
        public override JsonPointer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringRepr = reader.GetString();

            if (stringRepr is null)
            {
                return null;
            }

            if (!TryParse(stringRepr, out var pointer))
            {
                throw new JsonException("Invalid JSON Pointer");
            }

            return pointer;
        }

        public override void Write(Utf8JsonWriter writer, JsonPointer? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Type = "string";
            schema.Format = "json-pointer";
            schema.Items = null;
            schema.Example = new OpenApiString("/foo/bar");
        }
    }
}
