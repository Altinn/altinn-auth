namespace Altinn.Platform.Authorization.Models.EventLog
{
    /// <summary>
    /// Whether an authorization event repeats one already seen within the deduplication window.
    /// </summary>
    public enum AuthorizationEventDuplicateKind
    {
        /// <summary>
        /// First occurrence of the event within the window.
        /// </summary>
        None,

        /// <summary>
        /// The same event has already been seen in the same trace, i.e. the caller asked the same
        /// question more than once while handling a single request.
        /// </summary>
        SameTrace,

        /// <summary>
        /// The same event has already been seen within the window, but in another trace.
        /// </summary>
        Window,
    }
}
