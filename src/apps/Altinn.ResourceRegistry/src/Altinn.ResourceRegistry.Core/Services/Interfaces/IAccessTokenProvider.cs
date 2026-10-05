namespace Altinn.ResourceRegistry.Core.Services.Interfaces
{
    /// <summary>
    /// Provides an access token that can be used by HTTP clients
    /// </summary>
    public interface IAccessTokenProvider
    {
        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <returns>An access token as a printable string</returns>
        public Task<string> GetAccessToken();
    }
}
