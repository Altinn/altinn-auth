using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Api.Enterprise.Extensions
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
        public static ConsentRequestDetailsDto ToConsentRequestDetailsExternal(this ConsentRequestDetails details)
        {
            Authorization.Api.Contracts.Consent.ConsentPartyUrn to = Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(details.To.ValueSpan));

            Authorization.Api.Contracts.Consent.ConsentPartyUrn from = details.From switch
            {
                Core.Models.Consent.ConsentPartyUrn.PersonId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse(details.From.ValueSpan)),
                Core.Models.Consent.ConsentPartyUrn.OrganizationId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(details.From.ValueSpan)),
                _ => throw new ArgumentException("Unknown consent party urn")
            };

            Authorization.Api.Contracts.Consent.ConsentPortalViewMode portalViewMode = details.PortalViewMode switch
            {
                Core.Models.Consent.ConsentPortalViewMode.Hide => Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Hide,
                Core.Models.Consent.ConsentPortalViewMode.Show => Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Show,
                _ => throw new ArgumentException("Unknown consent portal view mode")
            };

            return new ConsentRequestDetailsDto
            {
                Id = details.Id,
                From = from,
                To = to,
                RequiredDelegator = details.RequiredDelegator != null
                    ? details.RequiredDelegator switch
                    {
                        Core.Models.Consent.ConsentPartyUrn.PersonId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse(details.RequiredDelegator.ValueSpan)),
                        Core.Models.Consent.ConsentPartyUrn.OrganizationId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(details.RequiredDelegator.ValueSpan)),
                        _ => throw new ArgumentException("Unknown consent party urn")
                    }
                    : null,
                HandledBy = details.HandledBy != null
                    ? details.HandledBy switch
                    {
                        Core.Models.Consent.ConsentPartyUrn.PersonId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse(details.HandledBy.ValueSpan)),
                        Core.Models.Consent.ConsentPartyUrn.OrganizationId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(details.HandledBy.ValueSpan)),
                        _ => throw new ArgumentException("Unknown consent party urn")
                    }
                    : null,
                Status = (Authorization.Api.Contracts.Consent.ConsentRequestStatusType)details.ConsentRequestStatus,
                Consented = details.Consented,
                ValidTo = details.ValidTo,
                ConsentRights = [.. details.ConsentRights.Select(static x => x.ToConsentRightExternal())],
                ConsentRequestEvents = [.. details.ConsentRequestEvents.Select(static x => x.ToConsentRequestEventExternal())],
                RedirectUrl = details.RedirectUrl,
                ViewUri = details.ViewUri,
                PortalViewMode = portalViewMode,
                RequestMessage = details.RequestMessage != null
                    ? new Dictionary<string, string>(details.RequestMessage)
                    : null
            };
        }
    }
}
