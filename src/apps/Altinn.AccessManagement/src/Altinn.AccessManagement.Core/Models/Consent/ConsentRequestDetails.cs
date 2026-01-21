namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents a consent request.
    /// </summary>
    public class ConsentRequestDetails
    {
        /// <summary>
        /// Defines the ID for the consent request.Created by Altinn
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Defines the party that has to consent to the consentRequest
        /// </summary>
        public required ConsentPartyUrn From { get; set; }

        /// <summary>
        /// Defines the party that is required to accept the consent request. 
        /// </summary>
        public ConsentPartyUrn RequiredDelegator { get; set; }

        /// <summary>
        /// Defines the party requesting consent.
        /// </summary>
        public required ConsentPartyUrn To { get; set; }

        /// <summary>
        /// Defines the party that handles the consent request on behalf of the requesting party.
        /// </summary>
        public ConsentPartyUrn HandledBy { get; set; }

        /// <summary>
        /// Defines how long the concent is valid
        /// </summary>
        public required DateTimeOffset ValidTo { get; set; }

        /// <summary>
        /// The consented rights.
        /// </summary>
        public required List<ConsentRight> ConsentRights { get; set; }

        /// <summary>
        /// The request message
        /// </summary>
        public Dictionary<string, string> RequestMessage { get; set; }

        /// <summary>
        /// The status of the consent request
        /// </summary>
        public ConsentRequestStatusType ConsentRequestStatus { get; set; }

        /// <summary>
        /// Defines when the consent was given.
        /// </summary>
        public DateTimeOffset? Consented { get; set; }

        /// <summary>
        /// Defines when the consent was revoked.
        /// </summary>
        public required List<ConsentRequestEvent> ConsentRequestEvents { get; set; }

        /// <summary>
        /// Redirect url for the user to be redirected after consent is given or denied.
        /// </summary>
        public required string RedirectUrl { get; set; } = string.Empty;

        /// <summary>
        /// The URI for the view that should be shown to the user when requesting consent.
        /// </summary>
        public string ViewUri { get; set; } = string.Empty;

        /// <summary>
        /// The consent template id.
        /// </summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// The version of the consent template.
        /// </summary>
        public int? TemplateVersion { get; set; }

        /// <summary>
        /// Defines the view mode for the consent portal.
        /// </summary>
        public ConsentPortalViewMode PortalViewMode { get; set; }
    }
}
