namespace Altinn.Authorization.Integration.Register.Models;

/// <summary>
/// A stream of all <typeparamref name="T"/> items in a data source.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Links">Pagination links.</param>
/// <param name="Stats">Stream statistics.</param>
/// <param name="Items">The items.</param>
public record ItemStream<T>(
    PaginatedLinks Links,
    ItemStreamStats Stats,
    IEnumerable<T> Items)
    : Paginated<T>(Links, Items);

/// <summary>
/// Item stream statistics.
/// </summary>
/// <param name="PageStart">The first item on the page.</param>
/// <param name="PageEnd">The last item on the page.</param>
/// <param name="SequenceMax">The highest item in the database.</param>
public record ItemStreamStats(
    ulong PageStart,
    ulong PageEnd,
    ulong SequenceMax);
