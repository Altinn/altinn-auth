#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Result for <see cref="IAccessListsRepository.LoadOrCreateAccessList(string, string, string, string, CancellationToken)"/>
/// </summary>
/// <param name="Mode">The result mode.</param>
/// <param name="Aggregate">The <see cref="IAccessListAggregate"/>.</param>
public record AccessListLoadOrCreateResult(
    AccessListLoadOrCreateResult.ResultMode Mode,
    IAccessListAggregate Aggregate)
{
    /// <summary>
    /// Gets a value indicating wheather the returned aggregate was newly created or not.
    /// </summary>
    public bool IsNew => Mode == ResultMode.Created;

    /// <summary>
    /// Enum that describes the result of a load or create operation.
    /// </summary>
    public enum ResultMode
    {
        Created,
        Loaded,
    }
}
