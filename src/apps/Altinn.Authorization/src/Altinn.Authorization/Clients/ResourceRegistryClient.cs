using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Altinn.Platform.Authorization.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Clients
{
    /// <summary>
    /// Client configuration for resource registry
    /// </summary>
    public class ResourceRegistryClient
    {        
        /// <summary>
        /// Gets an instance of httpclient from httpclientfactory
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// Initializes the http client for actor
        /// </summary>
        /// <param name="client">the http client</param>
        /// <param name="platformSettings">the platform settings configured for the authorization component</param>
        public ResourceRegistryClient(HttpClient client, IOptions<PlatformSettings> platformSettings)
        {
            PlatformSettings settings = platformSettings.Value;
            Client = client;
            Client.BaseAddress = new Uri(settings.ApiResourceRegistryEndpoint);
            Client.Timeout = new TimeSpan(0, 0, 30);
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
