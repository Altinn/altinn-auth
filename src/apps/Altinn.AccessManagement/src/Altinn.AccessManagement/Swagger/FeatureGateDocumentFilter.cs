using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.AccessManagement.Swagger;

/// <summary>
/// Removes endpoints from the Swagger document when their FeatureGate is not satisfied.
/// </summary>
public class FeatureGateDocumentFilter(IFeatureManager featureManager) : IDocumentFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var apiDescription in context.ApiDescriptions)
        {
            var featureGateAttr = apiDescription.ActionDescriptor.EndpointMetadata
                .OfType<FeatureGateAttribute>()
                .FirstOrDefault();

            if (featureGateAttr is null)
            {
                continue;
            }

            var features = featureGateAttr.Features;
            var requirementType = featureGateAttr.RequirementType;

            bool enabled = requirementType == RequirementType.All
                ? features.All(f => featureManager.IsEnabledAsync(f).GetAwaiter().GetResult())
                : features.Any(f => featureManager.IsEnabledAsync(f).GetAwaiter().GetResult());

            if (!enabled)
            {
                var route = "/" + apiDescription.RelativePath?.TrimEnd('/');
                var method = apiDescription.HttpMethod?.ToLowerInvariant();

                if (swaggerDoc.Paths.TryGetValue(route, out var pathItem))
                {
                    var operationType = method switch
                    {
                        "get" => OperationType.Get,
                        "post" => OperationType.Post,
                        "put" => OperationType.Put,
                        "delete" => OperationType.Delete,
                        "patch" => OperationType.Patch,
                        _ => (OperationType?)null
                    };

                    if (operationType.HasValue)
                    {
                        pathItem.Operations.Remove(operationType.Value);
                    }

                    if (pathItem.Operations.Count == 0)
                    {
                        swaggerDoc.Paths.Remove(route);
                    }
                }
            }
        }
    }
}
