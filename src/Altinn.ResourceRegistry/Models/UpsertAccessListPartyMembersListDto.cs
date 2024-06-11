#nullable enable

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Register;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Object sent to the API to add/remove/overwrite members from an access list.
/// </summary>
/// <param name="items">The members to add/remove/overwrite</param>
[JsonConverter(typeof(Converter))]
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public class UpsertAccessListPartyMembersListDto(
    IReadOnlyList<PartyUrn> items)
    : IReadOnlyList<PartyUrn>
{
    private readonly IReadOnlyList<PartyUrn> _items = items;

    /// <inheritdoc/>
    public PartyUrn this[int index] => _items[index];

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public IEnumerator<PartyUrn> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }

    private sealed class Converter : JsonConverter<UpsertAccessListPartyMembersListDto>
    {
        /// <inheritdoc/>
        public override UpsertAccessListPartyMembersListDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected object");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("data"u8))
            {
                throw new JsonException("Expected property 'data'");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected array");
            }

            var items = JsonSerializer.Deserialize<List<PartyUrn>>(ref reader, options);
            if (items is null)
            {
                throw new JsonException("Expected array");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("Expected end of object");
            }

            return new(items);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, UpsertAccessListPartyMembersListDto value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("data"u8);
            JsonSerializer.Serialize(writer, value._items, options);
            writer.WriteEndObject();
        }
    }

    private sealed class SchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Type = "object";
            schema.Properties.Clear();
            schema.Properties.Add("data", context.SchemaGenerator.GenerateSchema(typeof(List<PartyUrn>), context.SchemaRepository));
            schema.Required.Clear();
            schema.Required.Add("data");
        }
    }
}
