using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Authorization;
using Altinn.AccessMgmt.Core.HostedServices;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Outbox;
using Altinn.AccessMgmt.Core.HostedServices.Services;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AMPartyService = Altinn.AccessMgmt.Core.Services.AMPartyService;

namespace Altinn.AccessMgmt.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessMgmtCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<RegisterHostedService>();
        services.AddHostedService<AltinnRoleHostedService>();
        services.AddHostedService<SingleRightsHostedService>();
        services.AddHostedService<ConsentMigrationHostedService>();
        services.AddHostedService<OutboxHandlerJob>();
        services.AddHostedService<OutboxReaperJob>();
        services.AddScoped<RegisterHostedService>();
        services.AddScoped<IIngestService, IngestService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IDelegationService, DelegationService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IAmPartyRepository, AMPartyService>();
        services.AddScoped<IErrorQueueService, ErrorQueueService>();
        services.AddScoped<IRightImportProgressService, RightImportProgressService>();
        services.AddScoped<IAuthorizedPartyRepoServiceEf, AuthorizedPartyRepoServiceEf>();
        services.AddScoped<IClientDelegationService, ClientDelegationService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IAuthorizedPartiesService, AuthorizedPartiesServiceEf>();
        services.AddScoped<IConsentDelegationCheckService, ConsentDelegationCheckService>();

        services.AddScoped<IAuthorizationScopeProvider, DefaultAuthorizationScopeProvider>();
        services.AddScoped<IAuthorizationHandler, ScopeConditionAuthorizationHandler>();

        services.AddTransient<RequestApprovedNotificationHandler>();
        services.AddTransient<RequestPendingNotificationHandler>();

        services.AddSingleton<AuditMiddleware>();

        // Consent Migration - Configuration
        services.AddOptions<ConsentMigrationSettings>()
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .BindConfiguration("ConsentMigration");

        // Consent Migration - Services (Core - Scoped)
        services.AddScoped<IConsentMigrationService, ConsentMigrationService>();        

        AddJobs(services);
        return services;
    }

    private static void AddJobs(IServiceCollection services)
    {
        services.AddSingleton<IPartySyncService, PartySyncService>();
        services.AddSingleton<IRoleSyncService, RoleSyncService>();
        services.AddSingleton<IResourceSyncService, ResourceSyncService>();
        services.AddSingleton<IAltinnClientRoleSyncService, AltinnClientRoleSyncService>();
        services.AddSingleton<IPrivateTaxAffairRoleSyncService, PrivateTaxAffairRoleSyncService>();
        services.AddSingleton<IAltinnAdminRoleSyncService, AltinnAdminRoleSyncService>();
        services.AddSingleton<IAltinnBankruptcyEstateRoleSyncService, AltinnBankruptcyEstateRoleSyncService>();
        services.AddSingleton<IAllAltinnRoleSyncService, AllAltinnRoleSyncService>();
        services.AddSingleton<ISingleAppRightSyncService, SingleAppRightSyncService>();
        services.AddSingleton<ISingleResourceRegistryRightSyncService, SingleResourceRegistryRightSyncService>();
        services.AddSingleton<ISingleInstanceRightSyncService, SingleInstanceRightSyncService>();
        services.AddSingleton<IConsentMigrationSyncService, ConsentMigrationSyncService>();
    }
}
