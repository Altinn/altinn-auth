#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models.ApiDescriptions;

/// <summary>
/// Documents server-controlled timestamps on resource responses.
/// </summary>
public sealed class ServiceResourceSchemaFilter : SchemaFilter<ServiceResource>
{
    /// <inheritdoc/>
    protected override void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach (var name in new[] { "createdAt", "updatedAt" })
        {
            if (schema.Properties?[name] is OpenApiSchema property)
            {
                property.ReadOnly = true;
                property.Description = name == "createdAt"
                    ? "First registration in Resource Registry, in UTC, unchanged across versions. Null for virtual Storage apps. Ignored on POST and PUT."
                    : "When the returned metadata version was saved, in UTC. Includes unchanged PUTs, excludes policy uploads. Null when unknown or for virtual Storage apps. Ignored on POST and PUT.";
            }
        }
    }
}
