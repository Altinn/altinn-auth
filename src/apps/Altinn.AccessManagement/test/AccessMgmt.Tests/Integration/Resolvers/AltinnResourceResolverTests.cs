using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Resolvers;
using Altinn.AccessManagement.TestUtils.Factories;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.Authorization.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using LocalResource = Altinn.AccessMgmt.PersistenceEF.Models.Resource;
using LocalResourceType = Altinn.AccessMgmt.PersistenceEF.Models.ResourceType;

namespace Altinn.AccessManagement.Tests.Integration.Resolvers;

/// <summary>
/// Tests for <see cref="AltinnResourceResolver.ResolveResourceRegistryId"/> and <see cref="AltinnResourceResolver.ResolveAppOwnerAndAppId"/>,
/// which read the resource attributes from the local resource.
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

    /// <summary>
    /// Enum.TryParse accepts numbers and comma-separated flags, such as "8" for AltinnApp, so those must not map to a type.
    /// </summary>
    [Theory]
    [InlineData("resolver_unmapped_type_unknown", "NotAResourceRegistryType")]
    [InlineData("resolver_unmapped_type_number", "8")]
    [InlineData("resolver_unmapped_type_negative", "-1")]
    [InlineData("resolver_unmapped_type_flags", "AltinnApp, Consent")]
    public async Task ResolveResourceRegistryId_UnmappedTypeName_ReturnsDelegableWithoutType(string refId, string typeName)
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource(refId, typeName, delegable: true, ct);

        var result = await Resolve(refId, ct);

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

    [Fact]
    public async Task ResolveAppOwnerAndAppId_AltinnApp_ReturnsDelegableTypeAndIdentifier()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("app_ttd_resolver-app", "AltinnApp", delegable: true, ct);

        var result = await ResolveApp("ttd", "resolver-app", ct);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Delegable && a.Value == bool.TrueString);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Type && a.Value == nameof(ResourceType.AltinnApp));
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.ResourceRegistryId && a.Value == "app_ttd_resolver-app");
    }

    [Fact]
    public async Task ResolveAppOwnerAndAppId_MigratedApp_ReturnsDelegableTypeAndIdentifier()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("app_ttd_a1-3456-1:2", "MigratedApp", delegable: false, ct);

        var result = await ResolveApp("ttd", "a1-3456-1:2", ct);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Delegable && a.Value == bool.FalseString);
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Type && a.Value == nameof(ResourceType.MigratedApp));
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.ResourceRegistryId && a.Value == "app_ttd_a1-3456-1:2");
    }

    /// <summary>
    /// The resource registry matched org and app case-insensitively, so input casing must not matter. The identifier is
    /// returned as stored.
    /// </summary>
    [Theory]
    [InlineData("TTD", "Resolver-Mixed-Case")]
    [InlineData("ttd", "RESOLVER-MIXED-CASE")]
    [InlineData("Ttd", "resolver-mixed-case")]
    public async Task ResolveAppOwnerAndAppId_MixedCaseInput_ReturnsResource(string org, string app)
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("app_ttd_resolver-mixed-case", "AltinnApp", delegable: true, ct);

        var result = await ResolveApp(org, app, ct);

        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Type && a.Value == nameof(ResourceType.AltinnApp));
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.ResourceRegistryId && a.Value == "app_ttd_resolver-mixed-case");
    }

    [Fact]
    public async Task ResolveAppOwnerAndAppId_UnknownApp_ReturnsNoAttributes()
    {
        var result = await ResolveApp("ttd", "resolver-unknown-app", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>
    /// Only app resources are matched, as the resource registry lookup by app owner and app id did.
    /// </summary>
    [Fact]
    public async Task ResolveAppOwnerAndAppId_ResourceIsNotAnApp_ReturnsNoAttributes()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("app_ttd_resolver-not-an-app", "GenericAccessResource", delegable: true, ct);

        var result = await ResolveApp("ttd", "resolver-not-an-app", ct);

        Assert.Empty(result);
    }

    /// <summary>
    /// Identifiers are unique case-sensitively, so case variants can coexist. The lower-case identifier, which is the one
    /// Resource Registry builds, wins.
    /// </summary>
    [Fact]
    public async Task ResolveAppOwnerAndAppId_CaseVariantIdentifiers_PrefersLowerCase()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("app_ttd_Resolver-Variant", "AltinnApp", delegable: false, ct);
        await SeedResource("app_ttd_resolver-variant", "AltinnApp", delegable: true, ct);

        var result = await ResolveApp("TTD", "Resolver-Variant", ct);

        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.ResourceRegistryId && a.Value == "app_ttd_resolver-variant");
        Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Delegable && a.Value == bool.TrueString);
    }

    /// <summary>
    /// <see cref="Core.Resolvers.AttributeResolver"/> runs leaves concurrently. Given app owner, app id and resource id, both
    /// leaves query the same <see cref="AppDbContext"/> in one pass, which must not fault.
    /// </summary>
    [Fact]
    public async Task Resolve_AppAndResourceIdTogether_BothLeavesShareTheContext()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedResource("app_ttd_resolver-both", "AltinnApp", delegable: true, ct);

        using var scope = CreateScope();
        var resolver = new AltinnResourceResolver(scope.ServiceProvider.GetRequiredService<AppDbContext>());

        for (var i = 0; i < 20; i++)
        {
            var result = await resolver.Resolve(
                [
                    new(BaseUrn.Altinn.Resource.AppOwner, "ttd"),
                    new(BaseUrn.Altinn.Resource.AppId, "resolver-both"),
                    new(BaseUrn.Altinn.Resource.ResourceRegistryId, "app_ttd_resolver-both"),
                ],
                [BaseUrn.Altinn.Resource.Delegable, BaseUrn.Altinn.Resource.Type],
                ct);

            Assert.Contains(result, a => a.Id == BaseUrn.Altinn.Resource.Delegable && a.Value == bool.TrueString);
        }
    }

    private async Task<List<AttributeMatch>> Resolve(string resourceId, CancellationToken ct)
    {
        using var scope = CreateScope();
        var resolver = new AltinnResourceResolver(scope.ServiceProvider.GetRequiredService<AppDbContext>());

        var result = await resolver.ResolveResourceRegistryId()([new(BaseUrn.Altinn.Resource.ResourceRegistryId, resourceId)], ct);
        return [.. result];
    }

    private async Task<List<AttributeMatch>> ResolveApp(string org, string app, CancellationToken ct)
    {
        using var scope = CreateScope();
        var resolver = new AltinnResourceResolver(scope.ServiceProvider.GetRequiredService<AppDbContext>());

        var result = await resolver.ResolveAppOwnerAndAppId()([new(BaseUrn.Altinn.Resource.AppOwner, org), new(BaseUrn.Altinn.Resource.AppId, app)], ct);
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
