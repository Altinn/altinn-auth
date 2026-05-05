using System.Threading.Tasks;
using Altinn.Platform.Authorization.IntegrationTests.Fixtures;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Altinn.Platform.Authorization.IntegrationTests.Repositories;

/// <summary>
/// End-to-end checks for <see cref="DelegationMetadataRepository"/> against a
/// real PostgreSQL instance. Pinned to the <see cref="DelegationChangeType"/>
/// round-trip because the data-source-level <c>MapEnum</c> registration in
/// <c>Program.cs</c> is the only place where a typo or missing call would
/// silently break inserts/reads, and that codepath is bypassed by the
/// in-process test fixture which substitutes a mock repository.
/// </summary>
public class DelegationMetadataRepositoryIntegrationTests : IClassFixture<AuthorizationDbFixture>
{
    private readonly AuthorizationDbFixture _db;

    public DelegationMetadataRepositoryIntegrationTests(AuthorizationDbFixture db)
    {
        _db = db;
    }

    [Theory]
    [InlineData(DelegationChangeType.Grant)]
    [InlineData(DelegationChangeType.Revoke)]
    [InlineData(DelegationChangeType.RevokeLast)]
    public async Task InsertDelegation_then_GetCurrentDelegationChange_round_trips_DelegationChangeType(DelegationChangeType changeType)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_db.ApplicationConnectionString);
        dataSourceBuilder.MapEnum<DelegationChangeType>("delegation.delegationchangetype");
        await using var dataSource = dataSourceBuilder.Build();

        var repository = new DelegationMetadataRepository(dataSource, NullLogger<DelegationMetadataRepository>.Instance);

        // Distinct app/party tuple per case so each row is the "current" one for its lookup.
        var altinnAppId = $"test-org/test-app-{(int)changeType}";
        var offeredByPartyId = 50000 + (int)changeType;
        var coveredByUserId = 60000 + (int)changeType;

        var insert = new DelegationChange
        {
            DelegationChangeType = changeType,
            AltinnAppId = altinnAppId,
            OfferedByPartyId = offeredByPartyId,
            CoveredByUserId = coveredByUserId,
            CoveredByPartyId = null,
            PerformedByUserId = 70000,
            BlobStoragePolicyPath = $"policies/{altinnAppId}.xml",
            BlobStorageVersionId = "v1",
        };

        var inserted = await repository.InsertDelegation(insert);
        Assert.NotNull(inserted);
        Assert.Equal(changeType, inserted.DelegationChangeType);

        var current = await repository.GetCurrentDelegationChange(altinnAppId, offeredByPartyId, null, coveredByUserId);
        Assert.NotNull(current);
        Assert.Equal(changeType, current.DelegationChangeType);
    }
}
