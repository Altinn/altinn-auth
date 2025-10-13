using Altinn.Authorization.Host.Pipeline.Services;

namespace Altinn.Authorization.Host.Pipeline.Builders;

internal class PipelineSourceBuilder(PipelineGroup descriptor) : ISourceBuilder
{
    internal string Name { get; private set; }

    internal object Func { get; private set; }

    internal object Next { get; private set; }

    /// <inheritdoc/>
    public ISegmentBuilder<TOut> AddSource<TOut>(string name, PipelineSource<TOut> source)
    {
        var segment = new PipelineSegmentBuilder<TOut>(descriptor);
        Name = name;
        Func = source;
        Next = segment;
        return segment;
    }
}

/// <summary>
/// Builder for configuring the pipeline source stage.
/// Sources produce messages that flow through segments to the sink.
/// </summary>
public interface ISourceBuilder
{
    /// <summary>
    /// Configures the source function that produces messages.
    /// Sources stream messages asynchronously using <c>IAsyncEnumerable</c>.
    /// </summary>
    /// <typeparam name="TOutbound">The output message type.</typeparam>
    /// <param name="name">Name of the source.</param>
    /// <param name="source">Source function: yields messages asynchronously.</param>
    /// <returns>Builder to configure the next stage.</returns>
    ISegmentBuilder<TOutbound> AddSource<TOutbound>(string name, PipelineSource<TOutbound> source);
}
