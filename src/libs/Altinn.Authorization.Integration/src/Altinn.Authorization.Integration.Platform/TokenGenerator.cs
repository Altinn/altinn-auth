using System.Security.Cryptography.X509Certificates;
using Altinn.Common.AccessTokenClient.Services;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform;

internal class TokenGenerator
{
    internal class TestTool(IOptions<AltinnIntegrationOptions> options, IHttpClientFactory httpClientFactory) : ITokenGenerator
    {
        private IOptions<AltinnIntegrationOptions> Options { get; } = options;

        private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

        /// <inheritdoc/>
        public Task<string> CreateJWT(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<string> CreateJWT(string issuer, string app, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<string> CreatePlatformAccessToken(CancellationToken cancellationToken = default)
        {
            var opts = Options.Value;
            return await CreatePlatformAccessToken(opts.PlatformAccessToken.Issuer, opts.PlatformAccessToken.App, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string> CreatePlatformAccessToken(string issuer, string app, CancellationToken cancellationToken = default)
        {
            var opts = Options.Value;

            var request = RequestComposer.New(
                RequestComposer.WithSetUri(opts.PlatformAccessToken.TestTool.Endpoint, "/api/GetPlatformAccessToken"),
                RequestComposer.WithHttpVerb(HttpMethod.Get),
                RequestComposer.WithAppendQueryParam("env", opts.PlatformAccessToken.TestTool.Environment),
                RequestComposer.WithAppendQueryParam("ttl", 3600),
                RequestComposer.WithAppendQueryParam("app", app),
                RequestComposer.WithBasicAuth(opts.PlatformAccessToken.TestTool.Username, opts.PlatformAccessToken.TestTool.Password)
            );

            var client = HttpClientFactory.CreateClient(opts.HttpClientName);

            var response = await client.SendAsync(request, cancellationToken);

            var result = ResponseComposer.Handle<string>(
                response,
                ResponseComposer.SetBodyAsStringResultIfSuccesful
            );

            if (!result.IsSuccessful)
            {
                throw new UnauthorizedAccessException("Failed to retrieve a successful token from the test tool token generator");
            }

            return result.Content;
        }
    }

    internal class KeyVault(
            IOptions<AltinnIntegrationOptions> options,
            IAzureClientFactory<SecretClient> azureSecretClientFactory,
            IAzureClientFactory<CertificateClient> azureCertificateClientFactory,
            IAccessTokenGenerator platformKeyVault) : ITokenGenerator
    {
        public const string ServiceKey = "AltinnPlatformKeyVault";

        private SemaphoreSlim Semaphore { get; } = new(1, 1);

        private IAccessTokenGenerator PlatformTokenGenerator { get; } = platformKeyVault;

        private IOptions<AltinnIntegrationOptions> Options { get; } = options;

        private IAzureClientFactory<SecretClient> AzureSecretClientFactory { get; } = azureSecretClientFactory;

        private IAzureClientFactory<CertificateClient> AzureCertificateClientFactory { get; } = azureCertificateClientFactory;

        private string AccessToken { get; set; } = string.Empty;

        private DateTimeOffset AccessTokenExpires { get; set; } = DateTime.MinValue;

        /// <inheritdoc/>
        public async Task<string> CreatePlatformAccessToken(CancellationToken cancellationToken = default)
        {
            var opts = Options.Value;
            return await CreatePlatformAccessToken(opts.PlatformAccessToken.Issuer, opts.PlatformAccessToken.App, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<string> CreatePlatformAccessToken(string issuer, string app, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PlatformTokenGenerator.GenerateAccessToken(issuer, app));
        }

        /// <inheritdoc/>
        public async Task<string> CreateJWT(CancellationToken cancellationToken = default)
        {
            var opts = Options.Value;
            return await CreateJWT(opts.PlatformAccessToken.Issuer, opts.PlatformAccessToken.App, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string> CreateJWT(string issuer, string app, CancellationToken cancellationToken = default)
        {
            await Semaphore.WaitAsync(cancellationToken);

            try
            {
                if (string.IsNullOrEmpty(AccessToken) || AccessTokenExpires < DateTime.UtcNow)
                {
                    return await GenerateAndSetJWTToken(issuer, app, cancellationToken);
                }
            }
            finally
            {
                Semaphore.Release();
            }

            return AccessToken;
        }

        private async Task<string> GenerateAndSetJWTToken(string issuer, string app, CancellationToken cancellationToken)
        {
            try
            {
                var opts = Options.Value;
                var certClient = AzureCertificateClientFactory.CreateClient(ServiceKey);
                var secretClient = AzureSecretClientFactory.CreateClient(ServiceKey);
                await foreach (var cert in certClient.GetPropertiesOfCertificateVersionsAsync("JWTCertificate", cancellationToken))
                {
                    if ((cert.Enabled == true && cert.ExpiresOn == null) || cert.ExpiresOn >= DateTime.UtcNow)
                    {
                        var secret = await secretClient.GetSecretAsync(cert.Name, cert.Version, cancellationToken);
                        var pkcs12 = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(secret.Value.Value), null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                        AccessToken = PlatformTokenGenerator.GenerateAccessToken(issuer, app, pkcs12);
                        AccessTokenExpires = cert.ExpiresOn ?? secret.Value.Properties.ExpiresOn ?? DateTime.UtcNow.AddSeconds(opts.PlatformAccessToken.KeyVault.CacheTimeout);
                        return AccessToken;
                    }
                }
            }
            catch (Azure.RequestFailedException ex)
            {
                throw new UnauthorizedAccessException("Failed to retrieve certificate from key vault", ex);
            }

            throw new UnauthorizedAccessException("Couldn't find any cert 'JWTCertificate' that's enabled or not expired");
        }
    }
}

public interface ITokenGenerator
{
    /// <summary>
    /// Creates a new Platform access token
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<string> CreateJWT(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Platform Access Token
    /// </summary>
    /// <param name="issuer">token issuer</param>
    /// <param name="app">app name</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<string> CreateJWT(string issuer, string app, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Platform access token
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<string> CreatePlatformAccessToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Platform Access Token
    /// </summary>
    /// <param name="issuer">token issuer</param>
    /// <param name="app">app name</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<string> CreatePlatformAccessToken(string issuer, string app, CancellationToken cancellationToken = default);
}
