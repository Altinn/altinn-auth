#nullable enable

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models.ApiDescriptions;

/// <summary>
/// Base class for schema filters that affect a specific type.
/// </summary>
/// <typeparam name="T">The type this schema filter affects.</typeparam>
public abstract class SchemaFilter<T>
    : ISchemaFilter
{
    /// <inheritdoc/>
    void ISchemaFilter.Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(T))
        {
            return;
        }

        Apply(schema, context);
    }

    /// <summary>
    /// Applies the schema filter to the given schema.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <param name="context">The context.</param>
    protected abstract void Apply(OpenApiSchema schema, SchemaFilterContext context);
}
