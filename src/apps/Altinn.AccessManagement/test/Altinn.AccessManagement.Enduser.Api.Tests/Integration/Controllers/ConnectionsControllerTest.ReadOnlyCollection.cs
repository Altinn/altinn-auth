using Altinn.AccessManagement.TestUtils.Fixtures;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Shares a single <see cref="ApiFixture"/> — one test host plus one seeded
/// database — across the read-only <c>ConnectionsController</c> test classes,
/// instead of each building its own host (the dominant integration-test setup
/// cost).
/// </summary>
/// <remarks>
/// Members must be safe to share: additive seeding only (each keys its
/// <c>EnsureSeedOnce&lt;T&gt;</c> on its own type), no per-class
/// <c>ConfigureServices</c> / configuration overrides, and no mutation of rows
/// other classes read. Classes that need isolation stay on
/// <see cref="Xunit.IClassFixture{TFixture}"/>.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ConnectionsReadOnlyCollection : ICollectionFixture<ApiFixture>
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "ConnectionsController read-only";
}
