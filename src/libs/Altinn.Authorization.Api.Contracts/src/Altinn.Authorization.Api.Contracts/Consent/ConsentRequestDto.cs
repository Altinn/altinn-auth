namespace Altinn.Authorization.Api.Contracts.Consent
{
    /// <summary>
    /// Represents a consent request.
    /// </summary>
    public class ConsentRequestDto
    {
        /// <summary>
        /// The unique identifier for the consent request. Need to be unique across all consent requests.
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Defines the party to request consent from.
        /// </summary>
        public required ConsentPartyUrn From { get; set; }

        /// <summary>
        /// Defines the party to request consent from.
        /// </summary>
        public ConsentPartyUrn? RequiredDelegator { get; set; }

        /// <summary>
        /// Defines the party requesting consent.
        /// </summary>
        public required ConsentPartyUrn To { get; set; }

        /// <summary>
        /// Defines how long the concent is valid
        /// </summary>
        public required DateTimeOffset ValidTo { get; set; }

        /// <summary>
        /// The consented rights.
        /// </summary>
        public required List<ConsentRightDto> ConsentRights { get; set; }

        /// <summary>
        /// The request message
        /// </summary>
        public Dictionary<string, string>? RequestMessage { get; set; }

        /// <summary>
        /// Redirect url for the user to be redirected after consent is given or denied.
        /// </summary>
        public required string RedirectUrl { get; set; } = string.Empty;
    }
}
