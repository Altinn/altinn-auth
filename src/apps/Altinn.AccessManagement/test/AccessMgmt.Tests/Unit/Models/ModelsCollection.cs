namespace Altinn.AccessManagement.Tests.Unit.Models;

/// <summary>
/// Names the collection that groups the model unit tests so they run sequentially
/// rather than in parallel. The collection carries no shared fixture; it only
/// serializes the member classes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ModelsCollection
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "Models Test";
}
