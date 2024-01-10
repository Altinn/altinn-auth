#nullable enable

using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents an access list membership.
/// </summary>
/// <param name="Id">The party id.</param>
/// <param name="Since">Since when the party has been a member of the registry.</param>
/// <param name="Identifiers">An optional set of identifiers.</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record AccessListMembershipDto(
    string Id,
    DateTimeOffset Since,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    PartyIdentifiers Identifiers)
{
    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Required.Clear();
            schema.Required.Add("id");
            schema.Required.Add("since");

            var idSchema = schema.Properties["id"];
            idSchema.Nullable = false;
            idSchema.Type = "string";
            idSchema.Format = "urn";
            idSchema.Example = new OpenApiString("urn:altinn:party:e458014d-4d4f-49a1-96d5-a869d95e8715");

            var identifiersSchema = schema.Properties["identifiers"];
            identifiersSchema.Nullable = true;
        }
    }
}

/// <summary>
/// Additional identifiers for a party.
/// </summary>
/// <param name="PartyId">The party id.</param>
/// <param name="OrganizationNumber">The organization number.</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record PartyIdentifiers(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    Guid PartyId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    string? OrganizationNumber)
{
    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var orgNrSchema = schema.Properties["organizationNumber"];
            orgNrSchema.Type = "string";
            orgNrSchema.Format = "org.nr";
            orgNrSchema.Example = new OpenApiString("123456789");
        }
    }
}
