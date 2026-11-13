namespace Altinn.Platform.Authorization.Configuration
{
    /// <summary>
    /// Which duplicate authorization events are dropped instead of queued for the audit log, when
    /// <see cref="FeatureFlags.AuditLogDuplicateSuppression"/> is on.
    /// </summary>
    public enum AuditLogSuppressScope
    {
        /// <summary>
        /// Only repeats within the same trace: the same question asked more than once while handling a
        /// single request.
        /// </summary>
        Trace,

        /// <summary>
        /// Repeats within the same trace, and repeats from other traces within
        /// <see cref="AuditLogDeduplicationSettings.Window"/>.
        /// </summary>
        Window,
    }
}
