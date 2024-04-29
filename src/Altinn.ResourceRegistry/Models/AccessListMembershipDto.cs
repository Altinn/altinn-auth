#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.Urn;
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
    PartyReference.PartyUuid Id,
    DateTimeOffset Since,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    KeyValueUrnDictionary<PartyReference, PartyReference.Type> Identifiers)
{
    /// <summary>
    /// Creates a new <see cref="AccessListMembershipDto"/> from an <see cref="EnrichedAccessListMembership"/>.
    /// </summary>
    /// <param name="membership">The membership.</param>
    /// <returns>The mapped <see cref="AccessListMembershipDto"/>.</returns>
    public static AccessListMembershipDto From(EnrichedAccessListMembership membership)
    {
        var id = PartyReference.PartyUuid.Create(membership.PartyUuid);
        var identifiers = new KeyValueUrnDictionary<PartyReference, PartyReference.Type>();
        identifiers.Add(id);
        identifiers.Add(PartyReference.PartyId.Create(membership.PartyIdentifiers.PartyId));
        
        if (membership.PartyIdentifiers.OrgNumber is { } orgNo)
        {
            var orgNumber = PartyReference.OrganizationIdentifier.Create(OrganizationNumber.Parse(orgNo));
            identifiers.Add(orgNumber);
        }

        return new(id, membership.Since, identifiers);
    }

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
