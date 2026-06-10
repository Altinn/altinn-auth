using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Utils;

/// <summary>
/// <see cref="IngestService"/>
/// </summary>
[IntegrationTest]
public class IngestServiceTest : IClassFixture<ApiFixture>
{
    public ApiFixture Fixture { get; }

    public IngestServiceTest(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task IngestAndMergeData_InsertsNewProvider_WhenMatchByIdHasNoExistingRow()
    {
        var id = Guid.CreateVersion7();
        var name = $"IngestServiceTest insert {id}";
        var provider = NewProvider(id, name, refId: "999000001");

        using var scope = Fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
        var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();

        await ingest.IngestAndMergeData(
            [provider],
            new AuditValues(SystemEntityConstants.StaticDataIngest),
            ["Id"],
            ignoreColumnsToUpdate: ["audit_validfrom"],
            ignoreColumnsToInsert: ["audit_validfrom"],
            cancellationToken: TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var stored = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(name, stored.Name);
        });
    }

    [Fact]
    public async Task IngestAndMergeData_UpdatesExistingProvider_WhenMatchByIdFindsRow()
    {
        var id = Guid.CreateVersion7();
        var initial = NewProvider(id, $"IngestServiceTest before {id}", refId: "999000002");

        await Fixture.QueryDb(async db =>
        {
            db.Providers.Add(initial);
            await db.SaveChangesAsync(new AuditValues(SystemEntityConstants.StaticDataIngest), TestContext.Current.CancellationToken);
        });

        var updatedName = $"IngestServiceTest after {id}";
        var updated = NewProvider(id, updatedName, refId: "999000002", code: initial.Code);

        using var scope = Fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
        var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();

        await ingest.IngestAndMergeData(
            [updated],
            new AuditValues(SystemEntityConstants.StaticDataIngest),
            ["Id"],
            ignoreColumnsToUpdate: ["audit_validfrom"],
            ignoreColumnsToInsert: ["audit_validfrom"],
            cancellationToken: TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var stored = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(updatedName, stored.Name);
        });
    }

    [Fact]
    public async Task IngestAndMergeData_DefaultsMatchColumnsToId_WhenNullOrEmptyProvided()
    {
        var id = Guid.CreateVersion7();
        var provider = NewProvider(id, $"IngestServiceTest defaults {id}", refId: "999000003");

        using var scope = Fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
        var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();

        await ingest.IngestAndMergeData(
            [provider],
            new AuditValues(SystemEntityConstants.StaticDataIngest),
            matchColumns: null,
            ignoreColumnsToUpdate: ["audit_validfrom"],
            ignoreColumnsToInsert: ["audit_validfrom"],
            cancellationToken: TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            Assert.NotNull(await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task IngestTempData_WithEmptyIngestId_ThrowsArgumentException()
    {
        using var scope = Fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
        var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            ingest.IngestTempData(new List<Provider>(), Guid.Empty, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MergeTempData_WithoutMatchingIngestTable_WrapsUnderlyingException()
    {
        using var scope = Fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
        var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ingest.MergeTempData<Provider>(
            Guid.CreateVersion7(),
            new AuditValues(SystemEntityConstants.StaticDataIngest),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Failed to execute merge statement", ex.Message);
    }

    private static Provider NewProvider(Guid id, string name, string refId, string? code = null) => new()
    {
        Id = id,
        Name = name,
        RefId = refId,
        Code = code ?? $"itest-{id:N}"[..16],
        TypeId = ProviderTypeConstants.ServiceOwner,
    };
}
