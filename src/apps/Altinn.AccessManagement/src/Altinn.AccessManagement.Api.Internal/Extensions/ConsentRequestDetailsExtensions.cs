using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentRequestDetails to ConsentRequestDetailsBFF.
    /// </summary>
    public static class ConsentRequestDetailsExtensions
    {
        /// <summary>
        /// Converts a ConsentRequestDetails object to a ConsentRequestDetailsBFF object.
        /// </summary>
        /// <param name="details">The ConsentRequestDetails object to convert.</param>
        /// <returns>A ConsentRequestDetailsBFF object.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown consent party URN is encountered.</exception>
        public static ConsentRequestDetailsBffDto ToConsentRequestDetailsBFF(this ConsentRequestDetails details)
        {
            Authorization.Api.Contracts.Consent.ConsentPartyUrn to;
            if (details.To.IsPartyUuid(out Guid toPartyUuid))
            {
                to = Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(toPartyUuid);
            }
            else
            {
                throw new ArgumentException("Unknown consent party urn");
            }

            Authorization.Api.Contracts.Consent.ConsentPartyUrn from;
            if (details.From.IsPartyUuid(out Guid fromPartyUuid))
            {
                from = Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(fromPartyUuid);
            }
            else
            {
                throw new ArgumentException("Unknown consent party urn");
            }

            Authorization.Api.Contracts.Consent.ConsentPartyUrn? requiredDelegator = null;
            if (details.RequiredDelegator != null && details.RequiredDelegator.IsPartyUuid(out Guid delegatorUuid))
            {
                requiredDelegator = Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(delegatorUuid);
            }

            Authorization.Api.Contracts.Consent.ConsentPartyUrn? handledBy = null;
            if (details.HandledBy != null && details.HandledBy.IsPartyUuid(out Guid handledByUuid))
            {
                handledBy = Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(handledByUuid);
            }

            return new ConsentRequestDetailsBffDto
            {
                Id = details.Id,
                From = from,
                To = to,
                RequiredDelegator = requiredDelegator,
                HandledBy = handledBy,
                Consented = details.Consented,
                ValidTo = details.ValidTo,
                ConsentRights = details.ConsentRights.Select(static x => x.ToConsentRightExternal()).ToList(),
                ConsentRequestEvents = [.. details.ConsentRequestEvents.Select(static x => x.ToConsentRequestEventExternal())],
                RedirectUrl = details.RedirectUrl,
                ViewUri = details.ViewUri,
                TemplateId = details.TemplateId,
                TemplateVersion = details.TemplateVersion,
                Requestmessage = details.RequestMessage
            };
        }
    }
}
