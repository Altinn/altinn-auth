using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.Persistence.Extensions;

public sealed class FeatureFlagLegacyRoutingPolicy : ILegacyRoutingPolicy
{
    private readonly IFeatureManager _features;

    public FeatureFlagLegacyRoutingPolicy(IFeatureManager features) => _features = features;

    public Task<bool> IsEnabledAsync(string feature, string key = "Enabled", string space = "AccessManagement", CancellationToken ct = default) 
        => _features.IsEnabledAsync($"{space}.{feature}.{key}");
}

public interface ILegacyRoutingPolicy
{
    Task<bool> IsEnabledAsync(string feature, string key = "Enabled", string space = "AccessManagement", CancellationToken ct = default);
}
