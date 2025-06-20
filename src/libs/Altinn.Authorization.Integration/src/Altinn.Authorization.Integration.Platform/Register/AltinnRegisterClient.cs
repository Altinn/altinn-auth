using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="HttpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="RegisterOptions">Options for configuring the Altinn Register services.</param>
/// <param name="PlatformOptions">Options for configuring platform integration services.</param>
/// <param name="TokenGenerator">Service for generating authentication tokens.</param>
public partial class AltinnRegisterClient(
    IHttpClientFactory HttpClientFactory,
    IOptions<AltinnRegisterOptions> RegisterOptions,
    IOptions<AltinnIntegrationOptions> PlatformOptions,
    ITokenGenerator TokenGenerator
) : IAltinnRegister
{
    private HttpClient HttpClient => HttpClientFactory.CreateClient(PlatformOptions.Value.HttpClientName);
}

/// <summary>
/// Interface for interacting with the Altinn Register service.
/// </summary>
public interface IAltinnRegister
{
    /// <summary>
    /// Streams a paginated list of parties from the Altinn Register service.
    /// </summary>
    /// <param name="fields">The fields to include in the response.</param>
    /// <param name="nextPage">The URL of the next page, if paginated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of paginated <see cref="PartyModel"/> items.</returns>
    Task<IAsyncEnumerable<PlatformResponse<PageStream<PartyModel>>>> StreamParties(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a paginated list of roles from the Altinn Register service.
    /// </summary>
    /// <param name="fields">The fields to include in the response.</param>
    /// <param name="nextPage">The URL of the next page, if paginated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of paginated <see cref="RoleModel"/> items.</returns>
    Task<IAsyncEnumerable<PlatformResponse<PageStream<RoleModel>>>> StreamRoles(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default);
}
