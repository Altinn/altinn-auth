using System.Text.Json;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace AccessMgmt.Tests.Services;

public class ConnectionQueryTests : IClassFixture<PostgresFixture>
{
    private readonly AppDbContext _db;
    private readonly ConnectionQuery _query;
    private EFTestData _data;

    public ConnectionQueryTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);
        _query = new ConnectionQuery(_db);

        _data = SeedTestData(_db).GetAwaiter().GetResult();
    }

    private async Task<EFTestData> SeedTestData(AppDbContext db)
    {
        var testData = new EFTestData();

        var baker = new Entity() { Id = Guid.NewGuid(), Name = "Baker", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-01", ParentId = null, RefId = "ORG-01" };
        var org01U1 = new Entity() { Id = Guid.NewGuid(), Name = baker.Name + " - Oslo", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = baker.OrganizationIdentifier + "01", ParentId = baker.Id, RefId = baker.OrganizationIdentifier + "01" };
        var org01U2 = new Entity() { Id = Guid.NewGuid(), Name = baker.Name + " - Bergen", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = baker.OrganizationIdentifier + "02", ParentId = baker.Id, RefId = baker.OrganizationIdentifier + "02" };
        var org01U3 = new Entity() { Id = Guid.NewGuid(), Name = baker.Name + " - Kristiansand", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = baker.OrganizationIdentifier + "03", ParentId = baker.Id, RefId = baker.OrganizationIdentifier + "03" };
        var bdo = new Entity() { Id = Guid.NewGuid(), Name = "BDO", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-02", ParentId = null, RefId = "ORG-02" };
        var org02U1 = new Entity() { Id = Guid.NewGuid(), Name = bdo.Name + " - Oslo", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = bdo.OrganizationIdentifier + "01", ParentId = bdo.Id, RefId = bdo.OrganizationIdentifier + "01" };
        var skrik = new Entity() { Id = Guid.NewGuid(), Name = "Skrik Frisør", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-03", ParentId = null, RefId = "ORG-03" };
        var org03U1 = new Entity() { Id = Guid.NewGuid(), Name = skrik.Name + " - Byporten", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = skrik.OrganizationIdentifier + "01", ParentId = skrik.Id, RefId = skrik.OrganizationIdentifier + "01" };
        var org03U2 = new Entity() { Id = Guid.NewGuid(), Name = skrik.Name + " - CC Vest", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = skrik.OrganizationIdentifier + "02", ParentId = skrik.Id, RefId = skrik.OrganizationIdentifier + "02" };
        var pwc = new Entity() { Id = Guid.NewGuid(), Name = "PwC", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-04", ParentId = null, RefId = "ORG-04" };
        var org04U1 = new Entity() { Id = Guid.NewGuid(), Name = pwc.Name + " - Stavanger", TypeId = EntityTypeConstants.Organisation, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = pwc.OrganizationIdentifier + "01", ParentId = pwc.Id, RefId = pwc.OrganizationIdentifier + "01" };
        var petter = new Entity() { Id = Guid.NewGuid(), Name = "Petter", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "01018412345", RefId = "01018412345", DateOfBirth = DateOnly.Parse("1984-01-01") };
        var gunnar = new Entity() { Id = Guid.NewGuid(), Name = "Gunnar", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "02018412345", RefId = "02018412345", DateOfBirth = DateOnly.Parse("1984-01-02") };
        var nina = new Entity() { Id = Guid.NewGuid(), Name = "Nina", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "03018412345", RefId = "03018412345", DateOfBirth = DateOnly.Parse("1984-01-03") };
        var kari = new Entity() { Id = Guid.NewGuid(), Name = "Kari", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "04018412345", RefId = "04018412345", DateOfBirth = DateOnly.Parse("1984-01-04") };
        var william = new Entity() { Id = Guid.NewGuid(), Name = "William", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "05018412345", RefId = "05018412345", DateOfBirth = DateOnly.Parse("1984-01-05") };
        var terje = new Entity() { Id = Guid.NewGuid(), Name = "Terje", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "06018412345", RefId = "06018412345", DateOfBirth = DateOnly.Parse("1984-01-06") };

        var bakerRegn = new Assignment() { FromId = baker.Id, ToId = bdo.Id, RoleId = RoleConstants.Accountant }; // Regnskapsfører
        var bdoDagl = new Assignment() { FromId = bdo.Id, ToId = petter.Id, RoleId = RoleConstants.ManagingDirector }; // Daglig leder
        var bdoAgent = new Assignment() { FromId = bdo.Id, ToId = gunnar.Id, RoleId = RoleConstants.Agent }; // Agent
        var skrikDagl = new Assignment() { FromId = skrik.Id, ToId = nina.Id, RoleId = RoleConstants.ManagingDirector }; // Daglig leder
        var skrikRight = new Assignment() { FromId = skrik.Id, ToId = nina.Id, RoleId = RoleConstants.Rightholder }; // Rettighetsholder
        var bdoRevi = new Assignment() { FromId = bdo.Id, ToId = pwc.Id, RoleId = RoleConstants.Auditor }; // Revisor 
        var pwcDagl = new Assignment() { FromId = pwc.Id, ToId = william.Id, RoleId = RoleConstants.ManagingDirector }; // Daglig leder
        var pwcStyr = new Assignment() { FromId = pwc.Id, ToId = terje.Id, RoleId = RoleConstants.ChairOfTheBoard }; // Styreleder

        var delegation01 = new Delegation() { FromId = bakerRegn.Id, ToId = bdoAgent.Id, FacilitatorId = bdo.Id };
        var delegation02 = new Delegation() { FromId = bakerRegn.Id, ToId = bdoRevi.Id, FacilitatorId = bdo.Id };

        testData.Entities.Add("baker", baker);
        testData.Entities.Add("org01U1", org01U1);
        testData.Entities.Add("org01U2", org01U2);
        testData.Entities.Add("org01U3", org01U3);
        testData.Entities.Add("bdo", bdo);
        testData.Entities.Add("org02U1", org02U1);
        testData.Entities.Add("skrik", skrik);
        testData.Entities.Add("org03U1", org03U1);
        testData.Entities.Add("org03U2", org03U2);
        testData.Entities.Add("pwc", pwc);
        testData.Entities.Add("org04U1", org04U1);
        testData.Entities.Add("petter", petter);
        testData.Entities.Add("gunnar", gunnar);
        testData.Entities.Add("nina", nina);
        testData.Entities.Add("kari", kari);
        testData.Entities.Add("william", william);
        testData.Entities.Add("terje", terje);

        testData.Assignments.Add("bakerRegn", bakerRegn);
        testData.Assignments.Add("bdoDagl", bdoDagl);
        testData.Assignments.Add("bdoAgent", bdoAgent);
        testData.Assignments.Add("skrikDagl", skrikDagl);
        testData.Assignments.Add("skrikRight", skrikRight);
        testData.Assignments.Add("bdoRevi", bdoRevi);
        testData.Assignments.Add("pwcDagl", pwcDagl);
        testData.Assignments.Add("pwcStyr", pwcStyr);

        testData.Delegations.Add("delegation01", delegation01);
        testData.Delegations.Add("delegation02", delegation02);

        db.Entities.AddRange(testData.Entities.Values);
        db.Assignments.AddRange(testData.Assignments.Values);
        db.Delegations.AddRange(testData.Delegations.Values);

        try
        {
            await db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));
        }
        catch 
        {
        }

        return testData;
    }

    [Fact]
    public async Task Petter()
    {
        // Daglig leder i BDO

        var orgId = _data.Entities["bdo"].Id;
        var personId = _data.Entities["petter"].Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            OnlyUniqueResults = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var resNew = await _query.GetConnectionsFromOthersAsync(filter, true);
        var dtosNew = DtoMapper.ConvertFromOthers(resNew, false);

        var resOld = await _query.GetConnectionsFromOthersAsync(filter, false);
        var dtosOld = DtoMapper.ConvertFromOthers(resOld, false);

        var (onlyInA, onlyInB) = ConnectionDiffHelper.Diff(resOld, resNew);

        if (onlyInA.Any())
        {
            var msg = "Rows missing from B:\n" + JsonSerializer.Serialize(onlyInA);
            Console.WriteLine(msg);
        }

        if (onlyInB.Any())
        {
            var msg = "Rows missing from A:\n" + JsonSerializer.Serialize(onlyInB);
            Console.WriteLine(msg);
        }

        Assert.Equal(resOld.Count, resNew.Count);
        Assert.Equal(dtosOld.Count, dtosNew.Count);
    }

    [Fact]
    public async Task Petter_ShouldGetConnection_To_Baker_Via_BDO_When_KeyRoleIsEnabled()
    {
        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { _data.Entities["baker"].Id },
            ToIds = new[] { _data.Entities["petter"].Id },
            IncludeKeyRole = true
        };

        var result = await _query.GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, true);

        Assert.NotNull(result);
        Assert.True(result.Any(), "Expected a connections, but none were found.");
    }

    [Fact]
    public async Task Petter_ShouldNotGetConnection_To_Baker_When_KeyRoleIsDisabled()
    {
        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { _data.Entities["baker"].Id },
            ToIds = new[] { _data.Entities["petter"].Id },
            IncludeKeyRole = false
        };

        var result = await _query.GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers);

        Assert.NotNull(result);
        Assert.False(result.Any(), "Expected no connections, but some were found.");
    }

    [Fact]
    public async Task Baker_ShouldExist()
    {
        var petter = await _db.Entities.AsNoTracking().SingleAsync(t => t.RefId == "ORG-01");

        Assert.NotNull(petter);
        Assert.Equal("Baker", petter.Name);
    }

    [Fact]
    public async Task Organization_ShouldExist()
    {
        var orgType = await _db.EntityTypes.AsNoTracking().SingleAsync(t => t.Id == EntityTypeConstants.Organisation);

        Assert.NotNull(orgType);
        Assert.Equal("Organisasjon", orgType.Name);
    }

    [Theory]
    [MemberData(nameof(GetFilterCombinations))]
    public async Task GetConnectionsAsync_ShouldNotThrow(bool[] flags, bool useSingle)
    {
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

        await _query.GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers);
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

internal class EFTestData
{
    internal Dictionary<string, Entity> Entities { get; } = new();

    internal Dictionary<string, Assignment> Assignments { get; } = new();

    internal Dictionary<string, Delegation> Delegations { get; } = new();
}
