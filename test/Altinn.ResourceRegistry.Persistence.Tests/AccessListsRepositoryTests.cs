
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
        var info1 = (await Repository.CreateAccessList("owner", "identifier", "name", "description")).AsAccessListInfo();

        info1.ResourceOwner.Should().Be("owner");
        info1.Identifier.Should().Be("identifier");
        info1.Name.Should().Be("name");
        info1.Description.Should().Be("description");
        await CheckRegistryLookup(info1);

        // same owner, different identifier
        var info2 = (await Repository.CreateAccessList("owner", "identifier2", "name", "description")).AsAccessListInfo();

        info2.ResourceOwner.Should().Be("owner");
        info2.Identifier.Should().Be("identifier2");
        info2.Name.Should().Be("name");
        info2.Description.Should().Be("description");
        await CheckRegistryLookup(info2);

        // different owner, same identifier
        var info3 = (await Repository.CreateAccessList("owner2", "identifier", "name", "description")).AsAccessListInfo();

        info3.ResourceOwner.Should().Be("owner2");
        info3.Identifier.Should().Be("identifier");
        info3.Name.Should().Be("name");
        info3.Description.Should().Be("description");
        await CheckRegistryLookup(info3);

        // same owner, same identifier
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository.CreateAccessList("owner", "identifier", "name", "description"));

        // create or load existing
        var result = await Repository.LoadOrCreateAccessList("owner", "identifier", "wrong name", "wrong description");
        var aggregate = result.Aggregate;
        result.IsNew.Should().BeFalse();
        aggregate.Name.Should().Be("name");
        aggregate.Description.Should().Be("description");

        // create or load new
        result = await Repository.LoadOrCreateAccessList("owner", "identifier3", "name3", "description3");
        aggregate = result.Aggregate;
        result.IsNew.Should().BeTrue();
        Assert.NotNull(aggregate);

        var info4 = aggregate.AsAccessListInfo();
        info4.ResourceOwner.Should().Be("owner");
        info4.Identifier.Should().Be("identifier3");
        info4.Name.Should().Be("name3");
        info4.Description.Should().Be("description3");
        await CheckRegistryLookup(info4);
    }

    [Fact]
    public async Task LookupNonExisting()
    { 
        var info = await Repository.LookupInfo("owner", "identifier");
        Assert.Null(info);

        info = await Repository.LookupInfo(Guid.NewGuid());
        Assert.Null(info);
    }

    [Fact]
    public async Task UpdateRegistry()
    {
        var original = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        // update identifier
        {
            var aggregate = await Repository.LoadAccessList(original.Id);
            Assert.NotNull(aggregate);
            aggregate.Update(identifier: "identifier2");
            await aggregate.SaveChanged();

            var info = await Repository.LookupInfo(original.Id);
            Assert.NotNull(info);
            info.Identifier.Should().Be("identifier2");
            info.Name.Should().Be("name");
            info.Description.Should().Be("description");
            await CheckRegistryLookup(info);
            (await Repository.LookupInfo("owner", "identifier")).Should().BeNull();
        }


        // update identifier back
        {
            var aggregate = await Repository.LoadAccessList(original.Id);
            Assert.NotNull(aggregate);
            aggregate.Update(identifier: "identifier");
            await aggregate.SaveChanged();

            var info = await Repository.LookupInfo(original.Id);
            Assert.NotNull(info);
            info.Identifier.Should().Be("identifier");
            info.Name.Should().Be("name");
            info.Description.Should().Be("description");
            await CheckRegistryLookup(info);
            (await Repository.LookupInfo("owner", "identifier2")).Should().BeNull();
        }

        // update name and description
        {
            var aggregate = await Repository.LoadAccessList(original.Id);
            Assert.NotNull(aggregate);
            aggregate.Update(name: "name2", description: "description2");
            await aggregate.SaveChanged();

            var info = await Repository.LookupInfo(original.Id);
            Assert.NotNull(info);
            info.Identifier.Should().Be("identifier");
            info.Name.Should().Be("name2");
            info.Description.Should().Be("description2");
            await CheckRegistryLookup(info);
        }
    }

    [Fact]
    public async Task DeleteRegistry()
    {
        var original = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        // delete registry
        {
            var aggregate = await Repository.LoadAccessList(original.Id);
            Assert.NotNull(aggregate);
            aggregate.Delete();
            await aggregate.SaveChanged();

            (await Repository.LookupInfo("owner", "identifier")).Should().BeNull();
        }

        // check that the aggregate is still loadable
        {
            var aggregate = await Repository.LoadAccessList(original.Id);
            Assert.NotNull(aggregate);
            aggregate.IsDeleted.Should().BeTrue();
        }
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
        var original1 = await Repository.CreateAccessList("owner", "identifier1", "name", "description");
        var original2 = await Repository.CreateAccessList("owner", "identifier2", "name", "description");

        // Check that we have no connections
        (await Repository.GetAccessListResourceConnections(original1.Id, includeActions: true)).Should().BeEmpty();
        (await Repository.GetAccessListResourceConnections(original2.Id, includeActions: true)).Should().BeEmpty();

        // Check that we can add a connection with empty actions list
        {
            var aggregate = await Repository.LoadAccessList(original1.Id);
            Assert.NotNull(aggregate);
            var connection = aggregate.AddResourceConnection(RESOURCE1_NAME, ImmutableArray<string>.Empty);
            await aggregate.SaveChanged();

            connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            connection.Actions.Should().BeEmpty();

            var connections = await Repository.GetAccessListResourceConnections(original1.Id, includeActions: true);
            Assert.NotNull(connections);
            connections.Should().ContainSingle();
            connections[0].ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            connections[0].Actions.Should().BeEmpty();

            var info = await Repository.LookupInfo(original1.Id, AccessListIncludes.ResourceConnectionsActions);
            Assert.NotNull(info);
            Assert.NotNull(info.ResourceConnections);
            info.ResourceConnections.Should().ContainSingle();
            info.ResourceConnections[0].ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            info.ResourceConnections[0].Actions.Should().BeEmpty();
        }

        // check that we can add actions to the resource connection
        {
            var aggregate = await Repository.LoadAccessList(original1.ResourceOwner, original1.Identifier);
            Assert.NotNull(aggregate);
            var connection = aggregate.AddResourceConnectionActions(RESOURCE1_NAME, ImmutableArray.Create(ACTION_READ, ACTION_WRITE));
            await aggregate.SaveChanged();

            connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            connection.Actions.Should().HaveCount(2)
                .And.Contain(ACTION_READ)
                .And.Contain(ACTION_WRITE);

            var connections = await Repository.GetAccessListResourceConnections(original1.ResourceOwner, original1.Identifier, includeActions: true);
            Assert.NotNull(connections);
            connections.Should().ContainSingle();
            connections[0].ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            connections[0].Actions.Should().HaveCount(2)
                .And.Contain(ACTION_READ)
                .And.Contain(ACTION_WRITE);
        }

        // check that we can add another connection
        {
            var aggregate = await Repository.LoadAccessList(original1.ResourceOwner, original1.Identifier);
            Assert.NotNull(aggregate);
            var connection = aggregate.AddResourceConnection(RESOURCE2_NAME, ImmutableArray.Create(ACTION_READ));
            await aggregate.SaveChanged();

            connection.ResourceIdentifier.Should().Be(RESOURCE2_NAME);
            connection.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_READ);

            var connections = await Repository.GetAccessListResourceConnections(original1.ResourceOwner, original1.Identifier, includeActions: true);
            Assert.NotNull(connections);
            connections.Should().HaveCount(2);
            connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE1_NAME)
                .Which.Actions.Should().HaveCount(2)
                .And.Contain(ACTION_READ)
                .And.Contain(ACTION_WRITE);
            connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE2_NAME)
                .Which.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_READ);
        }

        // check that we can remove an action from one of the connections
        {
            var aggregate = await Repository.LoadAccessList(original1.ResourceOwner, original1.Identifier);
            Assert.NotNull(aggregate);
            var connection = aggregate.RemoveResourceConnectionActions(RESOURCE1_NAME, ImmutableArray.Create(ACTION_READ));
            await aggregate.SaveChanged();

            connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            connection.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_WRITE);

            var connections = await Repository.GetAccessListResourceConnections(original1.ResourceOwner, original1.Identifier, includeActions: true);
            Assert.NotNull(connections);
            connections.Should().HaveCount(2);
            connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE1_NAME)
                .Which.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_WRITE);
            connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE2_NAME)
                .Which.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_READ);
        }

        // check that we can remote a connection
        {
            var aggregate = await Repository.LoadAccessList(original1.ResourceOwner, original1.Identifier);
            Assert.NotNull(aggregate);
            var connection = aggregate.RemoveResourceConnection(RESOURCE1_NAME);
            await aggregate.SaveChanged();

            connection.ResourceIdentifier.Should().Be(RESOURCE1_NAME);
            connection.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_WRITE);

            var connections = await Repository.GetAccessListResourceConnections(original1.ResourceOwner, original1.Identifier, includeActions: true);
            Assert.NotNull(connections);
            connections.Should().HaveCount(1);
            connections.Should().ContainSingle(c => c.ResourceIdentifier == RESOURCE2_NAME)
                .Which.Actions.Should().HaveCount(1)
                .And.Contain(ACTION_READ);
        }

        // check that we can get a list of all lists with all connections
        {
            var aggregate2 = await Repository.LoadAccessList(original2.ResourceOwner, original2.Identifier);
            Assert.NotNull(aggregate2);
            aggregate2.AddResourceConnection(RESOURCE1_NAME, ImmutableArray.Create(ACTION_READ));
            aggregate2.AddResourceConnection(RESOURCE2_NAME, ImmutableArray.Create(ACTION_WRITE));
            await aggregate2.SaveChanged();

            var infos = await Repository.GetAccessListsByOwner(original1.ResourceOwner, continueFrom: null, 10, AccessListIncludes.ResourceConnections);
            Assert.NotNull(infos);
            infos.Should().HaveCount(2);

            var info1 = infos.Should().ContainSingle(i => i.Identifier == original1.Identifier).Which;
            var info2 = infos.Should().ContainSingle(i => i.Identifier == original2.Identifier).Which;

            info1.ResourceConnections.Should().HaveCount(1);
            info2.ResourceConnections.Should().HaveCount(2);
        }
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
        {
            var memberships = await Repository.GetAccessListMemberships(info.Id);
            memberships.Should().BeEmpty();
        }

        // Add members
        {
            var aggregate = await Repository.LoadAccessList(info.Id);
            Assert.NotNull(aggregate);
            aggregate.AddMembers(ImmutableArray.Create(member1, member2));
            await aggregate.SaveChanged();

            var memberships = await Repository.GetAccessListMemberships(info.Id);
            memberships.Should().HaveCount(2)
                .And.Contain(m => m.PartyId == member1)
                .And.Contain(m => m.PartyId == member2);
        }

        // Add more members
        {
            var aggregate = await Repository.LoadAccessList(info.Id);
            Assert.NotNull(aggregate);
            aggregate.AddMembers(ImmutableArray.Create(member3, member4));
            await aggregate.SaveChanged();

            var memberships = await Repository.GetAccessListMemberships(info.Id);
            memberships.Should().HaveCount(4)
                .And.Contain(m => m.PartyId == member1)
                .And.Contain(m => m.PartyId == member2)
                .And.Contain(m => m.PartyId == member3)
                .And.Contain(m => m.PartyId == member4);
        }

        // Remove members
        {
            var aggregate = await Repository.LoadAccessList(info.Id);
            Assert.NotNull(aggregate);
            aggregate.RemoveMembers(ImmutableArray.Create(member2, member3));
            await aggregate.SaveChanged();

            var memberships = await Repository.GetAccessListMemberships(info.Id);
            memberships.Should().HaveCount(2)
                .And.Contain(m => m.PartyId == member1)
                .And.Contain(m => m.PartyId == member4);
        }

        // Remove remaining members
        {
            var aggregate = await Repository.LoadAccessList(info.Id);
            Assert.NotNull(aggregate);
            aggregate.RemoveMembers(ImmutableArray.Create(member1, member4));
            await aggregate.SaveChanged();

            var memberships = await Repository.GetAccessListMemberships(info.Id);
            memberships.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task CheckOptimisticConcurrency()
    {
        var original = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        var aggregate1 = await Repository.LoadAccessList(original.Id);
        var aggregate2 = await Repository.LoadAccessList(original.Id);

        Assert.NotNull(aggregate1);
        Assert.NotNull(aggregate2);
        aggregate1.Should().NotBeSameAs(aggregate2, "we should have to separate aggregate instances pointing to the same aggregate");

        // update something in aggregate 1
        aggregate1.Update(name: "name2");

        // update something else in aggregate 2
        aggregate2.AddMembers(ImmutableArray.Create(Guid.NewGuid()));

        // save aggregate 1
        await aggregate1.SaveChanged();

        // try to save aggregate 2
        var exn = await aggregate2.Awaiting(x => x.SaveChanged()).Should().ThrowAsync<OptimisticConcurrencyException>();
        exn.Which.AggregateId.Should().Be(original.Id);
    }

    [Fact]
    public async Task SaveMultipleTimes()
    {
        var aggregate = await Repository.CreateAccessList("owner", "identifier", "name", "description");

        aggregate.Update(name: "name2");
        await aggregate.SaveChanged();

        await CheckRegistryLookup(aggregate.AsAccessListInfo());

        aggregate.Update(name: "name3");
        await aggregate.SaveChanged();

        await CheckRegistryLookup(aggregate.AsAccessListInfo());
    }

    [Fact]
    public async Task SaveMultipleChangesAtOnce()
    {
        var party1 = Guid.NewGuid();
        var party2 = Guid.NewGuid();
        var party3 = Guid.NewGuid();

        var original = await Repository.CreateAccessList("owner", "identifier", "name", "description");
        var originalVersion = original.CommittedVersion;

        original.Update(name: "name2");
        original.Update(name: "name3");
        original.AddMembers(ImmutableArray.Create(party1, party2, party3));
        original.RemoveMembers(ImmutableArray.Create(party2));
        await original.SaveChanged();

        var newVersion = original.CommittedVersion;
        newVersion.Value.Should().BeGreaterThan(originalVersion.Value!.Value);

        await CheckRegistryLookup(original.AsAccessListInfo());
        var members = await Repository.GetAccessListMemberships(original.Id);
        members.Should().HaveCount(2)
            .And.Contain(m => m.PartyId == party1)
            .And.Contain(m => m.PartyId == party3);
    }

    [Fact]
    public async Task RaceMultipleLoadOrCreate()
    {
        var latch = new ManualResetEvent(false);

        var readyTasks = new List<Task>();
        var resultTasks = new List<Task<ulong>>();

        for(var idx = 0; idx < 10; idx++)
        {
            var readySource = new TaskCompletionSource();

            var resultTask = Task.Run(async () =>
            {
                var resultSource = new TaskCompletionSource<Task<AccessListLoadOrCreateResult>>();
                var thread = new Thread(() =>
                {
                    try
                    {
                        readySource.SetResult();
                        latch.WaitOne();
                        var result = Repository.LoadOrCreateAccessList("owner", "identifier", $"name {idx}", $"description {idx}");
                        resultSource.SetResult(result);
                    }
                    catch (Exception e)
                    {
                        readySource.TrySetException(e);
                        resultSource.TrySetException(e);
                    }
                });

                thread.Start();
                var result = await resultSource.Task.Unwrap();
                return result.Aggregate.CommittedVersion.UnsafeValue;
            });

            readyTasks.Add(readySource.Task);
            resultTasks.Add(resultTask);
        }

        await Task.WhenAll(readyTasks);
        await Task.Delay(TimeSpan.FromMilliseconds(100)); // wait a bit to make sure all threads are waiting on the latch
        latch.Set();
        var versions = await Task.WhenAll(resultTasks);
        var expectedVersion = versions[0];
        versions.Should().AllSatisfy(version => version.Should().Be(expectedVersion));
    }

    private async Task CheckRegistryLookup(AccessListInfo info)
    {
        var lookup = await Repository.LookupInfo(info.ResourceOwner, info.Identifier);
        Assert.NotNull(lookup);
        lookup.ResourceOwner.Should().Be(info.ResourceOwner);
        lookup.Identifier.Should().Be(info.Identifier);
        lookup.Name.Should().Be(info.Name);
        lookup.Description.Should().Be(info.Description);

        lookup = await Repository.LookupInfo(info.Id);
        Assert.NotNull(lookup);
        lookup.ResourceOwner.Should().Be(info.ResourceOwner);
        lookup.Identifier.Should().Be(info.Identifier);
        lookup.Name.Should().Be(info.Name);
        lookup.Description.Should().Be(info.Description);
    }
}

