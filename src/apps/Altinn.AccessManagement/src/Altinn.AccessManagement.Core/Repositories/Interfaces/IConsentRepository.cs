using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Repositories.Interfaces
{
    /// <summary>
    /// Repository for handling consent
    /// </summary>
    public interface IConsentRepository
    {
        /// <summary>
        /// Creates a consent request
        /// </summary>
        Task<ConsentRequestDetails> CreateRequest(ConsentRequest consentRequest, ConsentPartyUrn performedByParty, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a consent request. Can only be performed by the party that created the request. Will be soft deleted.
        /// </summary>
        Task DeleteRequest(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a specific consent request based on the id
        /// </summary>
        Task<ConsentRequestDetails> GetRequest(Guid consentRequestId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Approves a consent request
        /// </summary>
        Task AcceptConsentRequest(Guid consentRequestId, Guid performedByParty, ConsentContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rejects a consent request
        /// </summary>
        Task RejectConsentRequest(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a consent
        /// </summary>
        Task Revoke(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active and historical consents for a party
        /// </summary>
        Task<List<Consent>> GetAllConsents(Guid partyUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the consent consenxt
        /// </summary>
        Task<ConsentContext> GetConsentContext(Guid consentRequestId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list over consent request for a specific party.
        /// </summary>
        Task<Result<List<ConsentRequestDetails>>> GetRequestsForParty(Guid consentParty, CancellationToken cancellationToken);
    }
}
