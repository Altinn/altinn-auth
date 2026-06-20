using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Shared <see cref="ApiFixture"/> for the read-only <c>RequestController</c>
/// test classes, with the RequestController feature flags enabled once at
/// construction — before the host is built. Member classes must not enable the
/// flags themselves: with a shared host, configuration set from a per-test-class
/// constructor runs after the host is built and is silently ignored.
/// </summary>
public sealed class RequestReadOnlyApiFixture : ApiFixture
{
    /// <summary>
    /// Initializes the fixture and enables the RequestController feature flags.
    /// </summary>
    public RequestReadOnlyApiFixture()
    {
        WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
        WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
    }
}

/// <summary>
/// Shares a single <see cref="RequestReadOnlyApiFixture"/> across the read-only
/// <c>RequestController</c> test classes. Members must be
/// additive-seed-only, must not call <c>ConfigureServices</c> / configuration
/// helpers, and must not write rows other members read. Mutating classes
/// (<c>Create*</c>, <c>Reject*</c>, <c>Withdraw*</c>, <c>Approve*</c>, …) stay
/// on <see cref="Xunit.IClassFixture{TFixture}"/>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class RequestReadOnlyCollection : ICollectionFixture<RequestReadOnlyApiFixture>
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "RequestController read-only";
}
