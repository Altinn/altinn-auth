#nullable enable

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models.ApiDescriptions;

/// <summary>
/// An <see cref="IOperationFilter"/> for adding conditional headers to requests and responses.
/// </summary>
public class ConditionalOperationFilter : IOperationFilter
{ 
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Guard.IsNotNull(operation);
        Guard.IsNotNull(context);

        AddRequestConditionsHeadersToRequests(operation, context);
        AddResponseVersionHeadersToConditionalResponses(operation, context);
    }

    private static void AddRequestConditionsHeadersToRequests(OpenApiOperation operation, OperationFilterContext context)
    {
        var descriptions = context.ApiDescription.ParameterDescriptions.Where(p => RequestConditionCollection.GetETagType(p.ModelMetadata.ModelType) is not null);
        var add = false;

        foreach (var description in descriptions)
        {
            add = true;
            for (var i = 0; i < operation.Parameters.Count; i++)
            {
                if (operation.Parameters[i].Name == description.Name)
                {
                    operation.Parameters.RemoveAt(i);
                    break;
                }
            }
        }

        if (add)
        {
            AddRequestConditionsHeaders(operation.Parameters);
        }
    }

    private static void AddResponseVersionHeadersToConditionalResponses(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasConditional = context.ApiDescription.GetProperty<Marker>() is not null;

        if (!hasConditional)
        {
            return;
        }

        AddResponseVersionHeaders(operation.Responses, StatusCodes.Status200OK);
        AddResponseVersionHeaders(operation.Responses, StatusCodes.Status304NotModified);

        if (operation.Responses.TryGetValue(StatusCodes.Status412PreconditionFailed.ToString(), out var precondFailedResponse))
        {
            precondFailedResponse.Description = "Precondition Failed";
        }
    }

    private static void AddResponseVersionHeaders(OpenApiResponses responses, int statusCode)
    {
        if (!responses.TryGetValue(statusCode.ToString(), out var response))
        {
            return;
        }

        var headers = response.Headers;
        headers.TryAdd("ETag", new OpenApiHeader()
        {
            Description = "The version tag of the resource",
            Schema = new OpenApiSchema()
            {
                Type = "string",
            },
        });

        headers.TryAdd("Last-Modified", new OpenApiHeader()
        {
            Description = "The last modified date of the resource",
            Schema = new OpenApiSchema()
            {
                Type = "string",
            },
        });
    }

    private static void AddRequestConditionsHeaders(IList<OpenApiParameter> parameters)
    {
        parameters.Add(new OpenApiParameter()
        {
            Name = "If-Match",
            In = ParameterLocation.Header,
            Description = "If-Match header",
            Schema = new OpenApiSchema()
            {
                Type = "string",
            },
        });

        parameters.Add(new OpenApiParameter()
        {
            Name = "If-None-Match",
            In = ParameterLocation.Header,
            Description = "If-None-Match header",
            Schema = new OpenApiSchema()
            {
                Type = "string",
            },
        });

        parameters.Add(new OpenApiParameter()
        {
            Name = "If-Modified-Since",
            In = ParameterLocation.Header,
            Description = "If-Modified-Since header",
            Schema = new OpenApiSchema()
            {
                Type = "string",
            },
        });

        parameters.Add(new OpenApiParameter()
        {
            Name = "If-Unmodified-Since",
            In = ParameterLocation.Header,
            Description = "If-Unmodified-Since header",
            Schema = new OpenApiSchema()
            {
                Type = "string",
            },
        });
    }

    /// <summary>
    /// Marker struct for indicating than an operation has conditional responses.
    /// </summary>
    internal class Marker
    {
        /// <summary>
        /// Gets singleton instance of <see cref="Marker"/>.
        /// </summary>
        public static Marker Instance { get; } = new();

        private Marker()
        { 
        }
    }
}
