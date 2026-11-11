using System.Net;
using Altinn.Platform.Authorization.Exceptions;

namespace Altinn.Platform.Authorization.Helpers;

/// <summary>
/// Sends a lookup to a service the decision depends on, and tells a lookup that failed apart from
/// one the service answered.
/// </summary>
public static class DependencyCall
{
    /// <summary>Access Management, the source of roles, access packages and delegations.</summary>
    public const string AccessManagement = "access-management";

    /// <summary>The service holding the role assignments of estates (Digitalt dødsbo).</summary>
    public const string OedAuthz = "oed-authz";

    /// <summary>Profile, the source of user profiles.</summary>
    public const string Profile = "profile";

    /// <summary>Register, the source of parties.</summary>
    public const string Register = "register";

    /// <summary>Resource Registry, the source of resources, their policies and access lists.</summary>
    public const string ResourceRegistry = "resource-registry";

    /// <summary>Storage, the source of the process state of an instance.</summary>
    public const string Storage = "storage";

    /// <summary>
    /// Sends the request, and throws <see cref="DependencyFailedException"/> when the service fails to
    /// serve it. The response is returned for the caller to read in every other case, including the
    /// ones where the service answers that it has nothing for the request.
    /// </summary>
    /// <param name="dependency">The name of the service, one of the constants of this class</param>
    /// <param name="send">Sends the request</param>
    /// <param name="cancellationToken">The token the request is sent with</param>
    /// <returns>The response from the service</returns>
    public static async Task<HttpResponseMessage> SendAsync(string dependency, Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex)
        {
            throw new DependencyFailedException(dependency, ex.StatusCode, ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Nobody asked for the call to be cancelled, so this is the timeout of the HttpClient.
            throw new DependencyFailedException(dependency, null, ex);
        }

        if (IsFailure(response.StatusCode))
        {
            HttpStatusCode statusCode = response.StatusCode;
            response.Dispose();
            throw new DependencyFailedException(dependency, statusCode);
        }

        return response;
    }

    /// <summary>
    /// Tells whether a status code means the service failed to serve the request.
    /// </summary>
    /// <remarks>
    /// 400 and 404 are answers: the service looked, and has nothing for what it was asked about.
    /// Everything else outside 2xx is a failure, also 401 and 403, which mean that the PDP's own
    /// access to the service is broken and say nothing about the subject of the decision.
    /// </remarks>
    /// <param name="statusCode">The status code of the response</param>
    /// <returns>True when the service failed to serve the request</returns>
    public static bool IsFailure(HttpStatusCode statusCode) =>
        (int)statusCode is < 200 or > 299
        && statusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.NotFound);
}
