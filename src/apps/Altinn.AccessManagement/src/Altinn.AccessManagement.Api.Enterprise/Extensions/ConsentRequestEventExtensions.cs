using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Api.Enterprise.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentRequestDetails to ConsentRequestDetailsBFF.
    /// </summary>
    public static class ConsentRequestEventExtensions   
    {
        /// <summary>
        /// Converts a ConsentRequestEvent object to a ConsentRequestEventExternal object.
        /// </summary>
        /// <param name="core">The ConsentRequestEvent object to convert.</param>
        /// <returns>A ConsentRequestDetailsBFF object.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown consent party URN is encountered.</exception>
        public static ConsentRequestEventDto ToConsentRequestEventExternal(this ConsentRequestEvent core)
        {
            Authorization.Api.Contracts.Consent.ConsentPartyUrn toExternal;

            if (core.PerformedBy.IsOrganizationId(out OrganizationNumber? organizationNumberTo))
            {
                toExternal = Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(organizationNumberTo);
            }
            else if (core.PerformedBy.IsPersonId(out PersonIdentifier? personIdentifier))
            {
                toExternal = Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(personIdentifier);
            }
            else if (core.PerformedBy.IsPartyUuid(out Guid partyUuid))
            {
                toExternal = Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(partyUuid);
            }
            else
            {
                throw new ArgumentException("Unknown consent party urn");
            }

            return new ConsentRequestEventDto
            {
                ConsentEventID = core.ConsentEventID,
                Created = core.Created,
                PerformedBy = toExternal,
                EventType = (Authorization.Api.Contracts.Consent.ConsentRequestEventType)core.EventType,
                ConsentRequestID = core.ConsentRequestID
            };
        }
    }
}
