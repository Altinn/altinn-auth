using Altinn.Common.AccessTokenClient.Services;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="accessTokenGenerator">Service for generating access tokens.</param>
/// <param name="options">Configuration options for the Altinn Register service.</param>
public partial class RegisterClient(IHttpClientFactory httpClientFactory, IAccessTokenGenerator accessTokenGenerator, IOptions<AltinnRegisterOptions> options) : IAltinnRegister
{
    /// <summary>
    /// The name of the HTTP client used to communicate with the Altinn Register service.
    /// </summary>
    internal const string HttpClientName = "Altinn Register";

    private HttpClient HttpClient => HttpClientFactory.CreateClient(HttpClientName);

    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    private IOptions<AltinnRegisterOptions> Options { get; } = options;

    private IAccessTokenGenerator AccessTokenGenerator { get; } = accessTokenGenerator;
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
    Task<IAsyncEnumerable<Paginated<PartyModel>>> StreamParties(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a paginated list of roles from the Altinn Register service.
    /// </summary>
    /// <param name="fields">The fields to include in the response.</param>
    /// <param name="nextPage">The URL of the next page, if paginated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of paginated <see cref="PartyModel"/> items.</returns>
    Task<IAsyncEnumerable<Paginated<RoleModel>>> StreamRoles(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default);
}
