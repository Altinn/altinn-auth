using System.Text.Json;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace AccessMgmt.Tests.Services;

public class ConnectionQueryTests : IClassFixture<PostgresFixture>
{
    private readonly AppDbContext _db;
    private readonly ConnectionQuery _query;

    public ConnectionQueryTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);
        _query = new ConnectionQuery(_db);

        SeedTestData(_db).GetAwaiter().GetResult();
    }

    private async Task SeedTestData(AppDbContext db)
    {
        db.Entities.AddRange(TestDataSet.Entities);
        db.Assignments.AddRange(TestDataSet.Assignments);
        db.Delegations.AddRange(TestDataSet.Delegations);

        try
        {
            await db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public async Task Petter()
    {
        var orgId = TestDataSet.GetEntity("Regnskaperne").Id;
        var personId = TestDataSet.GetEntity("Petter").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            OnlyUniqueResults = false,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, true);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);
        
        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);

        var baker = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Oslo").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Bergen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Kristiansand").Id);
    }

    [Fact]
    public async Task Gunnar()
    {
        var orgId = TestDataSet.GetEntity("Regnskaperne").Id;
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            OnlyUniqueResults = false,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, true);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);

        var baker = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Oslo").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Bergen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Kristiansand").Id);
    }

    [Fact]
    public async Task Nina()
    {
        var orgId = TestDataSet.GetEntity("Skrik Frisør").Id;
        var personId = TestDataSet.GetEntity("Nina").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            OnlyUniqueResults = false,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, true);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);
    }

    [Fact]
    public async Task William()
    {
        var orgId = TestDataSet.GetEntity("Revi").Id;
        var personId = TestDataSet.GetEntity("William").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            OnlyUniqueResults = false,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, true);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);

        var main = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);
        Assert.Contains(main.Connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne - Oslo").Id);
    }

    [Fact]
    public async Task Terje()
    {
        var orgId = TestDataSet.GetEntity("Revi").Id;
        var personId = TestDataSet.GetEntity("Terje").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            OnlyUniqueResults = false,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, true);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);

        var baker = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne - Oslo").Id);
    }

    [Fact]
    public async Task Petter_ShouldGetConnection_To_Baker_Via_BDO_When_KeyRoleIsEnabled()
    {
        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { TestDataSet.GetEntity("Baker Johnsen").Id },
            ToIds = new[] { TestDataSet.GetEntity("Petter").Id },
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
            FromIds = new[] { TestDataSet.GetEntity("Baker Johnsen").Id },
            ToIds = new[] { TestDataSet.GetEntity("Petter").Id },
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
        Assert.Equal("Baker Johnsen", petter.Name);
    }

    [Fact]
    public async Task Organization_ShouldExist()
    {
        var orgType = await _db.EntityTypes.AsNoTracking().SingleAsync(t => t.Id == EntityTypeConstants.Organization);

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

internal static class TestDataSet
{
    internal static Entity GetEntity(string name)
    {
        return Entities.First(x => x.Name == name);
    }

    #pragma warning disable IDE0028 // Simplify collection initialization
    #pragma warning disable SA1401 // Fields should be private
    internal static List<Entity> Entities = new()
    #pragma warning restore SA1401 // Fields should be private
    #pragma warning restore IDE0028 // Simplify collection initialization
    {
        new Entity() { Id = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), Name = "Baker Johnsen", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-01", ParentId = null, RefId = "ORG-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7006-8da4-55c17e07f4d6"), Name = "Baker Johnsen - Oslo", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-01-01", ParentId = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), RefId = "ORG-01-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7fb8-915b-c13b5c1c5264"), Name = "Baker Johnsen - Bergen", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-01-02", ParentId = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), RefId = "ORG-01-02" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-72da-afe2-daa4fe116965"), Name = "Baker Johnsen - Kristiansand", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-01-03", ParentId = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), RefId = "ORG-01-03" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-71fa-9a7e-624ca11ebaf7"), Name = "Regnskaperne", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-02", ParentId = null, RefId = "ORG-02" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7127-8101-095fd6ffe7bf"), Name = "Regnskaperne - Oslo", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-02-01", ParentId = Guid.Parse("0195efb8-7c80-71fa-9a7e-624ca11ebaf7"), RefId = "ORG-02-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7fac-9944-8a51f26c2633"), Name = "Skrik Frisør", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-03", ParentId = null, RefId = "ORG-03" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7225-bf3e-0aab8e9f59c3"), Name = "Skrik Frisør - Byporten", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-03-01", ParentId = Guid.Parse("0195efb8-7c80-7fac-9944-8a51f26c2633"), RefId = "ORG-03-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7b40-8415-0c0fe952dd2f"), Name = "Skrik Frisør - CC Vest", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-03-02", ParentId = Guid.Parse("0195efb8-7c80-7fac-9944-8a51f26c2633"), RefId = "ORG-03-02" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7c46-96b3-b2b1b0b51895"), Name = "Revi", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-04", ParentId = null, RefId = "ORG-04" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-713c-b504-84984779378c"), Name = "Revi - Stavanger", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-04-01", ParentId = Guid.Parse("0195efb8-7c80-7c46-96b3-b2b1b0b51895"), RefId = "ORG-04-01" },

        new Entity() { Id = Guid.Parse("0195efb8-7c80-7d22-b320-2eed10bc8a84"), Name = "Petter", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "01018412345", RefId = "01018412345", DateOfBirth = DateOnly.Parse("1984-01-01") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7414-87e1-3b3b9799161d"), Name = "Gunnar", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "02018412345", RefId = "02018412345", DateOfBirth = DateOnly.Parse("1984-01-02") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-77d3-b35b-4bbf7d207dc2"), Name = "Nina", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "03018412345", RefId = "03018412345", DateOfBirth = DateOnly.Parse("1984-01-03") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7d2c-b030-1d1c205d5400"), Name = "Kari", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "04018412345", RefId = "04018412345", DateOfBirth = DateOnly.Parse("1984-01-04") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7911-bf6c-67713e9fe4f8"), Name = "William", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "05018412345", RefId = "05018412345", DateOfBirth = DateOnly.Parse("1984-01-05") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-706b-9e0e-87f73c5b3ed0"), Name = "Terje", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "06018412345", RefId = "06018412345", DateOfBirth = DateOnly.Parse("1984-01-06") },
    };

    #pragma warning disable SA1401 // Fields should be private
    internal static List<Assignment> Assignments = new()
    #pragma warning restore SA1401 // Fields should be private
    {
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7f47-9046-14e33e571f55"), FromId = Entities.First(t => t.Name == "Baker Johnsen").Id, ToId = Entities.First(t => t.Name == "Regnskaperne").Id, RoleId = RoleConstants.Accountant }, // Regnskapsfører
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7c9d-9191-9e6c04c4683c"), FromId = Entities.First(t => t.Name == "Regnskaperne").Id, ToId = Entities.First(t => t.Name == "Petter").Id, RoleId = RoleConstants.ManagingDirector }, // Daglig leder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7f16-b2b7-b539522eeddb"), FromId = Entities.First(t => t.Name == "Regnskaperne").Id, ToId = Entities.First(t => t.Name == "Gunnar").Id, RoleId = RoleConstants.Agent }, // Agent
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7129-b367-cab290cc6e11"), FromId = Entities.First(t => t.Name == "Skrik Frisør").Id, ToId = Entities.First(t => t.Name == "Nina").Id, RoleId = RoleConstants.ManagingDirector }, // Daglig leder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a3c-8f8b-f97e069f6aa0"), FromId = Entities.First(t => t.Name == "Skrik Frisør").Id, ToId = Entities.First(t => t.Name == "Nina").Id, RoleId = RoleConstants.Rightholder }, // Rettighetsholder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-717a-85a5-0e127816ed69"), FromId = Entities.First(t => t.Name == "Regnskaperne").Id, ToId = Entities.First(t => t.Name == "Revi").Id, RoleId = RoleConstants.Auditor }, // Revisor 
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-708d-beec-13141fd0fae3"), FromId = Entities.First(t => t.Name == "Revi").Id, ToId = Entities.First(t => t.Name == "William").Id, RoleId = RoleConstants.ManagingDirector }, // Daglig leder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-759a-8a59-ce8abd161c1b"), FromId = Entities.First(t => t.Name == "Revi").Id, ToId = Entities.First(t => t.Name == "Terje").Id, RoleId = RoleConstants.ChairOfTheBoard }, // Styreleder
    };

    internal static Assignment GetAssignment(string fromName, string toName, Guid roleId)
    {
        var fromEntity = Entities.First(t => t.Name == fromName);
        var toEntity = Entities.First(t => t.Name == toName);
        
        return Assignments.First(t => t.FromId == fromEntity.Id && t.ToId == toEntity.Id);
    }

    #pragma warning disable SA1401 // Fields should be private
    internal static List<Delegation> Delegations = new()
    #pragma warning restore SA1401 // Fields should be private
    {
        new Delegation() { FromId = GetAssignment("Baker Johnsen", "Regnskaperne", RoleConstants.Accountant).Id, ToId = GetAssignment("Regnskaperne", "Gunnar", RoleConstants.Agent).Id, FacilitatorId = GetEntity("Regnskaperne").Id },
        new Delegation() { FromId = GetAssignment("Baker Johnsen", "Regnskaperne", RoleConstants.Accountant).Id, ToId = GetAssignment("Regnskaperne", "Revi", RoleConstants.Auditor).Id, FacilitatorId = GetEntity("Regnskaperne").Id },
    };
}

