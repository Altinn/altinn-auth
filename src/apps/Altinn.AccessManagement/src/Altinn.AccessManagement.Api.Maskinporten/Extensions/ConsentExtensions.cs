using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Api.Maskinporten.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentRequestDetails to ConsentRequestDetailsBFF.
    /// </summary>
    public static class ConsentExtensions
    {
        /// <summary>
        /// Converts a Consent object to a Consent object.
        /// </summary>
        /// <param name="consent">The Consent object to convert.</param>
        /// <returns>A ConsentRequestDetailsBFF object.</returns>
        /// <exception cref="ArgumentException">Thrown when an unknown consent party URN is encountered.</exception>
        public static ConsentInfoMaskinportenDto ToConsentInfoMaskinporten(this Consent consent)
        {
            Authorization.Api.Contracts.Consent.ConsentPartyUrn to = Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(consent.To.ValueSpan));

            Authorization.Api.Contracts.Consent.ConsentPartyUrn from = consent.From switch
            {
                Core.Models.Consent.ConsentPartyUrn.PersonId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse(consent.From.ValueSpan)),
                Core.Models.Consent.ConsentPartyUrn.OrganizationId => Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(consent.From.ValueSpan)),
                _ => throw new ArgumentException("Unknown consent party urn")
            };

            return new ConsentInfoMaskinportenDto
            {
                Id = consent.Id,
                From = from,
                To = to,
                Consented = consent.Consented,
                ValidTo = consent.ValidTo,
                ConsentRights = [.. consent.ConsentRights.Select(static x => x.ToConsentRightExternal())]
            };
        }
    }
}
