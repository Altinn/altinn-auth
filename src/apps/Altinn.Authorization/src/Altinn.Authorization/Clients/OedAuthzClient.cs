using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Clients
{
    /// <summary>
    /// Client configuration for Oed Authz API integration
    /// </summary>
    public class OedAuthzClient
    {
        /// <summary>
        /// Gets an instance of httpclient from httpclientfactory
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// Initializes the http client for retrieving Oed Authz role-assignments
        /// </summary>
        /// <param name="client">the http client</param>
        /// <param name="settings">the general settings configured for the authorization component</param>
        public OedAuthzClient(HttpClient client, IOptions<GeneralSettings> settings)
        {
            GeneralSettings generalSettings = settings.Value;
            Client = client;
            Client.BaseAddress = new Uri(generalSettings.OedAuthzApiEndpoint);
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// post request that gets OED roleassignments
        /// </summary>
        /// <param name="requestBody">the request body</param>
        /// <param name="token">the bearer token</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<HttpResponseMessage> GetOedRoleAssignments(StringContent requestBody, AuthenticationHeaderValue token)
        {
            Client.DefaultRequestHeaders.Authorization = token;
            string endpoint = Client.BaseAddress + "v1/pip";
            return await Client.PostAsync(endpoint, requestBody);
        }
    }
}
