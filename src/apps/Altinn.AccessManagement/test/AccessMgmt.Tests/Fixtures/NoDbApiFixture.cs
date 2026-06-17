using Altinn.AccessManagement.TestUtils.Fixtures;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// <see cref="AccessMgmtApiFixture"/> for tests that mock the entire data layer
/// and never touch Postgres. It skips the per-test database clone and the DB
/// connection (<see cref="ApiFixture.ProvisionsDatabase"/> is <c>false</c>), so
/// such tests pay only for the host build.
/// </summary>
/// <remarks>
/// A class using this fixture must register mocks for every data-layer
/// dependency its controllers resolve (e.g. <c>IDelegationMetadataRepository</c>);
/// otherwise the real repository will try to use the unprovisioned database.
/// </remarks>
public class NoDbApiFixture : AccessMgmtApiFixture
{
    /// <inheritdoc/>
    protected override bool ProvisionsDatabase => false;
}
