namespace Altinn.AccessManagement.Tests.Unit;

/// <summary>
/// Names the collection for the policy administration point unit tests so they run
/// sequentially rather than in parallel. The collection carries no shared fixture;
/// it only serializes the member class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PolicyAdministrationPointCollection
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "PolicyAdministrationPointTest";
}
