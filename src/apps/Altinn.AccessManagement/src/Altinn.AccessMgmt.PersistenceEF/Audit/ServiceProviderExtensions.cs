using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Altinn.AccessMgmt.PersistenceEF.Audit;

public static class ServiceProviderExtensions
{
    public static IServiceScope CreateEFScope(this IServiceProvider provider, AuditValues auditValues)
    {
        var scope = provider.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IAuditAccessor>();
        accessor.AuditValues = auditValues;
        return scope;
    }

    public static IApplicationBuilder UseEfAudit(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<AuditMiddleware>();
        return builder;
    }
}
