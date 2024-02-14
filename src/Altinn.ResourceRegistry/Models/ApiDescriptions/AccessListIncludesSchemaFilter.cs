#nullable enable

using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models.ApiDescriptions;

/// <summary>
/// Schema filter for <see cref="AccessListIncludes"/>.
/// </summary>
public sealed class AccessListIncludesSchemaFilter
    : SchemaFilter<AccessListIncludes>
{
    /// <inheritdoc/>
    protected override void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Enum = null;
        schema.Format = null;
        schema.Type = "array";
        schema.Items = new OpenApiSchema
        {
            Type = "string",
            Enum = AccessListIncludesModelBinder.AllowedValues.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList(),
        };
    }
}
