namespace Altinn.AccessManagement.Core.Models
{
    /// <summary>
    /// Represents the parameters used to query consent events for a specific party, with optional filters for consent
    /// request, event types, creation date range, and pagination.
    /// </summary>
    /// <param name="ConsentRequestId">The unique identifier of the consent request to filter events by. If null, events from all consent requests are
    /// included.</param>
    /// <param name="EventTypes">An array of event type names to filter the results. If null, events of all types are included.</param>
    /// <param name="CreatedAfter">The earliest creation date and time, in UTC, for events to include in the results. Only events created after
    /// this date are returned. If null, no lower bound is applied.</param>
    /// <param name="CreatedBefore">The latest creation date and time, in UTC, for events to include in the results. Only events created before this
    /// date are returned. If null, no upper bound is applied.</param>
    /// <param name="ContinueFrom">A pagination cursor id indicating the position in the result set from which to continue retrieving events. If null,
    /// retrieval starts from the beginning.</param>
    public record ConsentEventsQuery(
        Guid? ConsentRequestId,
        string[]? EventTypes,
        DateTimeOffset? CreatedAfter,
        DateTimeOffset? CreatedBefore,
        Guid? ContinueFrom);
}
