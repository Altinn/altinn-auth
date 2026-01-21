namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents a consent request.
    /// </summary>
    public class ConsentRequest
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
        /// Defines if a specific person to consent is required. This is used in cases where a specific person must consent and the right cant be delegated
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
        /// The consent template id.
        /// </summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// The version of the consent template.
        /// </summary>
        public int? TemplateVersion { get; set; }

        /// <summary>
        /// Redirect url for the user to be redirected after consent is given or denied.
        /// </summary>
        public required string RedirectUrl { get; set; } = string.Empty;

        /// <summary>
        /// Defines the portal view mode for the consent request. Hide is default
        /// </summary>
        public ConsentPortalViewMode PortalViewMode { get; set; } = ConsentPortalViewMode.Hide;
    }
}
