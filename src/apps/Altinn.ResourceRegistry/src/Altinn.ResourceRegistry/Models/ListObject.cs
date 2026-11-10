#nullable enable

using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// A list object is a wrapper around a list of items to allow for the API to be
/// extended in the future without breaking backwards compatibility.
/// </summary>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public abstract record ListObject
{
    /// <summary>
    /// Creates a new <see cref="ListObject{T}"/> from a list of items.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    /// <param name="items">The list of items.</param>
    /// <returns>A <see cref="ListObject{T}"/>.</returns>
    public static ListObject<T> Create<T>(IEnumerable<T> items)
        => new(items);

    /// <summary>
    /// Default schema filter for <see cref="ListObject"/>.
    /// </summary>
    protected class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is not OpenApiSchema openApiSchema || openApiSchema.Properties is null)
            {
                return;
            }

            openApiSchema.Required ??= new HashSet<string>();
            foreach (var prop in openApiSchema.Properties)
            {
                openApiSchema.Required.Add(prop.Key);
            }

            if (openApiSchema.Properties.TryGetValue("data", out var dataSchema) && dataSchema is OpenApiSchema dataOpenApiSchema)
            {
                dataOpenApiSchema.Type &= ~JsonSchemaType.Null;
            }
        }
    }
}

/// <summary>
/// A concrete list object.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items.</param>
public record ListObject<T>(
    [property: JsonPropertyName("data")]
    IEnumerable<T> Items)
    : ListObject;
