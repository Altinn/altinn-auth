#nullable enable

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
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

            var parameters = operation.Parameters;
            if (parameters is null)
            {
                continue;
            }

            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].Name == description.Name)
                {
                    parameters.RemoveAt(i);
                    break;
                }
            }
        }

        if (add)
        {
            var parameters = operation.Parameters ??= new List<IOpenApiParameter>();
            AddRequestConditionsHeaders(parameters);
        }
    }

    private static void AddResponseVersionHeadersToConditionalResponses(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasConditional = context.ApiDescription.GetProperty<Marker>() is not null;

        if (!hasConditional)
        {
            return;
        }

        var responses = operation.Responses;
        if (responses is null)
        {
            return;
        }

        AddResponseVersionHeaders(responses, StatusCodes.Status200OK);
        AddResponseVersionHeaders(responses, StatusCodes.Status304NotModified);

        if (responses.TryGetValue(StatusCodes.Status412PreconditionFailed.ToString(), out var precondFailedResponse)
            && precondFailedResponse is OpenApiResponse precondFailedOpenApiResponse)
        {
            precondFailedOpenApiResponse.Description = "Precondition Failed";
        }
    }

    private static void AddResponseVersionHeaders(OpenApiResponses responses, int statusCode)
    {
        if (!responses.TryGetValue(statusCode.ToString(), out var response) || response is not OpenApiResponse openApiResponse)
        {
            return;
        }

        var headers = openApiResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
        headers.TryAdd("ETag", new OpenApiHeader()
        {
            Description = "The version tag of the resource",
            Schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.String,
            },
        });

        headers.TryAdd("Last-Modified", new OpenApiHeader()
        {
            Description = "The last modified date of the resource",
            Schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.String,
            },
        });
    }

    private static void AddRequestConditionsHeaders(IList<IOpenApiParameter> parameters)
    {
        parameters.Add(new OpenApiParameter()
        {
            Name = "If-Match",
            In = ParameterLocation.Header,
            Description = "If-Match header",
            Schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.String,
            },
        });

        parameters.Add(new OpenApiParameter()
        {
            Name = "If-None-Match",
            In = ParameterLocation.Header,
            Description = "If-None-Match header",
            Schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.String,
            },
        });

        parameters.Add(new OpenApiParameter()
        {
            Name = "If-Modified-Since",
            In = ParameterLocation.Header,
            Description = "If-Modified-Since header",
            Schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.String,
            },
        });

        parameters.Add(new OpenApiParameter()
        {
            Name = "If-Unmodified-Since",
            In = ParameterLocation.Header,
            Description = "If-Unmodified-Since header",
            Schema = new OpenApiSchema()
            {
                Type = JsonSchemaType.String,
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
