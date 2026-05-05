using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Altinn.Authorization.Host.Pipeline.Builders;
using Altinn.Authorization.Host.Pipeline.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

// See: overhaul part-2 step 11
// xUnit1051 suggests passing TestContext.Current.CancellationToken to BlockingCollection.GetConsumingEnumerable() —
// not useful here since each test completes in <1s and the CTS is already wired through Run.
#pragma warning disable xUnit1051

namespace Altinn.Authorization.Host.Pipeline.Tests;

/// <summary>
/// Pure-unit tests for <see cref="PipelineSourceService"/>. Exercises async-enumerator
/// iteration, cancellation handling, exception paths, and the queue-completion contract
/// in <c>Run</c>'s finally block. The service is otherwise only exercised end-to-end
/// when a full pipeline runs in a host, so these tests pin behavior that would
/// otherwise regress silently.
/// </summary>
public class PipelineSourceServiceTest
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

    private static PipelineSourceService NewService(IServiceProvider? sp = null)
    {
        sp ??= new ServiceCollection().BuildServiceProvider();
        return new PipelineSourceService(NullLogger<PipelineSourceService>.Instance, sp);
    }

    private static PipelineArgs Args(string sourceName = "test-source", string pipelineName = "test-pipeline")
        => new() { Name = sourceName, Descriptor = new StubDescriptor(pipelineName) };

    private static async IAsyncEnumerable<T> Yield<T>(
        IEnumerable<T> values,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var v in values)
        {
            ct.ThrowIfCancellationRequested();
            yield return v;
        }

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<T> YieldThenThrow<T>(
        IEnumerable<T> prefix,
        Exception toThrow,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var v in prefix)
        {
            ct.ThrowIfCancellationRequested();
            yield return v;
        }

        await Task.CompletedTask;
        throw toThrow;
    }

    // ── Cancellation ───────────────────────────────────────────────────────────
    [Fact]
    public async Task PreCancelledToken_NoMessagesEmitted_QueueCompleted()
    {
        using var svc = new ServiceCollection().BuildServiceProvider();
        var sourceService = NewService(svc);
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        bool delegateInvoked = false;
        PipelineSource<int> source = (ctx, ct) =>
        {
            delegateInvoked = true;
            return Yield(new[] { 1, 2, 3 }, ct);
        };

        await sourceService.Run(Args(), source, outbound, cts);

        Assert.True(outbound.IsAddingCompleted);
        Assert.Empty(outbound.GetConsumingEnumerable());
        Assert.True(cts.Token.IsCancellationRequested);

        // Defensive — record whether the delegate ran at all (current behavior:
        // it is invoked once to obtain the enumerator before the cancellation
        // check fires; this test pins that contract).
        Assert.True(delegateInvoked);
    }

    // ── Happy path ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task NaturalEnumeratorTermination_AllMessagesEmitted_QueueCompleted()
    {
        var sourceService = NewService();
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();

        await sourceService.Run(Args(), (ctx, ct) => Yield(new[] { 10, 20, 30 }, ct), outbound, cts);

        var emitted = outbound.GetConsumingEnumerable().ToList();
        Assert.Equal([10, 20, 30], emitted.Select(m => m.Data));
        Assert.True(outbound.IsAddingCompleted);
        Assert.True(cts.Token.IsCancellationRequested); // Run's finally always cancels
    }

    [Fact]
    public async Task EmittedMessages_HaveStrictlyIncreasingSequence()
    {
        var sourceService = NewService();
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();

        await sourceService.Run(Args(), (ctx, ct) => Yield(new[] { 1, 2, 3 }, ct), outbound, cts);

        var sequences = outbound.GetConsumingEnumerable().Select(m => m.Sequence).ToList();
        Assert.Equal(3, sequences.Count);

        // Sequence is a static counter shared across the assembly, so absolute
        // values are order-dependent across tests; only assert monotonicity.
        Assert.True(sequences[1] > sequences[0]);
        Assert.True(sequences[2] > sequences[1]);
    }

    [Fact]
    public async Task EmittedMessages_CarryProvidedCancellationTokenSource()
    {
        var sourceService = NewService();
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();

        await sourceService.Run(Args(), (ctx, ct) => Yield(new[] { 1 }, ct), outbound, cts);

        var msg = outbound.GetConsumingEnumerable().Single();
        Assert.Same(cts, msg.CancellationTokenSource);
    }

    // ── Outbound queue closed mid-stream ───────────────────────────────────────
    [Fact]
    public async Task OutboundQueueAlreadyCompleted_BreaksOnFirstAdd_NoCrash()
    {
        var sourceService = NewService();
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        outbound.CompleteAdding(); // Force InvalidOperationException on the first Add
        using var cts = new CancellationTokenSource();

        await sourceService.Run(Args(), (ctx, ct) => Yield(new[] { 1, 2, 3 }, ct), outbound, cts);

        // Inner-loop catch handles the InvalidOperationException, breaks, and
        // Run's finally completes the queue + cancels the token.
        Assert.True(outbound.IsAddingCompleted);
        Assert.True(cts.Token.IsCancellationRequested);
    }

    // ── Source throws ─────────────────────────────────────────────────────────
    [Fact]
    public async Task SourceThrowsOperationCanceled_CaughtInOuterHandler_NoCrash()
    {
        var sourceService = NewService();
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();

        // Yield one value successfully, then throw OperationCanceledException —
        // exits via the outer `catch (OperationCanceledException)` branch.
        await sourceService.Run(
            Args(),
            (ctx, ct) => YieldThenThrow(new[] { 1 }, new OperationCanceledException("source cancel"), ct),
            outbound,
            cts);

        var emitted = outbound.GetConsumingEnumerable().ToList();
        Assert.Single(emitted);
        Assert.Equal(1, emitted[0].Data);
        Assert.True(outbound.IsAddingCompleted);
        Assert.True(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task SourceThrowsArbitraryException_CaughtInOuterHandler_NoCrash()
    {
        var sourceService = NewService();
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();

        // Yield two values, then throw a generic exception — exits via the outer
        // `catch (Exception)` branch which logs StreamingFailed + records telemetry.
        await sourceService.Run(
            Args(),
            (ctx, ct) => YieldThenThrow(new[] { 1, 2 }, new InvalidOperationException("source crash"), ct),
            outbound,
            cts);

        var emitted = outbound.GetConsumingEnumerable().ToList();
        Assert.Equal(2, emitted.Count);
        Assert.True(outbound.IsAddingCompleted);
        Assert.True(cts.Token.IsCancellationRequested);
    }

    // ── ServiceScope override ─────────────────────────────────────────────────
    [Fact]
    public async Task DescriptorWithCustomServiceScope_IsInvokedInsteadOfDefaultCreateScope()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sourceService = NewService(sp);
        var outbound = new BlockingCollection<PipelineMessage<int>>();
        using var cts = new CancellationTokenSource();

        bool customScopeInvoked = false;
        var args = new PipelineArgs
        {
            Name = "test-source",
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

        await sourceService.Run(args, (ctx, ct) => Yield(new[] { 1 }, ct), outbound, cts);

        Assert.True(customScopeInvoked);
        Assert.Single(outbound.GetConsumingEnumerable());
    }
}
