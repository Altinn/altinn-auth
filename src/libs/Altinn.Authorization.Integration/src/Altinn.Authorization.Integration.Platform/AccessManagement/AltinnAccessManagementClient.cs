using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.AccessManagement;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="options">Configuration options for the Altinn Register service.</param>
internal partial class AltinnAccessManagementClient(IHttpClientFactory httpClientFactory, IOptions<AltinnAccessManagementClient> options) : IAltinnAccessManagement
{
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    private IOptions<AltinnAccessManagementClient> Options { get; } = options;
}

/// <summary>
/// Interface for interacting with the Altinn Register service.
/// </summary>
public interface IAltinnAccessManagement
{
}
