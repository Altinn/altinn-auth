using Altinn.AccessManagement.Api.Enterprise.Extensions;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Api.Enterprise.Utils
{
    /// <summary>
    /// Mapper for converting between external and internal models
    /// </summary>
    public static class ModelMapper
    {
        /// <summary>
        /// Converts external consent request to internal model
        /// </summary>
        public static ConsentRequest ToCore(ConsentRequestDto consentRequestExternal)
        {
            return new ConsentRequest
            {
                Id = consentRequestExternal.Id,
                From = ToCore(consentRequestExternal.From),
                To = ToCore(consentRequestExternal.To),
                RequiredDelegator = consentRequestExternal.RequiredDelegator != null
                    ? ToCore(consentRequestExternal.RequiredDelegator)
                    : null,
                ValidTo = consentRequestExternal.ValidTo,
                ConsentRights = consentRequestExternal.ConsentRights.Select(static x => x.ToConsentRight()).ToList(),
                RequestMessage = consentRequestExternal.RequestMessage,
                RedirectUrl = consentRequestExternal.RedirectUrl
            };
        }

        private static Core.Models.Consent.ConsentPartyUrn ToCore(Authorization.Api.Contracts.Consent.ConsentPartyUrn consentPartyUrnExternal)
        {
            return consentPartyUrnExternal switch
            {
                _ when consentPartyUrnExternal.IsOrganizationId(out OrganizationNumber? organizationNumber) =>
                    Core.Models.Consent.ConsentPartyUrn.OrganizationId.Create(organizationNumber),
                _ when consentPartyUrnExternal.IsPersonId(out PersonIdentifier? personIdentifier) =>
                    Core.Models.Consent.ConsentPartyUrn.PersonId.Create(personIdentifier),
                _ when consentPartyUrnExternal.IsPartyUuid(out Guid partyUuid) =>
                    Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(partyUuid),
                _ => throw new ArgumentException("Unknown consent party urn")
            };
        }
    }
}
