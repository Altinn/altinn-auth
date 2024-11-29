using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Services;

/// <summary>
/// Sets up an access token generator for development environment using the web based Altinn test token generator.
/// </summary>
[ExcludeFromCodeCoverage]
public class DevAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly TokenGeneratorSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevAccessTokenGenerator"/> class.
    /// </summary>
    public DevAccessTokenGenerator(IOptions<TokenGeneratorSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <inheritdoc/>
    public string GenerateAccessToken(string issuer, string app)
    {
        return GetToken(app);
    }

    /// <inheritdoc/>
    public string GenerateAccessToken(string issuer, string app, X509Certificate2 certificate)
    {
        return GetToken(app);
    }

    private string GetToken(string app)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.Url}?env={_settings.Env}&app={app}");
        request.Headers.Authorization = new BasicAuthenticationHeaderValue(_settings.User, _settings.Password);

        using HttpClient client = new HttpClient();
        var response = client.SendAsync(request).Result.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }
}
