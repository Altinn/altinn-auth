namespace Altinn.Authorization.Api.Contracts.Consent
{
    /// <summary>
    /// represents an event related to a consent request.
    /// </summary>
    public class ConsentRequestEventDto
    {
        /// <summary>
        /// The ID of the consent event that this event is related to.
        /// </summary>
        public Guid ConsentEventID { get; set; }

        /// <summary>
        /// When the event was created.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Who made the event happen
        /// </summary>
        public required ConsentPartyUrn PerformedBy { get; set; }

        /// <summary>
        /// The type of event that happened.
        /// </summary>
        public ConsentRequestEventType EventType { get; set; }

        /// <summary>
        /// The ID of the consent request that this event is related to.
        /// </summary>
        public Guid ConsentRequestID { get; set; }
    }
}
