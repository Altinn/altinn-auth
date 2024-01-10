#nullable enable

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// A paginated <see cref="ListObject{T}"/>.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Links">Pagination links.</param>
/// <param name="Items">The items.</param>
public record Paginated<T>(
    PaginatedLinks Links,
    IReadOnlyList<T> Items)
    : ListObject<T>(Items);

/// <summary>
/// Pagination links.
/// </summary>
/// <param name="Next">Link to the next page of items (if any).</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record PaginatedLinks(
    string? Next)
{
    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Required.Add("next");

            var nextSchema = schema.Properties["next"];
            nextSchema.Nullable = true;
            nextSchema.Format = "uri-reference";
            nextSchema.Example = new OpenApiString("/foo/bar/bat?page=2");
        }
    }
}
