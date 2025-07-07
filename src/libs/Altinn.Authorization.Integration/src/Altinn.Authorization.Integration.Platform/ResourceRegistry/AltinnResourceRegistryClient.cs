using Microsoft.Extensions.Options;
using Yuniql.Extensibility;

namespace Altinn.Authorization.Integration.Platform.ResourceRegistry;

/// <summary>
/// Client for interacting with the Altinn Resource Register API.
/// </summary>
/// <param name="HttpClientFactory">Factory for creating HTTP client instances.</param>
/// <param name="ResourceRegistryOptions">Configuration options for the Altinn Resource Register.</param>
/// <param name="PlatformOptions">General platform integration options.</param>
public partial class AltinnResourceRegistryClient(
    IHttpClientFactory HttpClientFactory,
    IOptions<AltinnResourceRegistryOptions> ResourceRegistryOptions,
    IOptions<AltinnIntegrationOptions> PlatformOptions
) : IAltinnResourceRegistry
{
    private HttpClient HttpClient => HttpClientFactory.CreateClient(PlatformOptions.Value.HttpClientName);
}

/// <summary>
/// Set of methods for interacting with Altinn Resource Register API.
/// </summary>
public interface IAltinnResourceRegistry
{
    /// <summary>
    /// Streams updated resources from the Altinn Resource Register in a paginated manner.
    /// </summary>
    /// <param name="since">Updated resources since</param>
    /// <param name="nextPage">Optional. The URL of the next page of resources, if available.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>
    /// An asynchronous stream of paginated <see cref="ResourceUpdatedModel"/> objects,
    /// wrapped in a <see cref="PlatformResponse{T}"/>.
    /// </returns>
    Task<IAsyncEnumerable<PlatformResponse<PageStream<ResourceUpdatedModel>>>> StreamResources(DateTime since = default, string nextPage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves details of a specific resource from the Altinn Resource Register.
    /// </summary>
    /// <param name="id">The unique identifier of the resource.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>
    /// A <see cref="Task"/> that resolves to a <see cref="PlatformResponse{T}"/> 
    /// containing the requested <see cref="ResourceModel"/>.
    /// </returns>
    Task<PlatformResponse<ResourceModel>> GetResource(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves list of resource from the Altinn Resource Register.
    /// </summary>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>
    /// A <see cref="Task"/> that resolves to a <see cref="PlatformResponse{T}"/> 
    /// containing the requested <see cref="ResourceModel"/>.
    /// </returns>
    Task<PlatformResponse<List<ResourceModel>>> GetResources(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all service owners 
    /// </summary>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>
    /// A <see cref="Task"/> that resolves to a <see cref="PlatformResponse{T}"/> 
    /// containing the requested <see cref="ServiceOwners"/>.
    /// </returns>    
    Task<PlatformResponse<ServiceOwners>> GetServiceOwners(CancellationToken cancellationToken = default);
}
