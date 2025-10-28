using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Microsoft.EntityFrameworkCore;

namespace AccessMgmt.Tests.Services;

public class ConnectionQueryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ConnectionQueryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(GetFilterCombinations))]
    public async Task GetConnectionsAsync_ShouldNotThrow(bool[] flags, bool useSingle)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.SharedDb.Admin.ToString())
            .Options;

        using var db = new AppDbContext(options);
        var query = new ConnectionQuery(db);

        var filter = new ConnectionQueryFilter
        {
            FromIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            ToIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            RoleIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            PackageIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            ResourceIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            IncludeDelegation = flags[0],
            IncludeKeyRole = flags[1],
            IncludePackages = flags[2],
            IncludeResource = flags[3],
            EnrichEntities = flags[4],
            EnrichPackageResources = flags[5],
            ExcludeDeleted = flags[6],
            OnlyUniqueResults = flags[7]
        };

        await query.GetConnectionsAsync(filter);
    }

    public static IEnumerable<object[]> GetFilterCombinations()
    {
        var combinations = new List<object[]>();
        var total = 1 << 8;

        foreach (var useSingle in new[] { true, false })
        {
            for (int i = 0; i < total; i++)
            {
                var flags = new bool[8];
                for (int j = 0; j < 8; j++)
                {
                    flags[j] = (i & (1 << j)) != 0;
                }

                combinations.Add(new object[] { flags, useSingle });
            }
        }

        return combinations;
    }
}
