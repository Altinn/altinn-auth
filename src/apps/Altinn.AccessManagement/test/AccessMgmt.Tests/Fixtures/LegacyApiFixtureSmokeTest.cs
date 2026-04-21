using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// Smoke test verifying that <see cref="LegacyApiFixture"/> provisions the
/// full production database schema — both the EF <c>dbo</c> schemas and the
/// Yuniql <c>accessmanagement.*</c> / <c>consent.*</c> / <c>delegation.*</c>
/// schemas (plus enum types) — so tests that rely on the still-extant Dapper
/// repositories can run against it.
/// </summary>
/// <remarks>
/// This is the scope-boundary verification for sub-step 16.4-prep: a single
/// tiny test exercising a Dapper-backed repository
/// (<c>ResourceMetadataRepo</c>) that would otherwise fail with
/// <c>relation "accessmanagement.resource" does not exist</c> under a plain
/// <see cref="Altinn.AccessManagement.TestUtils.Fixtures.ApiFixture"/>.
/// Migration of the six remaining <c>WebApplicationFixture</c> consumers
/// is tracked as follow-up sub-steps 16.4a/b.
/// </remarks>
public class LegacyApiFixtureSmokeTest(LegacyApiFixture fixture) : IClassFixture<LegacyApiFixture>
{
    private LegacyApiFixture Fixture { get; } = fixture;

    [Fact]
    public async Task InsertAccessManagementResource_Succeeds_WhenYuniqlSchemaIsProvisioned()
    {
        // Ensure the host has been built so DI + Yuniql migrations ran.
        _ = Fixture.BuildConfiguration();

        using var scope = Fixture.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IResourceMetadataRepository>();

        var resource = new AccessManagementResource
        {
            ResourceRegistryId = $"legacy_smoke_{Guid.NewGuid():N}",
            ResourceType = ResourceType.AltinnApp,
        };

        var inserted = await repo.InsertAccessManagementResource(resource, TestContext.Current.CancellationToken);

        // A successful round-trip proves the Yuniql-provisioned
        // accessmanagement.resource relation exists in the per-test database.
        // (Note: the repo lowercases ResourceType on write and Enum.TryParse on
        // read is case-sensitive, so the returned ResourceType is Default —
        // that quirk is unrelated to the fixture plumbing under test.)
        Assert.NotNull(inserted);
        Assert.Equal(resource.ResourceRegistryId, inserted.ResourceRegistryId);
        Assert.NotEqual(0, inserted.ResourceId);
    }
}
