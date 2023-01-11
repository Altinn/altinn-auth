using System.Security.Cryptography.X509Certificates;
using Altinn.Common.AccessTokenClient.Configuration;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <inheritdoc />
    public class AccessTokenProvider : IAccessTokenProvider
    {
        private readonly IKeyVaultService _keyVaultService;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly PlatformSettings _platformSettings;
        private readonly AccessTokenSettings _accessTokenSettings;
        private readonly SecretsSettings _secretsSettings;
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private static DateTime _cacheTokenUntil = DateTime.MinValue;
        private string _accessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenProvider"/> class.
        /// </summary>
        /// <param name="keyVaultService">The key vault service.</param>
        /// <param name="accessTokenGenerator">The access token generator.</param>
        /// <param name="accessTokenSettings">The access token settings.</param>
        /// <param name="keyVaultSettings">The key vault settings.</param>
        /// <param name="platformSettings">The platform settings.</param>
        public AccessTokenProvider(
            IAccessTokenGenerator accessTokenGenerator,
            IOptions<AccessTokenSettings> accessTokenSettings,
            IOptions<PlatformSettings> platformSettings)
        {
            _accessTokenGenerator = accessTokenGenerator;
            _platformSettings = platformSettings.Value;
            _accessTokenSettings = accessTokenSettings.Value;
        }

        /// <inheritdoc />
        public async Task<string> GetAccessToken()
        {
            await Semaphore.WaitAsync();

            try
            {
                if (_accessToken == null || _cacheTokenUntil < DateTime.UtcNow)
                {
                    _accessToken = _accessTokenGenerator.GenerateAccessToken(
                        _platformSettings.AccessTokenIssuer,
                        "internal.authorization");

                    _cacheTokenUntil = DateTime.UtcNow.AddSeconds(_accessTokenSettings.TokenLifetimeInSeconds - 2); // Add some slack to avoid tokens expiring in transit
                }

                return _accessToken;
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
