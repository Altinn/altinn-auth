using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.HostedServices.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.HostedServices.Outbox;

/// <summary>
/// <see cref="OutboxReaperJob"/>
/// </summary>
public class OutboxReaperJobTest : IClassFixture<ApiFixture>
{
    public ApiFixture Fixture { get; }

    public OutboxReaperJobTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.ConfigureServices(services =>
        {
            services.AddSingleton<SuccessHandler>();
            services.AddSingleton<FailureHandler>();
            services.AddSingleton<TimeoutHandler>();
            services.AddSingleton<OutboxHandlerJob>();
            services.AddSingleton<OutboxReaperJob>();
            services.PostConfigure<AccessManagementDatabaseOptions>(opts =>
            {
                opts.AddOutboxHandler<SuccessHandler>(nameof(SuccessHandler));
                opts.AddOutboxHandler<FailureHandler>(nameof(FailureHandler));
                opts.AddOutboxHandler<TimeoutHandler>(nameof(TimeoutHandler));
            });
        });
    }

    [Fact]
    public async Task OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes()
    {
        await Fixture.QueryDb(async db =>
        {
            await db.OutboxMessages.UpsertOutboxAsync(nameof(OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes), nameof(FailureHandler), _ => new TestMessage { Content = "fail" }, null, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxHandlerJob>();
        var reaper = scope.ServiceProvider.GetRequiredService<OutboxReaperJob>();

        // 1. Fail once
        await handler.HandlerJob(TestContext.Current.CancellationToken);
        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes));
            Assert.Equal(OutboxStatus.Failed, outbox.Status);
        });
        await reaper.ReaperJob(TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes));
            Assert.Equal(OutboxStatus.Pending, outbox.Status);
            Assert.Null(outbox.CompletedAt);
            Assert.Equal(1, outbox.Retries);
        });

        // 2. Fail twice
        await handler.HandlerJob(TestContext.Current.CancellationToken);
        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes));
            Assert.Equal(OutboxStatus.Failed, outbox.Status);
        });
        await reaper.ReaperJob(TestContext.Current.CancellationToken);
        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes));
            Assert.Equal(OutboxStatus.Pending, outbox.Status);
            Assert.Null(outbox.CompletedAt);
            Assert.Equal(2, outbox.Retries);
        });

        // 3. fail last time. Should not be set to pending again.
        await handler.HandlerJob(TestContext.Current.CancellationToken);
        await reaper.ReaperJob(TestContext.Current.CancellationToken);
        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_ProcessFailedJobs_RetriesAtLeastThreeTimes));
            Assert.Equal(OutboxStatus.Failed, outbox.Status);
        });
    }

    [Fact]
    public async Task OutboxMessage_RemoveOldJobs_DeleteAfterOneDayOldAsync()
    {
        await Fixture.QueryDb(async db =>
        {
            db.OutboxMessages.Add(new()
            {
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                Data = "{ }",
                Handler = nameof(OutboxMessage_RemoveOldJobs_DeleteAfterOneDayOldAsync),
                Status = OutboxStatus.Completed,
                StartedAt = DateTime.UtcNow.AddDays(-1),
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var reaper = scope.ServiceProvider.GetRequiredService<OutboxReaperJob>();
        await reaper.ReaperJob(TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_RemoveOldJobs_DeleteAfterOneDayOldAsync));
            Assert.Null(outbox);

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
    }
}
