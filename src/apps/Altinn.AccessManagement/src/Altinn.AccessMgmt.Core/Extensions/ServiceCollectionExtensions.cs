using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.HostedServices;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Services;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessMgmtCore(this IServiceCollection services)
    {
        services.AddHostedService<RegisterHostedService>();
        services.AddScoped<IIngestService, IngestService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IDelegationService, DelegationService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IAuthorizedPartyRepoService, AuthorizedPartyRepoService>();

        services.AddScoped<IAmPartyRepository, AMPartyService>();
        services.AddScoped<IEntityService, EntityService>();

        AddJobs(services);
        return services;
    }

    private static void AddJobs(IServiceCollection services)
    {
        services.AddSingleton<IPartySyncService, PartySyncService>();
        services.AddSingleton<IRoleSyncService, RoleSyncService>();
        services.AddSingleton<IResourceSyncService, ResourceSyncService>();
    }
}
