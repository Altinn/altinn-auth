using System.Net;
using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;

namespace Altinn.AccessManagement.Core.Clients.Interfaces
{
    /// <summary>
    /// Interface for the IdPorten authorization client
    /// </summary>
    public interface IIdPortenAuthorizationClient
    {
        /// <summary>
        /// Returns all ID-porten authorizations for the given user.
        /// </summary>
        /// <returns></returns>
        Task<IdPortenClientResult<List<IdPortenAuthorization>>> GetIdPortenAuthorizations(string ssn, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a specific ID-porten authorization based on the id.
        /// </summary>
        /// <returns></returns>
        Task<IdPortenClientResult<bool>> DeleteIdPortenAuthorization(string ssn, string id, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Raw outcome of a call to the IdPorten authorization API: the status code the external
    /// API returned, together with the deserialized payload on success. Lets callers map the
    /// external status onto a domain result without the transport client knowing about the
    /// API error catalog.
    /// </summary>
    /// <typeparam name="T">The type of the deserialized payload.</typeparam>
    /// <param name="StatusCode">The HTTP status code returned by the external API.</param>
    /// <param name="Value">The deserialized payload, or <c>default</c> on a non-success response.</param>
    public readonly record struct IdPortenClientResult<T>(HttpStatusCode StatusCode, T? Value)
    {
        /// <summary>
        /// Gets a value indicating whether the external API returned a 2xx status code.
        /// </summary>
        public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
    }
}
