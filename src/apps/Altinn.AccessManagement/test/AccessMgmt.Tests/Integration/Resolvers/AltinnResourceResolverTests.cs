using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Resolvers;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils.Factories;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.Authorization.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using LocalResource = Altinn.AccessMgmt.PersistenceEF.Models.Resource;
using LocalResourceType = Altinn.AccessMgmt.PersistenceEF.Models.ResourceType;

namespace Altinn.AccessManagement.Tests.Integration.Resolvers;

/// <summary>
/// Tests for <see cref="AltinnResourceResolver.ResolveResourceRegistryId"/>, which reads Delegable and Type from the local resource.
/// </summary>
[IntegrationTest]
public class AltinnResourceResolverTests : IAsyncLifetime
{
    private PostgresDatabase? _database;
    private ServiceProvider? _serviceProvider;

    public async ValueTask InitializeAsync()
    {
        _database = await EFPostgresFactory.Create();
        _serviceProvider = new ServiceCollection()
            .AddAccessManagementDatabase(opts =>
            {
                opts.MigrationConnectionString = _database.Admin.ToString();
                opts.Source = SourceType.Migration;
                opts.EnableEFPooling = false;
            })
            .BuildServiceProvider();
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        NpgsqlConnection.ClearAllPools();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ResolveResourceRegistryId_DelegableResource_ReturnsDelegableAndType()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("resolver_delegable", "GenericAccessResource", delegable: true, ct);

        var result = await Resolve("resolver_delegable", ct);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Delegable && a.Value == bool.TrueString);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Type && a.Value == nameof(ResourceType.GenericAccessResource));
    }

    [Fact]
    public async Task ResolveResourceRegistryId_NonDelegableResource_ReturnsDelegableFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("resolver_not_delegable", "AltinnApp", delegable: false, ct);

        var result = await Resolve("resolver_not_delegable", ct);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Delegable && a.Value == bool.FalseString);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Type && a.Value == nameof(ResourceType.AltinnApp));
    }

    [Fact]
    public async Task ResolveResourceRegistryId_UnknownResource_ReturnsNoAttributes()
    {
        var result = await Resolve("resolver_unknown_resource", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveResourceRegistryId_UnmappedTypeName_ReturnsDelegableWithoutType()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("resolver_unmapped_type", "NotAResourceRegistryType", delegable: true, ct);

        var result = await Resolve("resolver_unmapped_type", ct);

        var attribute = Assert.Single(result);
        Assert.Equal(BaseUrn.Altinn.Resource.Delegable, attribute.Id);
        Assert.Equal(bool.TrueString, attribute.Value);
    }

    /// <summary>
    /// The import stores the type name it receives from the resource registry, which serializes <see cref="ResourceType"/> by name.
    /// Every single defined type must map back, also when the stored casing differs.
    /// </summary>
    [Fact]
    public async Task ResolveResourceRegistryId_EveryResourceTypeName_MapsBackToEnum()
    {
        var ct = TestContext.Current.CancellationToken;
        var types = Enum.GetValues<ResourceType>().Where(t => t is not ResourceType.Default and not ResourceType.All);

        foreach (var type in types)
        {
            var refId = $"resolver_type_{type}".ToLowerInvariant();
            await SeedResource(refId, type.ToString().ToLowerInvariant(), delegable: true, ct);

            var result = await Resolve(refId, ct);

            Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Type && a.Value == type.ToString());
        }
    }

    private async Task<List<AttributeMatch>> Resolve(string resourceId, CancellationToken ct)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Strict: the resource id leaf must not fall back to the resource registry.
        var contextRetrievalService = new Mock<IContextRetrievalService>(MockBehavior.Strict);
        var resolver = new AltinnResourceResolver(contextRetrievalService.Object, db);

        var result = await resolver.ResolveResourceRegistryId()([new(BaseUrn.Altinn.Resource.ResourceRegistryId, resourceId)], ct);
        return [.. result];
    }

    private async Task SeedResource(string refId, string typeName, bool delegable, CancellationToken ct)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var type = await db.ResourceTypes.FirstOrDefaultAsync(t => t.Name == typeName, ct);
        if (type is null)
        {
            type = new LocalResourceType { Id = Guid.CreateVersion7(), Name = typeName };
            db.ResourceTypes.Add(type);
        }

        db.Resources.Add(new LocalResource
        {
            Id = Guid.CreateVersion7(),
            RefId = refId,
            Name = refId,
            Description = refId,
            TypeId = type.Id,
            ProviderId = ProviderConstants.Altinn3,
            Delegable = delegable,
        });

        await db.SaveChangesAsync(ct);
    }

    private IServiceScope CreateScope() =>
        _serviceProvider!.CreateEFScope(new AuditValues(SystemEntityConstants.StaticDataIngest));
}
