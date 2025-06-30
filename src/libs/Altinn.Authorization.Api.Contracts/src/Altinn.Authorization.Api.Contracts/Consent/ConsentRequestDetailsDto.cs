namespace Altinn.Authorization.Api.Contracts.Consent
{
    /// <summary>
    /// Represents a consent request.
    /// </summary>
    public class ConsentRequestDetailsDto
    {
        /// <summary>
        /// The id of the consent request.
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Defines the party to request consent from.
        /// </summary>
        public required ConsentPartyUrn From { get; set; }

        /// <summary>
        /// Defines the party requesting consent.
        /// </summary>
        public required ConsentPartyUrn To { get; set; }

        /// <summary>
        /// The required party that need to accept the consent request.
        /// </summary>
        public ConsentPartyUrn? RequiredDelegator { get; set; }

        /// <summary>
        /// The handled by party that handles the consent request on behalf of the requesting party.
        /// </summary>
        public ConsentPartyUrn? HandledBy { get; set; }

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
        /// Defines when the consent was given.
        /// </summary>
        public DateTimeOffset? Consented { get; set; }

        /// <summary>
        /// Redirect url for the user to be redirected after consent is given or denied.
        /// </summary>
        public required string RedirectUrl { get; set; } = string.Empty;

        /// <summary>
        /// List all events related to consent request
        /// </summary>
        public required List<ConsentRequestEventDto> ConsentRequestEvents { get; set; }

        /// <summary>
        /// The URI for the view that should be shown to the user when requesting consent.
        /// </summary>
        public string ViewUri { get; set; } = string.Empty;
    }
}
