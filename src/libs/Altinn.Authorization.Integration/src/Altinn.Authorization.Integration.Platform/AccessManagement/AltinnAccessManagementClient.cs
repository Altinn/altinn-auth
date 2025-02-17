using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.AccessManagement;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="options">Configuration options for the Altinn Register service.</param>
public partial class AltinnAccessManagementClient(IHttpClientFactory httpClientFactory, IOptions<AltinnAccessManagementClient> options) : IAltinnAccessManagement
{
    /// <summary>
    /// The name of the HTTP client used to communicate with the Altinn Register service.
    /// </summary>
    internal const string HttpClientName = "Altinn Access Management";

    private HttpClient HttpClient => HttpClientFactory.CreateClient(HttpClientName);

    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    private IOptions<AltinnAccessManagementClient> Options { get; } = options;
}

/// <summary>
/// Interface for interacting with the Altinn Register service.
/// </summary>
public interface IAltinnAccessManagement
{
}
