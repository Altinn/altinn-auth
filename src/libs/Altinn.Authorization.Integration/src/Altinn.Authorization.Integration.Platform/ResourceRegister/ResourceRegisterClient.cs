using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.ResourceRegister;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP client instances.</param>
/// <param name="options">Configuration options for the Altinn Resource Register service.</param>
public partial class ResourceRegisterClient(IHttpClientFactory httpClientFactory, IOptions<AltinnResourceRegisterOptions> options) : IAltinnResourceRegister
{
    /// <summary>
    /// The name of the HTTP client used to communicate with the Altinn Register service.
    /// </summary>
    internal const string HttpClientName = "Altinn Resource Register";

    private HttpClient HttpClient => HttpClientFactory.CreateClient(HttpClientName);

    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    private IOptions<AltinnResourceRegisterOptions> Options { get; } = options;
}

/// <summary>
/// Set of methods for interacting with Altinn Resource Register API.
/// </summary>
public interface IAltinnResourceRegister
{
    /// <summary>
    /// Streams updated resources from the Altinn Resource Register in a paginated manner.
    /// </summary>
    /// <param name="nextPage">The URL of the next page of resources (if available).</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>An asynchronous stream of paginated <see cref="ResourceUpdatedModel"/> objects.</returns>
    Task<IAsyncEnumerable<Paginated<ResourceUpdatedModel>>> StreamResources(string nextPage = null, CancellationToken cancellationToken = default);
}
