using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.HostedServices.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.HostedServices.Outbox;

/// <summary>
/// <see cref="OutboxHandlerJob"/>
/// </summary>
public class OutboxHandlerJobTest : IClassFixture<ApiFixture>
{
    public ApiFixture Fixture { get; }

    public OutboxHandlerJobTest(ApiFixture fixture)
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
    public async Task OutboxMessage_WithSuccessHandler_Success()
    {
        await Fixture.QueryDb(async db =>
        {
            await db.OutboxMessages.UpsertOutboxAsync(nameof(OutboxMessage_WithSuccessHandler_Success), nameof(SuccessHandler), _ => new TestMessage { Content = "ok" }, null, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<OutboxHandlerJob>();

        await job.HandlerJob(TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(f => f.RefId == nameof(OutboxMessage_WithSuccessHandler_Success));
            Assert.Equal(OutboxStatus.Completed, outbox.Status);
            Assert.NotNull(outbox.CompletedAt);
        });
    }

    [Fact]
    public async Task OutboxMessage_WithFailureHandler_Failure()
    {
        await Fixture.QueryDb(async db =>
        {
            await db.OutboxMessages.UpsertOutboxAsync(nameof(OutboxMessage_WithFailureHandler_Failure), nameof(FailureHandler), _ => new TestMessage { Content = "fail" }, null, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxHandlerJob>();

        await handler.HandlerJob(TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(o => o.RefId == nameof(OutboxMessage_WithFailureHandler_Failure));
            Assert.Equal(OutboxStatus.Failed, outbox.Status);
        });
    }

    [Fact]
    public async Task OutboxMessage_WithTimeoutHandler_Timedout()
    {
        await Fixture.QueryDb(async db =>
        {
            await db.OutboxMessages.UpsertOutboxAsync(
                nameof(OutboxMessage_WithTimeoutHandler_Timedout),
                nameof(TimeoutHandler),
                msg =>
                {
                    msg.Timeout = TimeSpan.FromSeconds(1);
                    return new TestMessage { Content = "timedout" };
                },
                null,
                TestContext.Current.CancellationToken);

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxHandlerJob>();

        await handler.HandlerJob(TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(o => o.RefId == nameof(OutboxMessage_WithTimeoutHandler_Timedout));
            Assert.Equal(OutboxStatus.TimedOut, outbox.Status);
        });
    }

    [Fact]
    public async Task OutboxMessage_WithTimeoutHandler_Interrrupted()
    {
        await Fixture.QueryDb(async db =>
        {
            await db.OutboxMessages.UpsertOutboxAsync(
                nameof(OutboxMessage_WithTimeoutHandler_Interrrupted),
                nameof(TimeoutHandler),
                msg =>
                {
                    msg.Timeout = TimeSpan.FromSeconds(2);
                    return new TestMessage { Content = "interrupted" };
                },
                null,
                TestContext.Current.CancellationToken);

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxHandlerJob>();

        using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await handler.HandlerJob(ct.Token);
        await Fixture.QueryDb(async db =>
        {
            var outbox = await db.OutboxMessages.FirstOrDefaultAsync(o => o.RefId == nameof(OutboxMessage_WithTimeoutHandler_Interrrupted));
            Assert.Equal(OutboxStatus.Interrupted, outbox.Status);
        });
    }
}

public class SuccessHandler : IOutboxHandler
{
    public Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        return Task.FromResult(OutboxStatus.Completed);
    }
}

public class FailureHandler : IOutboxHandler
{
    public Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        throw new ArgumentException("failure");
    }
}

public class TimeoutHandler : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        await Task.Delay(100_000_000, cancellationToken);
        return OutboxStatus.Completed;
    }
}

public class TestMessage
{
    public string Content { get; set; }
}
