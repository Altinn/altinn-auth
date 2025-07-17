#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Utils;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Required.UnionWith(["identifier", "name", "description", "createdAt", "updatedAt"]);

            schema.Properties["identifier"].Nullable = false;
            schema.Properties["identifier"].Format = "slug";
            schema.Properties["identifier"].Example = new OpenApiString("godkjente-banker");
            schema.Properties["name"].Nullable = false;
            schema.Properties["name"].Example = new OpenApiString("Godkjente banker");
            schema.Properties["description"].Nullable = false;
            schema.Properties["description"].Example = new OpenApiString("En liste over godkjente banker");
        }
    }
}
