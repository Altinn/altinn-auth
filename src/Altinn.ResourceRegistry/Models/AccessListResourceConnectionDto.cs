#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Utils;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model for creating an access list resource connection.
/// </summary>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="ActionFilters">The allowed actions.</param>
/// <param name="CreatedAt">When the connection was created.</param>
/// <param name="UpdatedAt">When the connection was last updated.</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record AccessListResourceConnectionDto(
    string ResourceIdentifier,
    IReadOnlyCollection<string>? ActionFilters,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
    : IConvertibleFrom<AccessListResourceConnectionDto, AccessListResourceConnection>
{
    /// <summary>
    /// Gets the allowed actions or <see langword="null"/> if all actions are allowed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? ActionFilters { get; } 
        = ActionFilters is null or { Count: 0 } ? null : ActionFilters;

    /// <inheritdoc/>
    public static AccessListResourceConnectionDto From(AccessListResourceConnection connection)
        => new(
            connection.ResourceIdentifier,
            connection.Actions,
            connection.Created,
            connection.Modified);

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Required.UnionWith(["resourceIdentifier", "createdAt", "updatedAt"]);
        }
    }
}
