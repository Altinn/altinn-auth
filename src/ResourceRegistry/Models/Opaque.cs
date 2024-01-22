#nullable enable

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Altinn.ResourceRegistry.Utils;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// An opaque value is a value that can be transmitted to another party
/// without divulging any type information or expectations about the value.
/// </summary>
public static class Opaque
{
    /// <summary>
    /// Create a new opaque value.
    /// </summary>
    /// <typeparam name="T">The type of the inner value</typeparam>
    /// <param name="value">The inner value</param>
    /// <returns>A new opaque value.</returns>
    public static Opaque<T> Create<T>(T value) 
        where T : notnull 
        => new(value);
}

/// <summary>
/// An opaque value is a value that can be transmitted to another party
/// without divulging any type information or expectations about the value.
/// </summary>
/// <typeparam name="T">The type of the inner value</typeparam>
/// <param name="value">The inner value</param>
[SwaggerSchemaFilter(typeof(OpaqueSchemaFilter))]
public class Opaque<T>(T value)
    : IParsable<Opaque<T>>
    where T : notnull
{
    /// <summary>
    /// Gets the inner value.
    /// </summary>
    public T Value => value;

    /// <inheritdoc/>
    public override string ToString()
        => Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(value));

    /// <inheritdoc/>
    public static Opaque<T> Parse(string s, IFormatProvider? provider)
    {
        if (!Opaque<T>.TryParse(s, provider, out var result))
        {
            throw new FormatException($"Failed to parse opaque {typeof(T).FullName}");
        }

        return result;
    }

    /// <inheritdoc/>
    public static bool TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Opaque<T> result)
    {
        if (s == null)
        {
            result = null;
            return false;
        }

        byte[] buff = null!;
        var binaryLength = Base64UrlEncoder.GetMaxDecodedLength(s.Length);
        try 
        {
            buff = ArrayPool<byte>.Shared.Rent(binaryLength);
            if (!Base64UrlEncoder.TryDecode(s, buff, out var written))
            {
                result = null;
                return false;
            }

            var bytes = buff.AsSpan(0, written);
            var inner = JsonSerializer.Deserialize<T>(bytes);
            if (inner is null)
            {
                result = null;
                return false;
            }

            result = new Opaque<T>(inner);
            return true;
        }
        catch (JsonException)
        {
            result = null;
            return false;
        }
        finally
        {
            if (buff is not null)
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
        }
    }
}

/// <summary>
/// Schema filter for opaque types
/// </summary>
internal class OpaqueSchemaFilter
    : ISchemaFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.ExternalDocs = null;
        schema.Type = "string";
    }
}
