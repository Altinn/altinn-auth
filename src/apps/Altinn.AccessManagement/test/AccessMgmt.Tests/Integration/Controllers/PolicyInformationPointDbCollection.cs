using Altinn.AccessManagement.Tests.Fixtures;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Shares a single <see cref="AccessMgmtApiFixture"/> — one test host plus one
/// seeded database — across the PolicyInformationPoint DB-integration test classes,
/// instead of each building its own host (the dominant integration-test setup cost).
/// </summary>
/// <remarks>
/// Members seed additively (each keys its <c>EnsureSeedOnce&lt;T&gt;</c> on its own
/// type) under disjoint IDs and assert only against their own entities, so they
/// tolerate the shared host and database. No per-class <c>ConfigureServices</c>.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class PolicyInformationPointDbCollection : ICollectionFixture<AccessMgmtApiFixture>
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "PolicyInformationPoint DB";
}
