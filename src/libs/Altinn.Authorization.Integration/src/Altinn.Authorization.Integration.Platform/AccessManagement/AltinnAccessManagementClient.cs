using Altinn.AccessManagement.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.AccessManagement;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="HttpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="Options">Configuration options for the Altinn Register service.</param>
/// <param name="PlatformOptions">Options for configuring platform integration services.</param>
/// <param name="AccessManagementOptions">Options for configuring access management services.</param>
/// <param name="TokenGenerator">Service for generating platform access tokens.</param>
public partial class AltinnAccessManagementClient(
    IHttpClientFactory HttpClientFactory,
    IOptions<AltinnAccessManagementClient> Options,
    IOptions<AltinnIntegrationOptions> PlatformOptions,
    IOptions<AltinnAccessManagementOptions> AccessManagementOptions,
    ITokenGenerator TokenGenerator
) : IAltinnAccessManagement
{
    private HttpClient HttpClient => HttpClientFactory.CreateClient(PlatformOptions.Value.HttpClientName);

    /// <inheritdoc />
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<DelegationChange>>>> StreamAppRightDelegations(string nextPage = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(AccessManagementOptions.Value.Endpoint, "/accessmanagement/api/v1/internal/singleright/appdelegation/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithPlatformAccessToken(async () => await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<DelegationChange>(HttpClient, response, request);
    }

    /// <inheritdoc />
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<InstanceDelegationChange>>>> StreamInstanceRightDelegations(string nextPage = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(AccessManagementOptions.Value.Endpoint, "/accessmanagement/api/v1/internal/singleright/instancedelegation/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithPlatformAccessToken(async () => await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<InstanceDelegationChange>(HttpClient, response, request);
    }

    /// <inheritdoc />
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<DelegationChange>>>> StreamResourceRegistryRightDelegations(string nextPage = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(AccessManagementOptions.Value.Endpoint, "/accessmanagement/api/v1/internal/singleright/resourcedelegation/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithPlatformAccessToken(async () => await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);
        
        return new PaginatorStream<DelegationChange>(HttpClient, response, request);
    }
}

/// <summary>
/// Interface for interacting with the Altinn Register service.
/// </summary>
public interface IAltinnAccessManagement
{
    /// <summary>
    /// Streams a paginated list of single app right delegations from the RightsInternal controller.
    /// </summary>
    /// <param name="nextPage">The URL of the next page, if paginated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of paginated <see cref="DelegationChange"/> items.</returns>
    Task<IAsyncEnumerable<PlatformResponse<PageStream<DelegationChange>>>> StreamAppRightDelegations(string nextPage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a paginated list of single resource registry right delegations from the RightsInternal controller.
    /// </summary>
    /// <param name="nextPage">The URL of the next page, if paginated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of paginated <see cref="DelegationChange"/> items.</returns>
    Task<IAsyncEnumerable<PlatformResponse<PageStream<DelegationChange>>>> StreamResourceRegistryRightDelegations(string nextPage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a paginated list of single instance delegations from the RightsInternal controller.
    /// </summary>
    /// <param name="nextPage">The URL of the next page, if paginated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of paginated <see cref="InstanceDelegationChange"/> items.</returns>
    Task<IAsyncEnumerable<PlatformResponse<PageStream<InstanceDelegationChange>>>> StreamInstanceRightDelegations(string nextPage = null, CancellationToken cancellationToken = default);
}
