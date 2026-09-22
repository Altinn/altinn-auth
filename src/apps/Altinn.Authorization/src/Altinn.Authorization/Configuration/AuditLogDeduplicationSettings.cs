namespace Altinn.Platform.Authorization.Configuration
{
    /// <summary>
    /// Settings for detecting repeated authorization events before they are queued for the audit log.
    /// </summary>
    public class AuditLogDeduplicationSettings
    {
        /// <summary>
        /// How long an authorization event is remembered. A repeat of the same event within this
        /// window is classified as a duplicate.
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Upper bound on the number of events remembered per window, per instance. Keeps memory use
        /// bounded (roughly 40 bytes per entry, two windows at most) under traffic spikes. Events that
        /// arrive when the limit is reached are not remembered and are classified as untracked.
        /// </summary>
        public int MaxTrackedEvents { get; set; } = 100_000;
    }
}
