using System.Net;
using System.Text.Json;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.Authorization.Integration.Platform;

/// <summary>
/// Provides utility methods to create and modify HTTP requests.
/// </summary>
public static class ResponseComposer
{
    /// <summary>
    /// Handles an HTTP response and processes it using provided response handlers.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="responseHandlers">An array of response handlers to process the response context.</param>
    /// <returns>A <see cref="PlatformResponse{T}"/> containing the processed result.</returns>
    public static PlatformResponse<T> Handle<T>(HttpResponseMessage response, params Action<ResponseContext<T>>[] responseHandlers)
    {
        var context = new ResponseContext<T>()
        {
            Content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
            Message = response,
            Result = new PlatformResponse<T>()
            {
                IsSuccessful = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode
            },
        };

        foreach (var handler in responseHandlers)
        {
            handler(context);
        }

        return context.Result;
    }

    /// <summary>
    /// Deserializes the response content into the specified type if the response indicates success.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="context">The response context containing the HTTP response data.</param>
    public static void DeserializeResponseOnSuccess<T>(ResponseContext<T> context)
    {
        if (context.Message.IsSuccessStatusCode)
        {
            context.Result.Content = JsonSerializer.Deserialize<T>(context.Content);
        }
    }

    /// <summary>
    /// Deserializes the response content into <see cref="AltinnProblemDetails"/> if the response indicates failure.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="context">The response context containing the HTTP response data.</param>
    public static void DeserializeProblemDetailsOnUnsuccessStatusCode<T>(ResponseContext<T> context)
    {
        if (!context.Message.IsSuccessStatusCode)
        {
            try
            {
                context.Result.ProblemDetails = JsonSerializer.Deserialize<AltinnProblemDetails>(context.Content);
            }
            catch (JsonException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Creates a response handler that deserializes <see cref="AltinnProblemDetails"/> if the response has the specified HTTP status code.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    /// <param name="httpStatus">The HTTP status code to match.</param>
    /// <returns>An action that deserializes <see cref="AltinnProblemDetails"/> when the status code matches.</returns>
    public static Action<ResponseContext<T>> DeserilizeProblemDetailsOnStatusCode<T>(HttpStatusCode httpStatus) => context =>
    {
        if (context.Message.StatusCode == httpStatus)
        {
            try
            {
                context.Result.ProblemDetails = JsonSerializer.Deserialize<AltinnProblemDetails>(context.Content);
            }
            catch (JsonException)
            {
                return;
            }
        }
    };

    /// <summary>
    /// Represents the context for handling an HTTP response.
    /// </summary>
    /// <typeparam name="T">The type of the response content.</typeparam>
    public class ResponseContext<T>
    {
        /// <summary>
        /// Gets or sets the HTTP response message.
        /// </summary>
        public HttpResponseMessage Message { get; set; }

        /// <summary>
        /// Gets or sets the response content as a string.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the processed result of the response.
        /// </summary>
        public PlatformResponse<T> Result { get; set; }
    }
}
