#nullable enable

using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
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
    /// Default schema filter for <see cref="ListObject"/>.
    /// </summary>
    protected class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            foreach (var prop in schema.Properties)
            {
                schema.Required.Add(prop.Key);
            }

            schema.Properties["data"].Nullable = false;
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
    IReadOnlyList<T> Items)
    : ListObject;
