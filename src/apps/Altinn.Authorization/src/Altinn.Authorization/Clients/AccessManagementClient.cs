using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Altinn.Platform.Authorization.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Clients;

/// <summary>
/// Client Configuration for Altinn Access Management API
/// </summary>
public class AccessManagementClient
{
    /// <summary>
    /// Gets an instance of httpclient from httpclientfactory
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Gets an instance of the configuration required by the http client
    /// </summary>
    public IOptions<PlatformSettings> Settings { get; }

    /// <summary>
    /// Initializes the HTTP client for the Altinn Access Management API
    /// </summary>
    /// <param name="client">The default HTTP client</param>
    /// <param name="settings">the settings required by the HTTP client</param>
    public AccessManagementClient(HttpClient client, IOptions<PlatformSettings> settings)
    {
        Settings = settings;
        Client = client;
        client.Timeout = new TimeSpan(0, 0, 30);
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
