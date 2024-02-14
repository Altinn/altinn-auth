#nullable enable

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.JsonPatch;

/// <summary>
/// JSON Patch operation types.
/// </summary>
[JsonConverter(typeof(JsonPatchOperationTypeConverter))]
public enum JsonPatchOperationType
{
    /// <summary>
    /// Default value. Not valid.
    /// </summary>
    Unknown = default,

    /// <summary>
    /// Represents the `add` operation.
    /// </summary>
    [Description("add")]
    Add,

    /// <summary>
    /// Represents the `remove` operation.
    /// </summary>
    [Description("remove")]
    Remove,

    /// <summary>
    /// Represents the `replace` operation.
    /// </summary>
    [Description("replace")]
    Replace,

    /// <summary>
    /// Represents the `move` operation.
    /// </summary>
    [Description("move")]
    Move,

    /// <summary>
    /// Represents the `copy` operation.
    /// </summary>
    [Description("copy")]
    Copy,

    /// <summary>
    /// Represents the `test` operation.
    /// </summary>
    [Description("test")]
    Test
}

/// <summary>
/// JSON converter for <see cref="JsonPatchOperationType"/>.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2223")]
internal class JsonPatchOperationTypeConverter : JsonConverter<JsonPatchOperationType>
{
    private static readonly JsonEncodedText Add = JsonEncodedText.Encode("add");
    private static readonly JsonEncodedText Remove = JsonEncodedText.Encode("remove");
    private static readonly JsonEncodedText Replace = JsonEncodedText.Encode("replace");
    private static readonly JsonEncodedText Move = JsonEncodedText.Encode("move");
    private static readonly JsonEncodedText Copy = JsonEncodedText.Encode("copy");
    private static readonly JsonEncodedText Test = JsonEncodedText.Encode("test");

    /// <inheritdoc/>
    public override JsonPatchOperationType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return JsonPatchOperationType.Unknown;
        }

        var rawLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
        if (rawLength > 7)
        {
            throw new JsonException("Invalid JSON Patch operation type.");
        }

        // longest operation name, "replace", is 7 characters
        Span<byte> utf8Buffer = stackalloc byte[7];

        var length = reader.CopyString(utf8Buffer);
        if (length > utf8Buffer.Length)
        {
            throw new JsonException("Invalid JSON Patch operation type.");
        }

        var utf8Value = utf8Buffer[0..length];
        switch (length)
        {
            case 3:
                if (utf8Value.SequenceEqual(Add.EncodedUtf8Bytes))
                {
                    return JsonPatchOperationType.Add;
                }

                goto default;

            case 4:
                if (utf8Value.SequenceEqual(Move.EncodedUtf8Bytes))
                {
                    return JsonPatchOperationType.Move;
                }

                if (utf8Value.SequenceEqual(Copy.EncodedUtf8Bytes))
                {
                    return JsonPatchOperationType.Copy;
                }

                if (utf8Value.SequenceEqual(Test.EncodedUtf8Bytes))
                {
                    return JsonPatchOperationType.Test;
                }

                goto default;

            case 6:
                if (utf8Value.SequenceEqual(Remove.EncodedUtf8Bytes))
                {
                    return JsonPatchOperationType.Remove;
                }

                goto default;

            case 7:
                if (utf8Value.SequenceEqual(Replace.EncodedUtf8Bytes))
                {
                    return JsonPatchOperationType.Replace;
                }

                goto default;

            default:
                throw new JsonException("Invalid JSON Patch operation type.");
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, JsonPatchOperationType value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case JsonPatchOperationType.Add: writer.WriteStringValue(Add); break;
            case JsonPatchOperationType.Remove: writer.WriteStringValue(Remove); break;
            case JsonPatchOperationType.Replace: writer.WriteStringValue(Replace); break;
            case JsonPatchOperationType.Move: writer.WriteStringValue(Move); break;
            case JsonPatchOperationType.Copy: writer.WriteStringValue(Copy); break;
            case JsonPatchOperationType.Test: writer.WriteStringValue(Test); break;
            default: writer.WriteNullValue(); break;
        }
    }
}
