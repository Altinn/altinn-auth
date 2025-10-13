using System.Diagnostics;

namespace Altinn.Authorization.Host.Pipeline;

/// <summary>
/// Internal message wrapper for pipeline data flow with telemetry and cancellation support.
/// </summary>
/// <typeparam name="T">The message data type.</typeparam>
/// <param name="Data">The message data.</param>
/// <param name="ActivityContext">The telemetry activity context for distributed tracing.</param>
/// <param name="CancellationTokenSource">The cancellation token source for this message.</param>
public record PipelineMessage<T>(
    T Data,
    ActivityContext? ActivityContext,
    CancellationTokenSource CancellationTokenSource)
{
    public ulong Sequence { get; init; }
}
