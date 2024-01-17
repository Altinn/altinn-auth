
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Collections.Immutable;

namespace Altinn.ResourceRegistry.Persistence.Tests;

public class AccessListsRepositoryTests : DbTests
{
    public AccessListsRepositoryTests(DbFixture dbFixture) 
        : base(dbFixture)
    {
    }

    protected IAccessListsRepository Repository => Services.GetRequiredService<IAccessListsRepository>();
    protected NpgsqlDataSource DataSource => Services.GetRequiredService<NpgsqlDataSource>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddPartyRegistryRepository();
        base.ConfigureServices(services);
    }

    [Fact]
    public async Task CreatePartyRegistry()
    {
        var info1 = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        info1.RegistryOwner.Should().Be("owner");
        info1.Identifier.Should().Be("identifier");
        info1.Name.Should().Be("name");
        info1.Description.Should().Be("description");
        await CheckRegistryLookup(info1);

        // same owner, different identifier
        var info2 = await Repository.CreateAccessList("owner", "identifier2", "name", "description");

        info2.RegistryOwner.Should().Be("owner");
        info2.Identifier.Should().Be("identifier2");
        info2.Name.Should().Be("name");
        info2.Description.Should().Be("description");
        await CheckRegistryLookup(info2);

        // different owner, same identifier
        var info3 = await Repository.CreateAccessList("owner2", "identifier", "name", "description");

        info3.RegistryOwner.Should().Be("owner2");
        info3.Identifier.Should().Be("identifier");
        info3.Name.Should().Be("name");
        info3.Description.Should().Be("description");
        await CheckRegistryLookup(info3);

        // same owner, same identifier
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository.CreateAccessList("owner", "identifier", "name", "description"));
    }

    [Fact]
    public async Task LookupNonExisting()
    { 
        var info = await Repository.Lookup("owner", "identifier");
        Assert.Null(info);

        info = await Repository.Lookup(Guid.NewGuid());
        Assert.Null(info);
    }

    [Fact]
    public async Task UpdateRegistry()
    {
        var info = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        // update identifier
        info = await Repository.UpdateAccessList(info.Id, newIdentifier: "identifier2", newName: null, newDescription: null);

        info.Identifier.Should().Be("identifier2");
        info.Name.Should().Be("name");
        info.Description.Should().Be("description");
        await CheckRegistryLookup(info);
        (await Repository.Lookup("owner", "identifier")).Should().BeNull();

        // update identifier back
        info = await Repository.UpdateAccessList(info.Id, newIdentifier: "identifier", newName: null, newDescription: null);

        info.Identifier.Should().Be("identifier");
        info.Name.Should().Be("name");
        info.Description.Should().Be("description");
        await CheckRegistryLookup(info);
        (await Repository.Lookup("owner", "identifier2")).Should().BeNull();

        // update name and description
        info = await Repository.UpdateAccessList(info.Id, newIdentifier: null, newName: "name2", newDescription: "description2");

        info.Identifier.Should().Be("identifier");
        info.Name.Should().Be("name2");
        info.Description.Should().Be("description2");
        await CheckRegistryLookup(info);
    }

    [Fact]
    public async Task DeleteRegistry()
    {
        var info = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        // delete registry
        await Repository.DeleteAccessList(info.Id);
        (await Repository.Lookup("owner", "identifier")).Should().BeNull();

        // TODO: chech that the aggregate is still loadable
    }

    [Fact]
    public async Task AddAndRemoveResourceConnections()
    {
        const string ACTION_READ = "read";
        const string ACTION_WRITE = "write";

        const string RESOURCE1_NAME = "test1";
        const string RESOURCE2_NAME = "test2";

        // Insert a fake resource (we have a foreign constraint on the party registry table)
        await using var resourceCmd = DataSource.CreateCommand(/*strpsql*/"INSERT INTO resourceregistry.resources (identifier, created, serviceresourcejson) VALUES (@name, NOW(), @json);");
        var nameParam = resourceCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Text);
        var jsonParam = resourceCmd.Parameters.Add("json", NpgsqlTypes.NpgsqlDbType.Jsonb);
        jsonParam.Value = "{}";

        nameParam.Value = RESOURCE1_NAME;
        await resourceCmd.ExecuteNonQueryAsync();
        nameParam.Value = RESOURCE2_NAME;
        await resourceCmd.ExecuteNonQueryAsync();

        // Create a couple registries
        var info1 = await Repository.CreateAccessList("owner", "identifier1", "name", "description");
        var info2 = await Repository.CreateAccessList("owner", "identifier2", "name", "description");

        // Check that we have no connections
        (await Repository.GetAccessListResourceConnections(info1.Id)).Should().BeEmpty();
        (await Repository.GetAccessListResourceConnections(info2.Id)).Should().BeEmpty();

        // Check that we can add a connection with empty actions list
        var connection = await Repository.AddAccessListResourceConnection(info1.Id, RESOURCE1_NAME, ImmutableArray<string>.Empty);
        connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
        connection.Actions.Should().BeEmpty();

        var connections = await Repository.GetAccessListResourceConnections(info1.Id);
        connections.Should().ContainSingle();
        connections[0].ResourceIdentifier.Should().Be(RESOURCE1_NAME);
        connections[0].Actions.Should().BeEmpty();

        // check that we can add actions to the resource connection
        connection = await Repository.AddAccessListResourceConnectionActions(info1.RegistryOwner, info1.Identifier, RESOURCE1_NAME, ImmutableArray.Create(ACTION_READ, ACTION_WRITE));
        connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
        connection.Actions.Should().HaveCount(2);

        connections = await Repository.GetAccessListResourceConnections(info1.RegistryOwner, info1.Identifier);
        connections.Should().ContainSingle();
        connections[0].ResourceIdentifier.Should().Be(RESOURCE1_NAME);
        connections[0].Actions.Should().HaveCount(2)
            .And.Contain(ACTION_READ)
            .And.Contain(ACTION_WRITE);

        // check that we can add another connection
        connection = await Repository.AddAccessListResourceConnection(info1.RegistryOwner, info1.Identifier, RESOURCE2_NAME, ImmutableArray.Create(ACTION_READ));
        connection.ResourceIdentifier.Should().Be(RESOURCE2_NAME);
        connection.Actions.Should().HaveCount(1)
            .And.Contain(ACTION_READ);

        connections = await Repository.GetAccessListResourceConnections(info1.RegistryOwner, info1.Identifier);
        connections.Should().HaveCount(2);
        connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE1_NAME)
            .Which.Actions.Should().HaveCount(2)
            .And.Contain(ACTION_READ)
            .And.Contain(ACTION_WRITE);
        connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE2_NAME)
            .Which.Actions.Should().HaveCount(1)
            .And.Contain(ACTION_READ);

        // check that we can remove an action from one of the connections
        connection = await Repository.RemoveAccessListResourceConnectionActions(info1.RegistryOwner, info1.Identifier, RESOURCE1_NAME, ImmutableArray.Create(ACTION_READ));
        connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);

        connections = await Repository.GetAccessListResourceConnections(info1.RegistryOwner, info1.Identifier);
        connections.Should().HaveCount(2);
        connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE1_NAME)
            .Which.Actions.Should().HaveCount(1)
            .And.Contain(ACTION_WRITE);
        connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE2_NAME)
            .Which.Actions.Should().HaveCount(1)
            .And.Contain(ACTION_READ);

        // check that we can remote a connection
        connection = await Repository.DeleteAccessListResourceConnection(info1.RegistryOwner, info1.Identifier, RESOURCE1_NAME);
        connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
        connection.Actions.Should().HaveCount(1)
            .And.Contain(ACTION_WRITE);

        connections = await Repository.GetAccessListResourceConnections(info1.RegistryOwner, info1.Identifier);
        connections.Should().HaveCount(1);
        connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE2_NAME)
            .Which.Actions.Should().HaveCount(1)
            .And.Contain(ACTION_READ);
    }

    [Fact]
    public async Task AddRemoveMembers()
    {
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        var member3 = Guid.NewGuid();
        var member4 = Guid.NewGuid();

        var info = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        // Check that we have no members
        var memberships = await Repository.GetAccessListMemberships(info.Id);
        memberships.Should().BeEmpty();

        // Add members
        await Repository.AddAccessListMembers(info.Id, ImmutableArray.Create(member1, member2));
        memberships = await Repository.GetAccessListMemberships(info.Id);
        memberships.Should().HaveCount(2)
            .And.Contain(m => m.PartyId == member1)
            .And.Contain(m => m.PartyId == member2);

        // Add more members
        await Repository.AddAccessListMembers(info.RegistryOwner, info.Identifier, ImmutableArray.Create(member3, member4));
        memberships = await Repository.GetAccessListMemberships(info.Id);
        memberships.Should().HaveCount(4)
            .And.Contain(m => m.PartyId == member1)
            .And.Contain(m => m.PartyId == member2)
            .And.Contain(m => m.PartyId == member3)
            .And.Contain(m => m.PartyId == member4);

        // Remove members
        await Repository.RemoveAccessListMembers(info.Id, ImmutableArray.Create(member2, member3));
        memberships = await Repository.GetAccessListMemberships(info.Id);
        memberships.Should().HaveCount(2)
            .And.Contain(m => m.PartyId == member1)
            .And.Contain(m => m.PartyId == member4);

        // Remove remaining members
        await Repository.RemoveAccessListMembers(info.RegistryOwner, info.Identifier, ImmutableArray.Create(member1, member4));
        memberships = await Repository.GetAccessListMemberships(info.Id);
        memberships.Should().BeEmpty();
    }

    private async Task CheckRegistryLookup(AccessListInfo info)
    {
        var lookup = await Repository.Lookup(info.RegistryOwner, info.Identifier);
        Assert.NotNull(lookup);
        lookup.RegistryOwner.Should().Be(info.RegistryOwner);
        lookup.Identifier.Should().Be(info.Identifier);
        lookup.Name.Should().Be(info.Name);
        lookup.Description.Should().Be(info.Description);

        lookup = await Repository.Lookup(info.Id);
        Assert.NotNull(lookup);
        lookup.RegistryOwner.Should().Be(info.RegistryOwner);
        lookup.Identifier.Should().Be(info.Identifier);
        lookup.Name.Should().Be(info.Name);
        lookup.Description.Should().Be(info.Description);
    }
}

