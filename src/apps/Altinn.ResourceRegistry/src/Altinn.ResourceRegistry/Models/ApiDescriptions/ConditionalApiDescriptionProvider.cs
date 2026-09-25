#nullable enable

using Altinn.ResourceRegistry.Results;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.ResourceRegistry.Models.ApiDescriptions;

/// <summary>
/// Implements a provider of <see cref="ApiDescription"/> to change responses
/// of type <see cref="ConditionalResult{TValue, TTag}"/> to represent the
/// different response options a <see cref="ConditionalResult{TValue, TTag}"/>
/// has.
/// </summary>
internal sealed class ConditionalApiDescriptionProvider
    : IApiDescriptionProvider
{
    private readonly IModelMetadataProvider _modelMetadataProvider;

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalApiDescriptionProvider"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    public ConditionalApiDescriptionProvider(IModelMetadataProvider modelMetadataProvider)
    {
        _modelMetadataProvider = modelMetadataProvider;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The order -999 ensures that this provider is executed right after the <see cref="DefaultApiDescriptionProvider" />.
    /// </remarks>
    public int Order => -999;

    /// <inheritdoc />
    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }

    /// <inheritdoc />
    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        Guard.IsNotNull(context);

        foreach (var result in context.Results)
        {
            var hasConditional = false;
            foreach (var response in result.SupportedResponseTypes.Where(r => r.Type is not null))
            {
                var conditionalInnerType = ConditionalResult.GetUnderlyingTypes(response.Type!)?.ValueType;
                if (conditionalInnerType is null)
                {
                    continue;
                }

                response.Type = conditionalInnerType;
                response.ModelMetadata = _modelMetadataProvider.GetMetadataForType(conditionalInnerType);
                hasConditional = true;
            }

            if (!hasConditional)
            {
                continue;
            }

            result.SetProperty(ConditionalOperationFilter.Marker.Instance);
            if (!HasResponseCode(result, StatusCodes.Status404NotFound))
            {
                result.SupportedResponseTypes.Add(new ApiResponseType
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Type = typeof(void),
                });
            }

            if (!HasResponseCode(result, StatusCodes.Status412PreconditionFailed))
            {
                result.SupportedResponseTypes.Add(new ApiResponseType
                {
                    StatusCode = StatusCodes.Status412PreconditionFailed,
                    Type = typeof(void),
                });
            }

            if (IsReadMethod(result.HttpMethod) && !HasResponseCode(result, StatusCodes.Status304NotModified))
            {
                result.SupportedResponseTypes.Add(new ApiResponseType
                {
                    StatusCode = StatusCodes.Status304NotModified,
                    Type = typeof(void),
                });
            }
        }
    }

    private static bool HasResponseCode(ApiDescription apiDescription, int statusCode)
    {
        return apiDescription.SupportedResponseTypes.Any(r => r.StatusCode == statusCode);
    }

    private static bool IsReadMethod(string? method)
    {
        return method is not null && (HttpMethods.IsGet(method) || HttpMethods.IsHead(method));
    }
}
