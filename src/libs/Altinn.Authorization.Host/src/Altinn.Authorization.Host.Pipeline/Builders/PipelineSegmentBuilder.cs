using Altinn.Authorization.Host.Pipeline.Services;

namespace Altinn.Authorization.Host.Pipeline.Builders;

internal class PipelineSegmentBuilder<TIn>(
    PipelineGroup descriptor
    ) : ISegmentBuilder<TIn>
{
    internal string Name { get; private set; }
    
    internal object Func { get; private set; }
    
    internal object? Segment { get; private set; }
    
    internal object? Sink { get; private set; }

    /// <inheritdoc/>
    public ISegmentBuilder<TOut> AddSegment<TOut>(string name, PipelineSegment<TIn, TOut> segment)
    {
        var builder = new PipelineSegmentBuilder<TOut>(descriptor);

        Name = name;
        Func = segment;
        Segment = builder;
        return builder;
    }

    /// <inheritdoc/>
    public ISinkBuilder AddSink(string name, PipelineSink<TIn> sink)
    {
        var builder = new PipelineSinkBuilder(descriptor);

        Name = name;
        Func = sink;
        Sink = builder;
        return builder;
    }
}

/// <summary>
/// Builder for configuring pipeline segments (transformations) and sinks.
/// Segments transform messages: Source → Segment₁ → ... → Segmentₙ → Sink.
/// </summary>
/// <typeparam name="TInbound">The input message type.</typeparam>
public interface ISegmentBuilder<TInbound>
{
    /// <summary>
    /// Adds a transformation segment.
    /// Segments transform messages and are retried up to 3 times on failure.
    /// </summary>
    /// <typeparam name="TOutbound">The output message type.</typeparam>
    /// <param name="name">Name of the next segment.</param>
    /// <param name="segment">Transform function: receives <typeparamref name="TInbound"/>, returns <typeparamref name="TOutbound"/>.</param>
    /// <returns>Builder for the next stage.</returns>
    ISegmentBuilder<TOutbound> AddSegment<TOutbound>(string name, PipelineSegment<TInbound, TOutbound> segment);

    /// <summary>
    /// Adds the terminal sink stage.
    /// Sinks perform final processing (e.g., database writes, API calls) and are retried up to 3 times on failure.
    /// </summary>
    /// <param name="name">Name of the sink.</param>
    /// <param name="sink">Sink function: consumes <typeparamref name="TInbound"/> and performs final processing.</param>
    /// <returns>Builder to complete pipeline configuration.</returns>
    ISinkBuilder AddSink(string name, PipelineSink<TInbound> sink);
}
