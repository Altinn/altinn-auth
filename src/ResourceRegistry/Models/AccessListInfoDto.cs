#nullable enable

using Altinn.ResourceRegistry.Core.AccessLists;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents public access list metadata.
/// </summary>
/// <param name="Identifier">The access list identifier</param>
/// <param name="Name">The access list name</param>
/// <param name="Description">The access list description</param>
/// <param name="CreatedAt">When the access list was created</param>
/// <param name="UpdatedAt">When the access list was updated</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record AccessListInfoDto(
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Create a <see cref="AccessListInfoDto"/> from a <see cref="AccessListInfo"/>.
    /// </summary>
    /// <param name="info">The <see cref="AccessListInfo"/></param>
    /// <returns>A <see cref="AccessListInfoDto"/> matching the <see cref="AccessListInfo"/></returns>
    public static AccessListInfoDto From(AccessListInfo info)
        => new(
            info.Identifier,
            info.Name,
            info.Description,
            info.CreatedAt,
            info.UpdatedAt);

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            foreach (var prop in schema.Properties)
            {
                schema.Required.Add(prop.Key);
            }

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
