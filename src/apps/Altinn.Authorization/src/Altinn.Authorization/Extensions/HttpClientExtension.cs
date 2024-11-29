using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Altinn.Platform.Authorization.Extensions
{
    /// <summary>
    /// This extension is created to make it easy to add a bearer token to a HttpRequests. 
    /// </summary>
    public static class HttpClientExtension
    {
        /// <summary>
        /// Extension that add authorization header to request
        /// </summary>
        /// <param name="httpClient">The HttpClient</param>
        /// <param name="requestUri">The request Uri</param>
        /// <param name="content">The http content</param>
        /// <param name="authorizationToken">the authorization token (jwt)</param>
        /// <param name="platformAccessToken">The platformAccess tokens</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A HttpResponseMessage</returns>
        public static Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, string requestUri, HttpContent content, string authorizationToken = null, string platformAccessToken = null, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(requestUri, UriKind.Relative));
            request.Content = content;

            if (!string.IsNullOrEmpty(authorizationToken))
            {
                request.Headers.Add("Authorization", "Bearer " + authorizationToken);
            }

            if (!string.IsNullOrEmpty(platformAccessToken))
            {
                request.Headers.Add("PlatformAccessToken", platformAccessToken);
            }

            return httpClient.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Extension that add authorization header to request
        /// </summary>
        /// <param name="httpClient">The HttpClient</param>
        /// <param name="requestUri">The request Uri</param>
        /// <param name="authorizationToken">the authorization token (jwt)</param>
        /// <param name="platformAccessToken">The platformAccess tokens</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A HttpResponseMessage</returns>
        public static Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string requestUri, string authorizationToken = null, string platformAccessToken = null, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            if (!string.IsNullOrEmpty(authorizationToken))
            {
                request.Headers.Add("Authorization", "Bearer " + authorizationToken);
            }

            if (!string.IsNullOrEmpty(platformAccessToken))
            {
                request.Headers.Add("PlatformAccessToken", platformAccessToken);
            }

            return httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
    }
}
