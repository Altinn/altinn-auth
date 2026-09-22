using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Extensions;

public static class ServiceProviderExtensions
{
    public static IApplicationBuilder UseEfAudit(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<AuditMiddleware>();
        return builder;
    }

    /// <summary>
    /// Resolves feature toggle values once at startup and stores them in the host-scoped
    /// <see cref="AppLifecycleFeatures"/> singleton, so they can be read without evaluating the
    /// feature manager on every request.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    public static async Task InitializeAppLifecycleFeaturesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();
        var lifecycleFeatures = scope.ServiceProvider.GetRequiredService<AppLifecycleFeatures>();

        lifecycleFeatures.AdosSubunitInheritance = await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AdosSubunitInheritance);
    }
}
