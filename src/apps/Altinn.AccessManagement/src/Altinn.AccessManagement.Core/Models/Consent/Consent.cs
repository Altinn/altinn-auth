namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Describes a concent
    /// </summary>
    public class Consent
    {
        /// <summary>
        /// The unique identifier for the consent.
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// The consent party 
        /// </summary>
        public required ConsentPartyUrn From { get; set; }

        /// <summary>
        /// Defines the party requesting consent.
        /// </summary>
        public required ConsentPartyUrn To { get; set; }

        /// <summary>
        /// Defines the party that handles the consent request on behalf of the requesting party.
        /// </summary>
        public ConsentPartyUrn HandledBy { get; set; }

        /// <summary>
        /// Defines when the consent was given.
        /// </summary>
        public DateTimeOffset Consented { get; set; }

        /// <summary>
        /// Defines how long the concent is valid
        /// </summary>
        public DateTimeOffset ValidTo { get; set; }

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
        /// The consent context
        /// </summary>
        public ConsentContext Context { get; set; }

        /// <summary>
        /// A list of all the consent events.
        /// </summary>
        public List<ConsentRequestEvent> ConsentRequestEvents { get; set; }
    }
}
