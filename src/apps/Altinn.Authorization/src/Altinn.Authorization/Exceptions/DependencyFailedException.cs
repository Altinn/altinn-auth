using System.Net;

namespace Altinn.Platform.Authorization.Exceptions;

/// <summary>
/// A lookup against a service the decision depends on failed for a technical reason: the service
/// answered with a status that says it could not serve the request, or it did not answer at all.
/// </summary>
/// <remarks>
/// This is not the same as the service answering that it has nothing for the request. A lookup that
/// finds nothing leads to a decision, and a lookup that fails must never do so: the decision would
/// then say "no access" about something the PDP was unable to check.
/// </remarks>
public class DependencyFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyFailedException"/> class.
    /// </summary>
    /// <param name="dependency">The name of the service the lookup was made against</param>
    /// <param name="statusCode">The status code the service answered with, or null when it did not answer</param>
    /// <param name="innerException">The exception that stopped the call, when there is one</param>
    public DependencyFailedException(string dependency, HttpStatusCode? statusCode, Exception innerException = null)
        : base(CreateMessage(dependency, statusCode), innerException)
    {
        Dependency = dependency;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the name of the service the lookup was made against.
    /// </summary>
    public string Dependency { get; }

    /// <summary>
    /// Gets the status code the service answered with, or null when it did not answer.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    // The response body is deliberately left out: it can repeat identifiers from the request.
    private static string CreateMessage(string dependency, HttpStatusCode? statusCode) =>
        statusCode.HasValue
            ? $"The lookup against {dependency} failed with status code {(int)statusCode.Value}."
            : $"The lookup against {dependency} failed without a response.";
}
