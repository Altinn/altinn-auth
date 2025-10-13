namespace Altinn.Authorization.Host.Pipeline;

/// <summary>
/// Represents a group of related pipelines with shared configuration.
/// </summary>
internal class PipelineGroup : IPipelineGroup
{
    internal string GroupName { get; set; } = string.Empty;

    internal List<PipelineDescriptor> Builders { get; set; } = [];

    internal TimeSpan? Recurring { get; set; }

    internal string? FeatureFlag { get; set; }

    /// <inheritdoc/>
    public IPipelineGroup WithFeatureFlag(string featureFlag)
    {
        FeatureFlag = featureFlag;
        return this;
    }

    /// <inheritdoc/>
    public IPipelineGroup WithRecurring(TimeSpan recurring)
    {
        Recurring = recurring;
        return this;
    }

    /// <inheritdoc/>
    public IPipelineGroup WithGroupName(string name)
    {
        GroupName = name;
        return this;
    }

    /// <inheritdoc/>
    public IPipelineDescriptor AddPipeline(string name)
    {
        var builder = new PipelineDescriptor(this);
        Builders.Add(builder);
        return builder.WithName(name);
    }
}

/// <summary>
/// Interface for configuring a pipeline group.
/// </summary>
public interface IPipelineGroup
{
    /// <summary>
    /// Configures the group to execute pipelines on a recurring schedule.
    /// </summary>
    /// <param name="recurring">The interval between pipeline executions.</param>
    /// <returns>The pipeline group for chaining.</returns>
    IPipelineGroup WithRecurring(TimeSpan recurring);

    /// <summary>
    /// Sets the name of the pipeline group.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <returns>The pipeline group for chaining.</returns>
    IPipelineGroup WithGroupName(string name);

    /// <summary>
    /// Associates a feature flag with this pipeline group.
    /// </summary>
    /// <param name="featureFlag">The feature flag name.</param>
    /// <returns>The pipeline group for chaining.</returns>
    IPipelineGroup WithFeatureFlag(string featureFlag);

    /// <summary>
    /// Adds a new pipeline to the group.
    /// </summary>
    /// <param name="name">The pipeline name.</param>
    /// <returns>A pipeline descriptor to configure the pipeline.</returns>
    IPipelineDescriptor AddPipeline(string name);
}
