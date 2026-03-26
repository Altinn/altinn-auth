using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Extensions;

public static class FeatureManagerExtensions
{
    public static async Task<bool> IsDisabledAsync(this IFeatureManager featureManager, string flag, CancellationToken cancellationToken = default)
    {
        return !await featureManager.IsEnabledAsync(flag, cancellationToken);
    }
}
