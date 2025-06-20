using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.AccessManagement;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="HttpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="Options">Configuration options for the Altinn Register service.</param>
/// <param name="PlatformOptions">Options for configuring platform integration services.</param>
public partial class AltinnAccessManagementClient(
    IHttpClientFactory HttpClientFactory,
    IOptions<AltinnAccessManagementClient> Options,
    IOptions<AltinnIntegrationOptions> PlatformOptions
) : IAltinnAccessManagement
{
    private HttpClient HttpClient => HttpClientFactory.CreateClient(PlatformOptions.Value.HttpClientName);
}

/// <summary>
/// Interface for interacting with the Altinn Register service.
/// </summary>
public interface IAltinnAccessManagement
{
}
