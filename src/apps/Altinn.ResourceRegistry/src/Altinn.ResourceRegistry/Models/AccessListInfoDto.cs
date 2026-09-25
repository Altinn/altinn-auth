#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Utils;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents public access list metadata.
/// </summary>
/// <param name="Urn">URN of the access list</param>
/// <param name="Identifier">The access list identifier</param>
/// <param name="Name">The access list name</param>
/// <param name="Description">The access list description</param>
/// <param name="CreatedAt">When the access list was created</param>
/// <param name="UpdatedAt">When the access list was updated</param>
/// <param name="ResourceConnections">The resource connections</param>
/// <param name="Version">The aggregate version</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record AccessListInfoDto(
    string Urn,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IEnumerable<AccessListResourceConnectionDto>? ResourceConnections,
    [property: JsonIgnore]
    AggregateVersion Version)
    : ITaggedEntity<AggregateVersion>
    , IConvertibleFrom<AccessListInfoDto, AccessListInfo>
{
    /// <inheritdoc/>
    public static AccessListInfoDto From(AccessListInfo info)
        => new(
            $"urn:altinn:access-list:{info.ResourceOwner}:{info.Identifier}",
            info.Identifier,
            info.Name,
            info.Description,
            info.CreatedAt,
            info.UpdatedAt,
            info.ResourceConnections?.Select(AccessListResourceConnectionDto.From),
            new(info.Version));

    /// <inheritdoc/>
    void ITaggedEntity<AggregateVersion>.GetHeaderValues(out AggregateVersion version, out HttpDateTimeHeaderValue modifiedAt)
    {
        version = Version;
        modifiedAt = new(UpdatedAt);
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

            openApiSchema.Required ??= new HashSet<string>();
            openApiSchema.Required.UnionWith(["identifier", "name", "description", "createdAt", "updatedAt"]);

            var properties = openApiSchema.Properties;
            if (properties is null)
            {
                return;
            }

            if (properties.TryGetValue("identifier", out var identifierSchema) && identifierSchema is OpenApiSchema identifier)
            {
                identifier.Type &= ~JsonSchemaType.Null;
                identifier.Format = "slug";
                identifier.Example = "godkjente-banker";
            }

            if (properties.TryGetValue("name", out var nameSchema) && nameSchema is OpenApiSchema name)
            {
                name.Type &= ~JsonSchemaType.Null;
                name.Example = "Godkjente banker";
            }

            if (properties.TryGetValue("description", out var descriptionSchema) && descriptionSchema is OpenApiSchema description)
            {
                description.Type &= ~JsonSchemaType.Null;
                description.Example = "En liste over godkjente banker";
            }
        }
    }
}
