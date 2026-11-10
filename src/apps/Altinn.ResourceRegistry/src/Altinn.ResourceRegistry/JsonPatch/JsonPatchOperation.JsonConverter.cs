#nullable enable

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Models;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.JsonPatch;

public sealed partial record class JsonPatchOperation
{
    private sealed class JsonConverter : JsonConverter<JsonPatchOperation>
    {
        private static class Props
        {
            public static readonly JsonEncodedText Op = JsonEncodedText.Encode("op");
            public static readonly JsonEncodedText From = JsonEncodedText.Encode("from");
            public static readonly JsonEncodedText Path = JsonEncodedText.Encode("path");
            public static readonly JsonEncodedText Value = JsonEncodedText.Encode("value");
        }

        /// <inheritdoc/>
        public override JsonPatchOperation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected a JSON Patch operation object, but found a '{reader.TokenType}' token.");
            }

            Optional<JsonPatchOperationType> type = default;
            Optional<JsonPointer?> source = default;
            Optional<JsonPointer?> target = default;
            Optional<JsonElement> value = default;

            if (!reader.Read())
            {
                throw new JsonException("Expected a JSON patch operation object, but could not read properties.");
            }

            Span<byte> utf8Buffer = stackalloc byte[5];
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propNameLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
                if (propNameLength > 5)
                {
                    throw new JsonException($"Expected a JSON patch operation object, but found key '{reader.GetString()}'.");
                }

                var length = reader.CopyString(utf8Buffer);
                var utf8Value = utf8Buffer[0..length];

                var propNameReader = reader;
                if (!reader.Read())
                {
                    throw new JsonException("Expected a JSON patch operation object.");
                }

                if (utf8Value.SequenceEqual(Props.Op.EncodedUtf8Bytes))
                {
                    if (type.HasValue)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but 'op' is set more than once.");
                    }

                    type = JsonSerializer.Deserialize<JsonPatchOperationType>(ref reader, options);
                    if (!reader.Read())
                    {
                        throw new JsonException("Expected a JSON patch operation object.");
                    }
                }
                else if (utf8Value.SequenceEqual(Props.Path.EncodedUtf8Bytes))
                {
                    if (target.HasValue)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but 'path' is set more than once.");
                    }

                    target = JsonSerializer.Deserialize<JsonPointer>(ref reader, options);

                    if (!reader.Read())
                    {
                        throw new JsonException("Expected a JSON patch operation object.");
                    }
                }
                else if (utf8Value.SequenceEqual(Props.From.EncodedUtf8Bytes))
                {
                    if (source.HasValue)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but 'from' is set more than once.");
                    }

                    source = JsonSerializer.Deserialize<JsonPointer>(ref reader, options);
                    if (!reader.Read())
                    {
                        throw new JsonException("Expected a JSON patch operation object.");
                    }
                }
                else if (utf8Value.SequenceEqual(Props.Value.EncodedUtf8Bytes))
                {
                    if (value.HasValue)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but 'value' is set more than once.");
                    }

                    value = JsonElement.ParseValue(ref reader);
                    if (!reader.Read())
                    {
                        throw new JsonException("Expected a JSON patch operation object.");
                    }
                }
                else
                {
                    throw new JsonException($"Expected a JSON patch operation object, but found key '{propNameReader.GetString()}'.");
                }
            }

            Debug.Assert(reader.TokenType == JsonTokenType.EndObject);
            if (!type.HasValue)
            {
                throw new JsonException("Expected a JSON patch operation object, but no 'op' set.");
            }

            if (!target.HasValue || target.Value is null)
            {
                throw new JsonException("Expected a JSON patch operation object, but no 'path' set.");
            }

            switch (type.Value)
            {
                case JsonPatchOperationType.Add:
                    if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Undefined)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but no 'value' set for 'add' operation.");
                    }

                    return Add(target.Value, value.Value);

                case JsonPatchOperationType.Remove:
                    return Remove(target.Value);

                case JsonPatchOperationType.Replace:
                    if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Undefined)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but no 'value' set for 'replace' operation.");
                    }

                    return Replace(target.Value, value.Value);

                case JsonPatchOperationType.Move:
                    if (!source.HasValue || source.Value is null)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but no 'from' set for 'move' operation.");
                    }

                    return Move(source.Value, target.Value);

                case JsonPatchOperationType.Copy:
                    if (!source.HasValue || source.Value is null)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but no 'from' set for 'copy' operation.");
                    }

                    return Copy(source.Value, target.Value);

                case JsonPatchOperationType.Test:
                    if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Undefined)
                    {
                        throw new JsonException("Expected a JSON patch operation object, but no 'value' set for 'test' operation.");
                    }

                    return Test(target.Value, value.Value);

                case JsonPatchOperationType.Unknown:
                default:
                    throw new JsonException("Invalid JSON patch operation object.");
            }
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, JsonPatchOperation value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(Props.Op);
            JsonSerializer.Serialize(writer, value.Type, options);

            writer.WritePropertyName(Props.Path);
            JsonSerializer.Serialize(writer, value.Target, options);

            switch (value.Type)
            {
                case JsonPatchOperationType.Add:
                case JsonPatchOperationType.Replace:
                case JsonPatchOperationType.Test:
                    writer.WritePropertyName(Props.Value);
                    value.Value!.Value.WriteTo(writer);
                    break;

                case JsonPatchOperationType.Move:
                case JsonPatchOperationType.Copy:
                    writer.WritePropertyName(Props.From);
                    JsonSerializer.Serialize(writer, value.Source, options);
                    break;

                case JsonPatchOperationType.Remove:
                    break;

                case JsonPatchOperationType.Unknown:
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value.Type));
                    break;
            }

            writer.WriteEndObject();
        }
    }
}
