#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.ResourceRegistry.JsonPatch;

/// <summary>
/// Represents a RFC 6902 JSON Patch operation.
/// </summary>
/// <remarks>
/// This type has value semantics.
/// </remarks>
[JsonConverter(typeof(JsonConverter))]
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public sealed partial record class JsonPatchOperation
    : IEquatable<JsonPatchOperation>
{
    /// <summary>
    /// Gets the operation type.
    /// </summary>
    public JsonPatchOperationType Type { get; }

    /// <summary>
    /// Gets the source path.
    /// </summary>
    /// <remarks>
    /// Source will be <see langword="null"/> if <see cref="Type"/>
    /// is not one of <see cref="JsonPatchOperationType.Move"/> or 
    /// <see cref="JsonPatchOperationType.Copy"/>.
    /// </remarks>
    public JsonPointer? Source { get; }

    /// <summary>
    /// Gets the target path.
    /// </summary>
    public JsonPointer Target { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <remarks>
    /// Value will be <see langword="null"/> if <see cref="Type"/>
    /// is not one of <see cref="JsonPatchOperationType.Add"/>,
    /// <see cref="JsonPatchOperationType.Replace"/>, or
    /// <see cref="JsonPatchOperationType.Test"/>.
    /// optional.
    /// </remarks>
    public JsonElement? Value { get; }

    private JsonPatchOperation(JsonPatchOperationType type, JsonPointer? source, JsonPointer target, JsonElement? value)
    {
        Type = type;
        Source = source;
        Target = target;
        Value = value.Clone();
    }

    /// <summary>
    /// Creates an `add` operation.
    /// </summary>
    /// <param name="target">The source path.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>An `add` operation.</returns>
    public static JsonPatchOperation Add(JsonPointer target, JsonElement value)
    {
        return new(JsonPatchOperationType.Add, null, target, value);
    }

    /// <summary>
    /// Creates an `remove` operation.
    /// </summary>
    /// <param name="target">The source path.</param>
    /// <returns>An `remove` operation.</returns>
    public static JsonPatchOperation Remove(JsonPointer target)
    {
        return new(JsonPatchOperationType.Remove, null, target, null);
    }

    /// <summary>
    /// Creates an `replace` operation.
    /// </summary>
    /// <param name="target">The source path.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>An `replace` operation.</returns>
    public static JsonPatchOperation Replace(JsonPointer target, JsonElement value)
    {
        return new(JsonPatchOperationType.Replace, null, target, value);
    }

    /// <summary>
    /// Creates an `move` operation.
    /// </summary>
    /// <param name="source">The path to the value to move.</param>
    /// <param name="target">The target path.</param>
    /// <returns>An `move` operation.</returns>
    public static JsonPatchOperation Move(JsonPointer source, JsonPointer target)
    {
        return new(JsonPatchOperationType.Move, source, target, null);
    }

    /// <summary>
    /// Creates an `copy` operation.
    /// </summary>
    /// <param name="source">The path to the value to copy.</param>
    /// <param name="target">The target path.</param>
    /// <returns>An `copy` operation.</returns>
    public static JsonPatchOperation Copy(JsonPointer source, JsonPointer target)
    {
        return new(JsonPatchOperationType.Copy, source, target, null);
    }

    /// <summary>
    /// Creates an `test` operation.
    /// </summary>
    /// <param name="target">The source path.</param>
    /// <param name="value">The value to match.</param>
    /// <returns>An `test` operation.</returns>
    public static JsonPatchOperation Test(JsonPointer target, JsonElement value)
    {
        return new(JsonPatchOperationType.Test, null, target, value);
    }

    /// <inheritdoc/>
    public bool Equals(JsonPatchOperation? other)
    {
        if (other is null)
        {
            return false;
        }

        return Type == other.Type &&
            Source == other.Source &&
            Target == other.Target &&
            Value.IsEquivalentTo(other.Value);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Source, Target, Value.GetStableHashCode());
    }
}
