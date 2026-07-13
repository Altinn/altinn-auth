namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Names the collection for the resource controller tests so they run sequentially
/// rather than in parallel. The collection carries no shared fixture; it only
/// serializes the member class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ResourceControllerCollection
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "ResourceController Tests";
}
