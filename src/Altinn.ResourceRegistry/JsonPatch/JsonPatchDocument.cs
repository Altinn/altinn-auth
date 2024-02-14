#nullable enable

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.JsonPatch;

/// <summary>
/// A RFC 6902 JSON Patch document.
/// </summary>
/// <remarks>
/// This type has value semantics.
/// </remarks>
[JsonConverter(typeof(JsonConverter))]
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public sealed record class JsonPatchDocument
    : IEquatable<JsonPatchDocument>
{ 
    /// <summary>
    /// Gets the JSON Patch operations.
    /// </summary>
    public ImmutableArray<JsonPatchOperation> Operations { get; }

    /// <summary>
    /// Create a new <see cref="JsonPatchDocument"/> from an <see cref="ImmutableArray{T}"/> of <see cref="JsonPatchOperation"/>.
    /// </summary>
    /// <param name="operations">The operations</param>
    public JsonPatchDocument(ImmutableArray<JsonPatchOperation> operations)
    {
        Operations = operations;
    }

    /// <summary>
    /// Create a new <see cref="JsonPatchDocument"/> from an <see cref="IEnumerable{T}"/> of <see cref="JsonPatchOperation"/>.
    /// </summary>
    /// <param name="operations">The operations</param>
    public JsonPatchDocument(IEnumerable<JsonPatchOperation> operations)
        : this(operations.ToImmutableArray())
    {
    }

    /// <inheritdoc/>
    public bool Equals(JsonPatchDocument? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Operations.SequenceEqual(other.Operations);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        foreach (var operation in Operations)
        {
            hashCode.Add(operation);
        }

        return hashCode.ToHashCode();
    }

    private sealed class JsonConverter : JsonConverter<JsonPatchDocument>
    {
        /// <inheritdoc/>
        public override JsonPatchDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var operations = JsonSerializer.Deserialize<ImmutableArray<JsonPatchOperation>>(ref reader, options);

            return new JsonPatchDocument(operations);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, JsonPatchDocument value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Operations, options);
        }
    }

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var operationsSchema = schema.Properties["operations"];

            schema.Properties.Clear();
            schema.Type = "array";
            schema.AdditionalPropertiesAllowed = true;
            schema.Items = operationsSchema.Items;
        }
    }
}
