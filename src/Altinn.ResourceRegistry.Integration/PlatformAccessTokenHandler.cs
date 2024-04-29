using Altinn.Common.AccessTokenClient.Services;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Integration;

/// <summary>
/// A handler that adds the platform access token to the request.
/// </summary>
internal class PlatformAccessTokenHandler
    : DelegatingHandler
{
    private static readonly string PlatformAccessTokenHeaderName = "PlatformAccessToken";

    private readonly IAccessTokenGenerator _accessTokenGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformAccessTokenHandler"/> class.
    /// </summary>
    /// <param name="accessTokenGenerator">The <see cref="IAccessTokenGenerator"/>.</param>
    public PlatformAccessTokenHandler(IAccessTokenGenerator accessTokenGenerator)
    {
        _accessTokenGenerator = accessTokenGenerator;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Guard.IsNotNull(request);

        if (!request.Headers.Contains(PlatformAccessTokenHeaderName))
        {
            var accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");

            request.Headers.Add(PlatformAccessTokenHeaderName, accessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
