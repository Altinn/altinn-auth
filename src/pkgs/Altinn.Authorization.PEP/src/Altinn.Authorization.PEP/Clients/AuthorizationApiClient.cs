using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Common.PEP.Clients
{
    /// <summary>
    /// Represents a form of types HttpClient for communication with the Authorization platform service.
    /// </summary>
    public class AuthorizationApiClient
    {
        private const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";
        private const string ForwardedForHeaderName = "x-forwarded-for";
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Initialize a new instance of the <see cref="AuthorizationApiClient"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">the heep context accessor</param>
        /// <param name="client">A HttpClient provided by the built in HttpClientFactory.</param>
        /// <param name="platformSettings">The current platform settings</param>
        /// <param name="logger">A logger provided by the built in LoggerFactory.</param>
        public AuthorizationApiClient(
                IHttpContextAccessor httpContextAccessor,
                HttpClient client,
                IOptions<PlatformSettings> platformSettings,
                ILogger<AuthorizationApiClient> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = client;
            _logger = logger;

            if (!_httpClient.DefaultRequestHeaders.Contains(ForwardedForHeaderName))
            {
                string clientIpAddress = _httpContextAccessor?.HttpContext?.Request?.Headers?[ForwardedForHeaderName];
                _httpClient.DefaultRequestHeaders.Add(ForwardedForHeaderName, clientIpAddress);
            }

            client.BaseAddress = new Uri($"{platformSettings.Value.ApiAuthorizationEndpoint}");
            client.DefaultRequestHeaders.Add(SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Method for performing authorization.
        /// </summary>
        /// <param name="xacmlJsonRequest">An authorization request.</param>
        /// <returns>The result of the authorization request.</returns>
        public Task<XacmlJsonResponse> AuthorizeRequest(XacmlJsonRequestRoot xacmlJsonRequest)
            => AuthorizeRequest(xacmlJsonRequest, CancellationToken.None);

        /// <summary>
        /// Method for performing authorization.
        /// </summary>
        /// <param name="xacmlJsonRequest">An authorization request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>The result of the authorization request.</returns>
        public async Task<XacmlJsonResponse> AuthorizeRequest(XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken)
        {
            XacmlJsonResponse xacmlJsonResponse = null;
            string apiUrl = $"decision";
            string requestJson = JsonSerializer.Serialize(xacmlJsonRequest, jsonOptions);
            using StringContent httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            using HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, httpContent, cancellationToken);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            _logger.LogInformation("Authorization PDP time elapsed: " + ts.TotalMilliseconds);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                xacmlJsonResponse = await response.Content.ReadFromJsonAsync<XacmlJsonResponse>(jsonOptions, cancellationToken);
            }
            else
            {
                _logger.LogInformation($"// PDPAppSI // GetDecisionForRequest // Non-zero status code: {response.StatusCode}");
                _logger.LogInformation($"// PDPAppSI // GetDecisionForRequest // Response: {await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            return xacmlJsonResponse;
        }
    }
}
