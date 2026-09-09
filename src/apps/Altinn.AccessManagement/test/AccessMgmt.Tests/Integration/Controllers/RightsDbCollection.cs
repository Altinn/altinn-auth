using Altinn.AccessManagement.Tests.Fixtures;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Shares a single <see cref="RightsApiFixture"/>, one test host plus one seeded
/// database, across the controller tests that mock the policy / delegation data
/// layer, instead of each building its own host.
/// </summary>
/// <remarks>
/// Members are safe to share: AppsInstanceDelegation seeds additively under its own
/// IDs via <c>EnsureSeedOnce</c>, so there is no cross-class collision.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class RightsDbCollection : ICollectionFixture<RightsApiFixture>
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "Rights DB";
}
