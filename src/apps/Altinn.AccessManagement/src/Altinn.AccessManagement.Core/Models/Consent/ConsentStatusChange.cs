namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// DTO representing a consent status change
    /// </summary>
    public class ConsentStatusChange
    {
        /// <summary>
        /// The consent request ID
        /// </summary>
        public required Guid ConsentRequestId { get; set; }

        /// <summary>
        /// The event type representing the status change (e.g., "created", "approved", "rejected", "revoked")
        /// </summary>
        public required ConsentRequestEventType EventType { get; set; }

        /// <summary>
        /// When the status change occurred
        /// </summary>
        public required DateTimeOffset ChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the consent event.
        /// </summary>
        public required Guid ConsentEventId { get; set; }
    }
}
