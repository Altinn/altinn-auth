using System.Net.Http.Json;
using System.Text.Json;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ResourceRegistry.Tests;

public class ResourceTimestampTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ResourceId = "timestamp_resource";
    private const string ResourcePath = "resourceregistry/api/v1/resource/";
    private static readonly DateTimeOffset OriginalTime = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IResourceRegistryRepository Repository => Services.GetRequiredService<IResourceRegistryRepository>();

    [Fact]
    public async Task CreateResource_ClientTimestamps_UsesDatabaseValues()
    {
        var input = new ServiceResource { Identifier = ResourceId, CreatedAt = OriginalTime, UpdatedAt = OriginalTime };
        var before = await GetDatabaseTime();
        var created = await Repository.CreateResource(input);

        Assert.NotNull(created.CreatedAt);
        Assert.InRange(created.CreatedAt.Value, before, await GetDatabaseTime());
        Assert.Equal(created.CreatedAt, created.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, created.CreatedAt.Value.Offset);
        Assert.NotEqual(input.CreatedAt, created.CreatedAt);
        AssertTimestamps(created, await Repository.GetResource(ResourceId, null));

        await using var cmd = DataSource.CreateCommand("SELECT serviceresourcejson ? 'createdAt' OR serviceresourcejson ? 'updatedAt' FROM resourceregistry.resources WHERE identifier = @id");
        cmd.Parameters.AddWithValue("id", ResourceId);
        Assert.Equal(false, await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task UpdateResource_NewVersion_ReturnsConsistentOriginalAndVersionTimestamps()
    {
        var original = await CreateHistoricalResource();
        var updated = await Repository.UpdateResource(new ServiceResource
        {
            Identifier = ResourceId,
            Description = new Dictionary<string, string> { ["en"] = "Updated metadata" },
            CreatedAt = OriginalTime.AddYears(-1),
            UpdatedAt = OriginalTime.AddYears(-1),
        });

        Assert.Equal(OriginalTime, updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt > original.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, updated.UpdatedAt.Value.Offset);
        Assert.True(updated.VersionId > original.VersionId);
        AssertTimestamps(updated, await Repository.GetResource(ResourceId, null));
        AssertTimestamps(original, await Repository.GetResource(ResourceId, original.VersionId));

        var allVersions = await Repository.Search(new ResourceSearch { Id = ResourceId }, includeAllVersions: true);
        Assert.Equal(2, allVersions.Count);
        AssertTimestamps(original, Assert.Single(allVersions, r => r.VersionId == original.VersionId));
        AssertTimestamps(updated, Assert.Single(allVersions, r => r.VersionId == updated.VersionId));
        await AssertApiTimestamps(updated, original);

        var deleted = await Repository.DeleteResource(ResourceId);
        AssertTimestamps(updated, deleted);
    }

    [Fact]
    public async Task GetResource_UnknownModifiedTime_ReturnsNullDespiteMetadataJson()
    {
        var original = await CreateHistoricalResource();
        await using var cmd = DataSource.CreateCommand("""
            UPDATE resourceregistry.resources
            SET modified = NULL,
                serviceresourcejson = serviceresourcejson || '{"createdAt":"untrusted","updatedAt":"untrusted"}'::jsonb
            WHERE identifier = @id
            """);
        cmd.Parameters.AddWithValue("id", ResourceId);
        await cmd.ExecuteNonQueryAsync();
        original.UpdatedAt = null;

        await AssertApiTimestamps(original, original);
        using var client = CreateClient();
        var json = await client.GetFromJsonAsync<JsonElement>(ResourcePath + ResourceId);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("updatedAt").ValueKind);
    }

    [Theory]
    [InlineData("app_skd_cluster-test")]
    [InlineData("resourcelist?includeApps=true")]
    public async Task GetResource_VirtualStorageApp_ReturnsNullTimestamps(string path)
    {
        using var client = CreateClient();
        var json = await client.GetFromJsonAsync<JsonElement>(ResourcePath + path);
        var resource = json.ValueKind == JsonValueKind.Array
            ? Assert.Single(json.EnumerateArray(), r => r.GetProperty("identifier").GetString() == "app_skd_cluster-test")
            : json;

        Assert.Equal(JsonValueKind.Null, resource.GetProperty("createdAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, resource.GetProperty("updatedAt").ValueKind);
    }

    private async Task<DateTimeOffset> GetDatabaseTime()
    {
        await using var cmd = DataSource.CreateCommand("SELECT now()");
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    private async Task<ServiceResource> CreateHistoricalResource()
    {
        await Repository.CreateResource(new ServiceResource { Identifier = ResourceId });
        await using var cmd = DataSource.CreateCommand("""
            UPDATE resourceregistry.resource_identifier SET created = @time WHERE identifier = @id;
            UPDATE resourceregistry.resources SET created = @time, modified = @time WHERE identifier = @id;
            """);
        cmd.Parameters.AddWithValue("id", ResourceId);
        cmd.Parameters.AddWithValue("time", OriginalTime);
        await cmd.ExecuteNonQueryAsync();
        return (await Repository.GetResource(ResourceId, null))!;
    }

    private async Task AssertApiTimestamps(ServiceResource latest, ServiceResource historical)
    {
        using var client = CreateClient();
        AssertTimestamps(latest, await client.GetFromJsonAsync<ServiceResource>(ResourcePath + ResourceId));
        AssertTimestamps(historical, await client.GetFromJsonAsync<ServiceResource>($"{ResourcePath}{ResourceId}?versionId={historical.VersionId}"));
        foreach (var path in new[] { "resourcelist?includeApps=false", $"Search?Id={ResourceId}" })
        {
            var resources = await client.GetFromJsonAsync<List<ServiceResource>>(ResourcePath + path);
            Assert.NotNull(resources);
            AssertTimestamps(latest, Assert.Single(resources));
        }
    }

    private static void AssertTimestamps(ServiceResource expected, ServiceResource? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.VersionId, actual.VersionId);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.UpdatedAt, actual.UpdatedAt);
    }
}
