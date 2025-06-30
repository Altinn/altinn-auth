namespace Altinn.Authorization.Api.Contracts.Consent
{
    /// <summary>
    /// Model for looking up a consent.
    /// </summary>
    public class ConsentLookupDto
    {
        /// <summary>
        /// Defines the Id of the consent.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Defines the party that has approved a consent request.
        /// </summary>
        public required ConsentPartyUrn From { get; set; }

        /// <summary>
        /// Defines the party receiving consent.
        /// </summary>
        public required ConsentPartyUrn To { get; set; }
    }
}
