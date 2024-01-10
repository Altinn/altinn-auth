#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// A unique reference to a party in the form of an URN.
/// </summary>
/// <param name="Type">The reference type.</param>
/// <param name="Value">The reference value (party-id or org.no).</param>
[JsonConverter(typeof(Converter))]
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record PartyReference(
    PartyReferenceType Type,
    string Value)
{
    private sealed class Converter : JsonConverter<PartyReference>
    {
        public override PartyReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, PartyReference value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Properties.Clear();
            schema.AdditionalProperties = null;
            schema.Type = "string";
            schema.Format = "urn";
            schema.Example = new OpenApiString("urn:altinn:party:e458014d-4d4f-49a1-96d5-a869d95e8715");
        }
    }
}

/// <summary>
/// Supported party reference types.
/// </summary>
public enum PartyReferenceType
{
    PartyId,
    OrganizationNumber,
}
