using System.Net;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.Authorization.Integration.Platform;

/// <summary>
/// Represents a platform response that contains HTTP status information, content, and potential problem details.
/// </summary>
/// <typeparam name="T">The type of the response content.</typeparam>
public class PlatformResponse<T>
{
    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the content of the response.
    /// </summary>
    public T Content { get; set; }

    /// <summary>
    /// Gets or sets the problem details if the response indicates an error.
    /// </summary>
    public AltinnProblemDetails ProblemDetails { get; set; }
}
