using System.Web;
using Altinn.Common.AccessTokenClient.Services;

namespace Altinn.Authorization.Integration.Platform;

/// <summary>
/// Provides utility methods to create and modify HTTP requests.
/// </summary>
public static class RequestComposer
{
    /// <summary>
    /// Creates a new HTTP request message and applies the provided actions to it.
    /// </summary>
    /// <param name="actions">A set of actions to configure the HTTP request.</param>
    /// <returns>A configured <see cref="HttpRequestMessage"/>.</returns>
    public static HttpRequestMessage New(params Action<HttpRequestMessage>[] actions)
    {
        var request = new HttpRequestMessage();
        foreach (var action in actions)
        {
            action(request);
        }

        return request;
    }

    /// <summary>
    /// Sets the HTTP method for the request.
    /// </summary>
    /// <param name="method">The HTTP method to set.</param>
    /// <returns>An action to configure the request method.</returns>
    public static Action<HttpRequestMessage> WithHttpVerb(HttpMethod method) => request =>
    {
        request.Method = method;
    };

    /// <summary>
    /// Sets the URI of the request.
    /// </summary>
    /// <remarks>
    /// ignores uri if empty string or null.
    /// </remarks>
    /// <param name="uri">The URI to set.</param>
    /// <returns>An action to configure the request URI.</returns>
    public static Action<HttpRequestMessage> WithSetUri(string uri) => request =>
    {
        if (!string.IsNullOrEmpty(uri))
        {
            request.RequestUri = new Uri(uri);
        }
    };

    /// <summary>
    /// Sets the URI of the request with additional segments.
    /// </summary>
    /// <remarks>
    /// ignores uri if null.
    /// </remarks>
    /// <param name="uri">The base URI.</param>
    /// <param name="segments">Additional path segments to append.</param>
    /// <returns>An action to configure the request URI.</returns>
    public static Action<HttpRequestMessage> WithSetUri(Uri uri, params string[] segments) => request =>
    {
        if (uri != null)
        {
            request.RequestUri = new Uri(uri, string.Join("/", segments));
        }
    };

    /// <summary>
    /// Appends a query parameter with multiple values to the request URI.
    /// </summary>
    /// <typeparam name="T">The type of values.</typeparam>
    /// <param name="param">The query parameter name.</param>
    /// <param name="values">The values to associate with the parameter.</param>
    /// <returns>An action to modify the request URI.</returns>
    public static Action<HttpRequestMessage> WithAppendQueryParam<T>(string param, IEnumerable<T> values) => request =>
    {
        values ??= [];
        if (request == null || string.IsNullOrEmpty(param) || !values.Any())
        {
            return;
        }

        var query = HttpUtility.ParseQueryString(request.RequestUri?.Query ?? string.Empty);
        query[param] = string.Join(",", values);
        request.RequestUri = new UriBuilder(request.RequestUri)
        {
            Query = query.ToString()
        }.Uri;
    };

    /// <summary>
    /// Appends a query parameter with a single value to the request URI.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="param">The query parameter name.</param>
    /// <param name="value">The value to associate with the parameter.</param>
    /// <returns>An action to modify the request URI.</returns>
    public static Action<HttpRequestMessage> WithAppendQueryParam<T>(string param, T value) => request =>
    {
        if (request == null || string.IsNullOrEmpty(param) || EqualityComparer<T>.Default.Equals(value, default))
        {
            return;
        }

        var query = HttpUtility.ParseQueryString(request.RequestUri?.Query ?? string.Empty);
        query[param] = value.ToString();
        request.RequestUri = new UriBuilder(request.RequestUri)
        {
            Query = query.ToString()
        }.Uri;
    };

    /// <summary>
    /// Adds a platform access token to the request headers.
    /// </summary>
    /// <param name="accessTokenGenerator">The access token generator.</param>
    /// <param name="app">The application identifier.</param>
    /// <param name="issuer">The token issuer (default: "platform").</param>
    /// <returns>An action to add the access token to the request headers.</returns>
    public static Action<HttpRequestMessage> WithPlatformAccessToken(IAccessTokenGenerator accessTokenGenerator, string app, string issuer = "platform") => request =>
    {
        var token = accessTokenGenerator.GenerateAccessToken(issuer, app);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("PlatformAccessToken", token);
        }
    };
}
