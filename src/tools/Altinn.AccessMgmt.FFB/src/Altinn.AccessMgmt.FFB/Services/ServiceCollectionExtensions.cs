namespace Altinn.AccessMgmt.FFB.Services;

/// <summary>
/// Registers the page-facing service layer by convention.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly string[] ServiceNamespaces =
    [
        "Altinn.AccessMgmt.FFB.Services.Tools",
        "Altinn.AccessMgmt.FFB.Services.PageData",
    ];

    /// <summary>
    /// Registers every non-static class named *Service under Services/Tools and
    /// Services/PageData as a singleton. New services in those folders need no
    /// Program.cs entry — they are picked up here. Singleton is safe because the
    /// services are stateless and create their own DbContext per method call.
    /// </summary>
    public static IServiceCollection AddPageServices(this IServiceCollection services)
    {
        var serviceTypes = typeof(ServiceCollectionExtensions).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && t.Name.EndsWith("Service", StringComparison.Ordinal)
                && ServiceNamespaces.Contains(t.Namespace, StringComparer.Ordinal));

        foreach (var serviceType in serviceTypes)
        {
            services.AddSingleton(serviceType);
        }

        return services;
    }
}
