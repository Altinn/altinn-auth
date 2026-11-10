#nullable enable

using System.Text.Json.Nodes;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Microsoft.OpenApi;
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
        schema.Type = JsonSchemaType.Array;
        schema.Items = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = AccessListIncludesModelBinder.AllowedValues.Select(v => (JsonNode)v).ToList(),
        };
    }
}
