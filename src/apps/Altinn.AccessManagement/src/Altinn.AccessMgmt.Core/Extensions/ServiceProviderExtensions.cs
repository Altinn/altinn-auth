using Altinn.AccessMgmt.Core.Audit;
using Microsoft.AspNetCore.Builder;

namespace Altinn.AccessMgmt.Core.Extensions;

public static class ServiceProviderExtensions
{
    public static IApplicationBuilder UseEfAudit(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<AuditMiddleware>();
        return builder;
    }
}
