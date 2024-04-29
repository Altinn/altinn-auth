namespace Altinn.ResourceRegistry.Core.Aggregates;

/// <summary>
/// A base class for aggregates.
/// </summary>
public interface IAggregate
{
    /// <summary>
    /// Gets the aggregate id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets a value indicating wheather the aggregate is initialized.
    /// </summary>
    public bool IsInitialized { get; }

    /// <summary>
    /// Gets a value indicating wheather the aggregate is deleted.
    /// </summary>
    public bool IsDeleted { get; }

    /// <summary>
    /// Gets when this aggregate was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets when this aggregate was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; }

    /// <summary>
    /// Gets the current aggregate version (for use with optimistic concurency).
    /// </summary>
    public EventId CommittedVersion { get; }

    /// <summary>
    /// Save changes to the aggregate.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    public Task SaveChanges(CancellationToken cancellationToken = default);
}
