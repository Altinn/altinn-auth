#nullable enable

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    IReadOnlyList<PartyReference> items)
    : IReadOnlyList<PartyReference>
{
    private readonly IReadOnlyList<PartyReference> _items = items;

    /// <inheritdoc/>
    public PartyReference this[int index] => _items[index];

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public IEnumerator<PartyReference> GetEnumerator()
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, UpsertAccessListPartyMembersListDto value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class SchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Type = "object";
            schema.Properties.Clear();
            schema.Properties.Add("data", context.SchemaGenerator.GenerateSchema(typeof(List<PartyReference>), context.SchemaRepository));
            schema.Required.Clear();
            schema.Required.Add("data");
        }
    }
}