/*



0195efb8-7c80-7bda-9f72-4ef5c897f619
0195efb8-7c80-7743-b054-e946a946c44a
0195efb8-7c80-7311-a2df-21bd3352ba24
0195efb8-7c80-7839-8b2a-8794bf7a929d
0195efb8-7c80-7e40-9ea1-4362ea2aefa5
0195efb8-7c80-76ee-a7d9-f2d76ac7187b
0195efb8-7c80-7a77-8203-e7ca159053d0
0195efb8-7c80-7b15-bfc8-4d596bc18d01
0195efb8-7c80-7604-8518-2ddb45c89645
0195efb8-7c80-7532-9427-433385e63908
0195efb8-7c80-7d79-87ce-f1dcdf1bba79
0195efb8-7c80-73a9-a8e4-78615f974d92
0195efb8-7c80-7d62-920b-051135c76e45
0195efb8-7c80-7fbc-aa99-25524087073b
0195efb8-7c80-7293-b522-68cf3998f2ee
0195efb8-7c80-79b4-ae6e-32ffcf783b5e
0195efb8-7c80-7eca-85f2-434a235c966f
0195efb8-7c80-7f61-9aa2-4fbe5e208795
0195efb8-7c80-71b9-a6ea-d4bc8f68d681
0195efb8-7c80-7fd5-9676-2eb0d1b62c8e
0195efb8-7c80-7dfa-aaab-2c1023643bba
0195efb8-7c80-71b4-8ddb-cb457780038a
0195efb8-7c80-79ea-8db3-56ef39cbef4f
0195efb8-7c80-7c54-b2e9-5e219227c565
0195efb8-7c80-70b0-8731-671de4b2eeef
*/
