using System.Diagnostics;

namespace Altinn.Authorization.Host.Pipeline.Tests;

/// <summary>
/// Smoke test for the freshly-scaffolded Altinn.Authorization.Host.Pipeline.Tests
/// project. Exercises the public <see cref="PipelineMessage{T}"/> record so the
/// xUnit v3 runner discovers at least one test in the assembly — avoids a
/// recurrence of the empty-test-project problem (audit ID C1') that this Task
/// just fixed for ABAC. Real Pipeline coverage tests follow under Phase D.1.
/// </summary>
public class PipelineMessageSmokeTest
{
    [Fact]
    public void PipelineMessage_Construction_PreservesRecordValues()
    {
        using var cts = new CancellationTokenSource();
        var activity = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);

        var message = new PipelineMessage<int>(42, activity, cts) { Sequence = 7 };

        Assert.Equal(42, message.Data);
        Assert.Equal(activity, message.ActivityContext);
        Assert.Same(cts, message.CancellationTokenSource);
        Assert.Equal(7UL, message.Sequence);
    }
}
