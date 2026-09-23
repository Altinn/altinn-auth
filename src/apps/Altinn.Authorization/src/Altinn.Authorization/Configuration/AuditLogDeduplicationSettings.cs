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
        /// Upper bound on the number of entries remembered per generation, per instance. An event takes one
        /// entry for its trace and, the first time it is seen, one for itself. Two generations are kept,
        /// each allocated for the maximum on first use, so memory is fixed at about 2 × 28 bytes × this
        /// value (5.6 MB for the default). When a generation is full, a new one is started early and the
        /// oldest events are forgotten before their window ends, counted in
        /// <c>altinn.pdp.auditlog.tracker.capacity_rotations</c>.
        /// </summary>
        public int MaxTrackedEvents { get; set; } = 100_000;
    }
}
