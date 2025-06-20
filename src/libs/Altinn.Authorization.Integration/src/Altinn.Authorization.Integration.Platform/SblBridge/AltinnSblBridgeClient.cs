using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.SblBridge;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="HttpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="PlatformOptions">Options for configuring platform integration services.</param>
/// <param name="Options">Configuration options for the Altinn Register service.</param>
public partial class AltinnSblBridgeClient(
    IHttpClientFactory HttpClientFactory,
    IOptions<AltinnIntegrationOptions> PlatformOptions,
    IOptions<AltinnSblBridgeOptions> Options
) : IAltinnSblBridge
{
    private HttpClient HttpClient => HttpClientFactory.CreateClient(PlatformOptions.Value.HttpClientName);
}

/// <summary>
/// Interface for interacting with the Altinn Register service.
/// </summary>
public interface IAltinnSblBridge
{
}
