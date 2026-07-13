namespace Altinn.Authorization.Tests;

/// <summary>
/// Names the collection that groups the policy tests so they run sequentially
/// rather than in parallel. The collection carries no shared fixture; it only
/// serializes the member classes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PolicyTestCollection
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "Policy tests";
}
