using Altinn.AccessManagement.Tests.Fixtures;

namespace Altinn.AccessManagement.Tests.Integration.Controllers;

/// <summary>
/// Shares a single <see cref="ConsentApiFixture"/> — one test host plus one
/// Yuniql-provisioned database — across the consent controller test classes that
/// use <see cref="Xunit.IClassFixture{TFixture}"/>, instead of each building its
/// own host.
/// </summary>
/// <remarks>
/// Members must be safe to share: their inserted consent-request IDs are disjoint
/// (Enterprise uses random <c>Guid.CreateVersion7()</c>, Maskinporten uses fixed
/// distinct GUIDs) and the party mock is Setup-only. Consent classes that build a
/// fresh fixture per test for isolation (BFF, FetchStatusChanges) stay as they are.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ConsentDbCollection : ICollectionFixture<ConsentApiFixture>
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "Consent DB";
}
