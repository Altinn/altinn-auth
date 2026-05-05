using System.Collections.Concurrent;
using Altinn.Authorization.Host.Pipeline.Builders;
using Altinn.Authorization.Host.Pipeline.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

// See: overhaul part-2 step 12
#pragma warning disable xUnit1051

namespace Altinn.Authorization.Host.Pipeline.Tests;

/// <summary>
/// Pure-unit tests for <see cref="PipelineSinkService"/>. Pins the retry
/// semantics of <c>DispatchSegment</c> (3 attempts, exponential backoff,
/// InvalidOperationException after exhaustion) and the outer-handler /
/// finally-block contract in <c>Run</c>.
/// </summary>
public class PipelineSinkServiceTest
{
    private sealed class StubDescriptor(string? pipelineName) : IPipelineDescriptor
    {
        public string? Name { get; } = pipelineName;

        public Func<IServiceProvider, IServiceScope>? ServiceScope { get; set; }

        public IPipelineDescriptor WithName(string name) => throw new NotSupportedException();

        public IPipelineDescriptor WithLease(string lease) => throw new NotSupportedException();

        public IPipelineDescriptor WithServiceScope(Func<IServiceProvider, IServiceScope> func) => throw new NotSupportedException();

        public ISourceBuilder WithStages() => throw new NotSupportedException();
    }

    private static PipelineSinkService NewService(IServiceProvider? sp = null)
    {
        sp ??= new ServiceCollection().BuildServiceProvider();
        return new PipelineSinkService(NullLogger<PipelineSinkService>.Instance, sp);
    }

    private static PipelineArgs Args(string sinkName = "test-sink", string pipelineName = "test-pipeline")
        => new() { Name = sinkName, Descriptor = new StubDescriptor(pipelineName) };

    private static PipelineMessage<int> Message(int data, ulong sequence = 1)
        => new(data, null, new CancellationTokenSource()) { Sequence = sequence };

    private static BlockingCollection<PipelineMessage<int>> Inbound(params PipelineMessage<int>[] messages)
    {
        var queue = new BlockingCollection<PipelineMessage<int>>();
        foreach (var m in messages)
        {
            queue.Add(m);
        }

        queue.CompleteAdding();
        return queue;
    }

    // ── Empty input ────────────────────────────────────────────────────────────
    [Fact]
    public async Task EmptyInbound_SinkDelegateNeverInvoked()
    {
        var svc = NewService();
        var inbound = Inbound();

        bool invoked = false;
        PipelineSink<int> sink = ctx =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        await svc.Run(Args(), sink, inbound);

        Assert.False(invoked);
    }

    // ── Happy path ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task SuccessOnFirstAttempt_NoRetry()
    {
        var svc = NewService();
        var msg = Message(42);
        var inbound = Inbound(msg);

        int callCount = 0;
        int? observed = null;
        PipelineSink<int> sink = ctx =>
        {
            callCount++;
            observed = ctx.Data;
            return Task.CompletedTask;
        };

        await svc.Run(Args(), sink, inbound);

        Assert.Equal(1, callCount);
        Assert.Equal(42, observed);
        Assert.False(msg.CancellationTokenSource.IsCancellationRequested);
    }

    [Fact]
    public async Task MultipleMessages_AllProcessedInOrder()
    {
        var svc = NewService();
        var inbound = Inbound(Message(1, 1), Message(2, 2), Message(3, 3));

        var observed = new List<int>();
        PipelineSink<int> sink = ctx =>
        {
            observed.Add(ctx.Data);
            return Task.CompletedTask;
        };

        await svc.Run(Args(), sink, inbound);

        Assert.Equal([1, 2, 3], observed);
    }

    // ── Retry semantics (slow tests due to hardcoded Task.Delay backoff) ──────
    [Fact]
    public async Task SuccessOnSecondAttempt_RetriedOnce()
    {
        var svc = NewService();
        var msg = Message(7);
        var inbound = Inbound(msg);

        int callCount = 0;
        PipelineSink<int> sink = ctx =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new InvalidOperationException("transient");
            }

            return Task.CompletedTask;
        };

        await svc.Run(Args(), sink, inbound);

        Assert.Equal(2, callCount);
        Assert.False(msg.CancellationTokenSource.IsCancellationRequested);
    }

    [Fact]
    public async Task SuccessOnThirdAttempt_RetriedTwice()
    {
        var svc = NewService();
        var msg = Message(11);
        var inbound = Inbound(msg);

        int callCount = 0;
        PipelineSink<int> sink = ctx =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new InvalidOperationException("transient");
            }

            return Task.CompletedTask;
        };

        await svc.Run(Args(), sink, inbound);

        Assert.Equal(3, callCount);
        Assert.False(msg.CancellationTokenSource.IsCancellationRequested);
    }

    [Fact]
    public async Task AllAttemptsFail_CtsCancelled_RunDoesNotPropagate()
    {
        var svc = NewService();
        var msg = Message(99);
        var inbound = Inbound(msg);

        int callCount = 0;
        PipelineSink<int> sink = ctx =>
        {
            callCount++;
            throw new InvalidOperationException($"attempt {callCount} fails");
        };

        // Run catches the InvalidOperationException emitted by DispatchSegment after retry exhaustion;
        // the message's CancellationTokenSource is cancelled to signal upstream stages.
        await svc.Run(Args(), sink, inbound);

        Assert.Equal(3, callCount);
        Assert.True(msg.CancellationTokenSource.IsCancellationRequested);
    }

    [Fact]
    public async Task RetryExhaustion_StopsConsumingAfterFirstAbortedMessage()
    {
        var svc = NewService();
        var msg1 = Message(1, 1);
        var msg2 = Message(2, 2);
        var inbound = Inbound(msg1, msg2);

        var observed = new List<int>();
        PipelineSink<int> sink = ctx =>
        {
            observed.Add(ctx.Data);
            throw new InvalidOperationException("always fails");
        };

        await svc.Run(Args(), sink, inbound);

        // EnumerateSink returns after the first message's retries are exhausted;
        // msg2 is left unconsumed in the queue. Pin: 3 attempts on msg1, 0 on msg2.
        Assert.Equal(3, observed.Count);
        Assert.All(observed, v => Assert.Equal(1, v));
        Assert.True(msg1.CancellationTokenSource.IsCancellationRequested);
        Assert.False(msg2.CancellationTokenSource.IsCancellationRequested);
    }

    // ── Service scope override ────────────────────────────────────────────────
    [Fact]
    public async Task DescriptorWithCustomServiceScope_IsInvokedInsteadOfDefaultCreateScope()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var svc = NewService(sp);
        var inbound = Inbound(Message(1));

        bool customScopeInvoked = false;
        var args = new PipelineArgs
        {
            Name = "test-sink",
            Descriptor = new StubDescriptor("test-pipeline")
            {
                ServiceScope = providedSp =>
                {
                    customScopeInvoked = true;
                    Assert.Same(sp, providedSp);
                    return providedSp.CreateScope();
                },
            },
        };

        await svc.Run(args, _ => Task.CompletedTask, inbound);

        Assert.True(customScopeInvoked);
    }
}
