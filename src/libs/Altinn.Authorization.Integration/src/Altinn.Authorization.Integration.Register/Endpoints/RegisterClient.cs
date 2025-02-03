using Altinn.Authorization.Integration.Register.Extensions;
using Altinn.Authorization.Integration.Register.Options;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Register;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
public partial class RegisterClient
{
    /// <summary>
    /// The name of the HTTP client used to communicate with the Altinn Register service.
    /// </summary>
    internal const string HttpClientName = "Altinn Register";

    private HttpClient HttpClient => HttpClientFactory.CreateClient(HttpClientName);

    private IHttpClientFactory HttpClientFactory { get; }

    private IOptions<AltinnRegisterOptions> Options { get; }

    private readonly IAccessTokenGenerator _accessTokenGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="accessTokenGenerator">Service for generating access tokens.</param>
    /// <param name="options">Configuration options for the Altinn Register service.</param>
    public RegisterClient(
        IHttpClientFactory httpClientFactory,
        IAccessTokenGenerator accessTokenGenerator,
        IOptions<AltinnRegisterOptions> options)
    {
        HttpClientFactory = httpClientFactory;
        Options = options;
        _accessTokenGenerator = accessTokenGenerator;
    }

    /// <summary>
    /// Adds the platform access token authorization headers to an HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    private void AddAuthorization(HttpRequestMessage request)
    {
        var token = _accessTokenGenerator.GenerateAccessToken("platform", "access-management");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("PlatformAccessToken", token);
        }
    }
}
