#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.Urn;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents an access list membership.
/// </summary>
/// <param name="Id">The party id.</param>
/// <param name="Since">Since when the party has been a member of the access list.</param>
/// <param name="Identifiers">An optional set of identifiers.</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record AccessListMembershipDto(
    PartyUrn.PartyUuid Id,
    DateTimeOffset Since,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    KeyValueUrnDictionary<PartyUrn, PartyUrn.Type> Identifiers)
{
    /// <summary>
    /// Creates a new <see cref="AccessListMembershipDto"/> from an <see cref="EnrichedAccessListMembership"/>.
    /// </summary>
    /// <param name="membership">The membership.</param>
    /// <returns>The mapped <see cref="AccessListMembershipDto"/>.</returns>
    public static AccessListMembershipDto From(EnrichedAccessListMembership membership)
    {
        var id = PartyUrn.PartyUuid.Create(membership.PartyUuid);
        var identifiers = new KeyValueUrnDictionary<PartyUrn, PartyUrn.Type>();
        identifiers.Add(id);
        identifiers.Add(PartyUrn.PartyId.Create(membership.PartyIdentifiers.PartyId));
        
        if (membership.PartyIdentifiers.OrgNumber is { } orgNo)
        {
            var orgNumber = PartyUrn.OrganizationIdentifier.Create(OrganizationNumber.Parse(orgNo));
            identifiers.Add(orgNumber);
        }

        return new(id, membership.Since, identifiers);
    }

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is not OpenApiSchema openApiSchema)
            {
                return;
            }

            openApiSchema.Required = new HashSet<string> { "id", "since" };

            var properties = openApiSchema.Properties;
            if (properties is null)
            {
                return;
            }

            if (properties.TryGetValue("id", out var idSchemaValue) && idSchemaValue is OpenApiSchema idSchema)
            {
                idSchema.Type = JsonSchemaType.String;
                idSchema.Format = "urn";
                idSchema.Example = "urn:altinn:party:e458014d-4d4f-49a1-96d5-a869d95e8715";
            }

            if (properties.TryGetValue("identifiers", out var identifiersSchemaValue) && identifiersSchemaValue is OpenApiSchema identifiersSchema)
            {
                identifiersSchema.Type |= JsonSchemaType.Null;
            }
        }
    }
}
