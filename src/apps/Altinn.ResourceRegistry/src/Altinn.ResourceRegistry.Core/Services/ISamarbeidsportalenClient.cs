#nullable enable

using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Client for the open (unauthenticated) metadata API in Samarbeidsportalen.
    /// </summary>
    public interface ISamarbeidsportalenClient
    {
        /// <summary>
        /// Fetches every Maskinporten scope prefix, active or not, with its owner.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>All prefixes in the configured environment</returns>
        /// <exception cref="HttpRequestException">The API answered with a non-success status code or could not be reached.</exception>
        /// <exception cref="InvalidOperationException">The endpoint is not configured, or the response has no body.</exception>
        Task<IReadOnlyList<MaskinportenScopePrefix>> GetScopePrefixes(CancellationToken cancellationToken = default);
    }
}
