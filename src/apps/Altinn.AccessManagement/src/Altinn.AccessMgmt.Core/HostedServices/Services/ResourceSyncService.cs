using System.Diagnostics;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Telemetry;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <inheritdoc />
public partial class ResourceSyncService : IResourceSyncService
{
    private const string ResourceUrnPrefix = "urn:altinn:resource:";
    private readonly ILogger<ResourceSyncService> _logger;
    private readonly IAltinnResourceRegistry _resourceRegistry;
    private readonly IServiceProvider _serviceProvider;

    private KeyValuePair<string, object> OtelTags { get; } = new KeyValuePair<string, object?>("service", nameof(ResourceSyncService));

    /// <summary>
    /// Constructor
    /// </summary>
    public ResourceSyncService(
        IAltinnResourceRegistry resourceRegistry,
        IServiceProvider serviceProvider,
        ILogger<ResourceSyncService> logger)
    {
        _logger = logger;
        _resourceRegistry = resourceRegistry;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<bool> SyncResourceOwners(CancellationToken cancellationToken)
    {
        var serviceOwners = await _resourceRegistry.GetServiceOwners(cancellationToken);
        if (!serviceOwners.IsSuccessful)
        {
            Log.FailedToReadResourceOwners(_logger);
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
        var dbProviders = await dbContext.Providers.ToListAsync(cancellationToken);

        using var activity = CoreTelemetry.ActivitySource.CreateActivity(nameof(ResourceSyncService), ActivityKind.Internal);

        foreach (var serviceOwner in serviceOwners.Content.Orgs)
        {
            var provider = dbProviders.FirstOrDefault(provider => provider.Id == serviceOwner.Value.Id);
            if (provider is { })
            {
                if (provider.Name != serviceOwner.Value.Name.Nb ||
                   provider.LogoUrl != serviceOwner.Value.Logo ||
                   provider.RefId != serviceOwner.Value.Orgnr ||
                   provider.Code != serviceOwner.Key)
                {
                    provider.Name = serviceOwner.Value.Name.Nb;
                    provider.LogoUrl = serviceOwner.Value.Logo;
                    provider.RefId = serviceOwner.Value.Orgnr;
                    provider.Code = serviceOwner.Key;
                    dbContext.Providers.Update(provider);
                }
            }
            else
            {
                dbContext.Providers.Add(new Provider()
                {
                    Id = serviceOwner.Value.Id,
                    LogoUrl = serviceOwner.Value.Logo,
                    Name = serviceOwner.Value.Name.Nb,
                    RefId = serviceOwner.Value.Orgnr,
                    Code = serviceOwner.Key,
                    TypeId = ProviderTypeConstants.ServiceOwner,
                });
            }
        }

        await dbContext.SaveChangesAsync(new AuditValues(SystemEntityConstants.ResourceRegistryImportSystem), cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task SyncResources(ILease lease, CancellationToken cancellationToken)
    {
        var options = new AuditValues(SystemEntityConstants.ResourceRegistryImportSystem);
        using var scope = _serviceProvider.CreateEFScope(options);
        var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

        ResourceTypes = await dbContext.ResourceTypes.ToListAsync(cancellationToken);
        var leaseData = await lease.Get<ResourceRegistryLease>(cancellationToken);

        using var activity = CoreTelemetry.ActivitySource.StartActivity(nameof(ResourceSyncService), ActivityKind.Internal);
        await foreach (var page in await _resourceRegistry.StreamResources(leaseData.Since, leaseData.ResourceNextPageLink, cancellationToken))
        {
            try
            {
                if (page.IsProblem)
                {
                    Log.FailedToReadFromStream(_logger);
                    return;
                }

                foreach (var updatedResource in page.Content.Data)
                {
                    try
                    {
                        var resource = await UpsertResource(dbContext, updatedResource, cancellationToken);
                        if (resource is null)
                        {
                            continue;
                        }

                        if (updatedResource.Deleted)
                        {
                            await DeleteUpdatedSubject(dbContext, updatedResource, resource, cancellationToken);
                        }
                        else
                        {
                            await UpsertUpdatedSubject(dbContext, updatedResource, resource, cancellationToken);
                        }

                        leaseData.Since = updatedResource.UpdatedAt;
                    }
                    catch (Exception ex)
                    {
                        Log.FailedToWriteUpdateSubjectForResource(_logger, ex, updatedResource.SubjectUrn, updatedResource.ResourceUrn);
                        throw;
                    }
                }

                leaseData.ResourceNextPageLink = page.Content.Links.Next;
                await lease.Update(leaseData, cancellationToken);
            }
            catch (Exception ex)
            {
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error);
                CoreTelemetry.HostedServicesFailures.Add(1, OtelTags);
                CoreTelemetry.HostedServicesOk.Record(0, OtelTags);
                return;
            }
        }

        CoreTelemetry.HostedServicesSuccess.Add(1, OtelTags);
        CoreTelemetry.HostedServicesOk.Record(1, OtelTags);
    }

    private Task UpsertUpdatedSubject(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken) => updatedResource.SubjectUrn switch
    {
        var s when s.StartsWith("urn:altinn:rolecode:", StringComparison.OrdinalIgnoreCase) => UpsertRoleCodeResource(dbContext, updatedResource, resource, cancellationToken),
        var s when s.StartsWith("urn:altinn:accesspackage:", StringComparison.OrdinalIgnoreCase) => UpsertAccessPackageResource(dbContext, updatedResource, resource, cancellationToken),
        _ => Task.CompletedTask,
    };

    private Task DeleteUpdatedSubject(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken) => updatedResource.SubjectUrn switch
    {
        var s when s.StartsWith("urn:altinn:rolecode:", StringComparison.OrdinalIgnoreCase) => DeleteRoleCodeResource(dbContext, updatedResource, resource, cancellationToken),
        var s when s.StartsWith("urn:altinn:accesspackage:", StringComparison.OrdinalIgnoreCase) => DeleteAccessPackageResource(dbContext, updatedResource, resource, cancellationToken),
        _ => Task.CompletedTask,
    };

    private async Task DeleteRoleCodeResource(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .AsNoTracking()
            .Where(r => EF.Functions.ILike(r.LegacyUrn, updatedResource.SubjectUrn) || EF.Functions.ILike(r.Urn, updatedResource.SubjectUrn))
            .SingleOrDefaultAsync(cancellationToken);

        if (role is { })
        {
            var roleResource = await dbContext.RoleResources
                .Where(r => r.RoleId == role.Id && r.ResourceId == resource.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (roleResource is { })
            {
                dbContext.RoleResources.Remove(roleResource);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            Activity.Current?.AddTag("role", $"{updatedResource.SubjectUrn} does not exist.");
        }
    }

    private async Task DeleteAccessPackageResource(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken)
    {
        var packageResource = await dbContext.PackageResources
            .AsTracking()
            .Include(r => r.Package)
            .Where(r => r.Package.Urn == updatedResource.SubjectUrn && r.ResourceId == resource.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (packageResource is { })
        {
            dbContext.PackageResources.Remove(packageResource);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpsertAccessPackageResource(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken)
    {
        var package = await dbContext.Packages
            .AsNoTracking()
            .Where(p => p.Urn == updatedResource.SubjectUrn)
            .FirstOrDefaultAsync(cancellationToken);

        if (package is { })
        {
            var packageResource = await dbContext.PackageResources.FirstOrDefaultAsync(t => t.PackageId == package.Id && t.ResourceId == resource.Id, cancellationToken);
            if (packageResource == null)
            {
                packageResource = new PackageResource
                {
                    PackageId = package.Id,
                    ResourceId = resource.Id,
                };

                dbContext.PackageResources.Add(packageResource);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task UpsertRoleCodeResource(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken)
    {
        var subjectUrnPart = updatedResource.SubjectUrn.Split(":").Last();

        var role = await dbContext.Roles
            .AsNoTracking()
            .Where(r => EF.Functions.ILike(r.LegacyUrn, updatedResource.SubjectUrn) || EF.Functions.ILike(r.Urn, updatedResource.SubjectUrn))
            .FirstOrDefaultAsync(cancellationToken);
        
        if (role is { })
        {
            var roleResource = await dbContext.RoleResources.FirstOrDefaultAsync(t => t.RoleId == role.Id && t.ResourceId == resource.Id, cancellationToken);
            if (roleResource == null)
            {
                roleResource = new RoleResource
                {
                    RoleId = role.Id,
                    ResourceId = resource.Id,
                };

                dbContext.RoleResources.Add(roleResource);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            
            return;
        }
        else
        {
            Activity.Current?.AddTag("role", $"{updatedResource.SubjectUrn} does not exist.");
        }
    }

    private async Task<Resource?> UpsertResource(AppDbContext dbContext, ResourceUpdatedModel resourceUpdated, CancellationToken cancellationToken)
    {
        var identifier = ParseResourceIdentifier(resourceUpdated.ResourceUrn);
        var response = await _resourceRegistry.GetResource(identifier, cancellationToken: cancellationToken);
        if (response.IsProblem)
        {
            Log.FailedToGetResource(_logger, resourceUpdated.ResourceUrn);
            return null;
        }

        var convertedResource = await ConvertToResource(dbContext, response.Content, cancellationToken);
        if (convertedResource is null)
        {
            return null;
        }

        var resource = await dbContext.Resources.FirstOrDefaultAsync(t => t.RefId == convertedResource.RefId, cancellationToken);
        if (resource is null)
        {
            dbContext.Resources.Add(convertedResource);
            await dbContext.SaveChangesAsync(cancellationToken);
            return convertedResource;
        }

        resource.Name = convertedResource.Name;
        resource.Description = convertedResource.Description;
        resource.TypeId = convertedResource.TypeId;
        resource.ProviderId = convertedResource.ProviderId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return resource;
    }

    private static string ParseResourceIdentifier(string resourceUrn)
    {
        if (resourceUrn.StartsWith(ResourceUrnPrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            return resourceUrn[ResourceUrnPrefix.Length..];
        }

        Activity.Current?.AddTag("ResourceUrn", "Couldn't parse urn suffix");
        return string.Empty;
    }

    public IEnumerable<ResourceType> ResourceTypes { get; set; }

    private async Task<Resource> ConvertToResource(AppDbContext dbContext, ResourceModel model, CancellationToken cancellationToken)
    {
        var provider = await dbContext.Providers
        .AsNoTracking()
        .SingleOrDefaultAsync(p => p.Code == model.HasCompetentAuthority.Orgcode.ToLowerInvariant(), cancellationToken);

        if (provider is null)
        {
            return null;
        }

        var resourceType = await GetOrCreateResourceType(dbContext, model, cancellationToken) ?? throw new InvalidOperationException("Unable to get or create resourcetype");
        return new Resource()
        {
            Name = model.Title?.Nb ?? model.Identifier,
            Description = model.Description?.Nb ?? "-",
            RefId = model.Identifier,
            ProviderId = provider.Id,
            TypeId = resourceType.Id
        };
    }

    private async Task LoadResourceTypes(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        ResourceTypes = await dbContext.ResourceTypes.ToListAsync(cancellationToken);
    }

    private async Task<ResourceType> GetOrCreateResourceType(AppDbContext dbContext, ResourceModel model, CancellationToken cancellationToken)
    {
        if (ResourceTypes == null || !ResourceTypes.Any())
        {
            await LoadResourceTypes(dbContext, cancellationToken);
        }

        var type = ResourceTypes.FirstOrDefault(t => t.Name.Equals(model.ResourceType, StringComparison.OrdinalIgnoreCase));

        if (type == null)
        {
            type = new ResourceType()
            {
                Id = Guid.CreateVersion7(),
                Name = model.ResourceType
            };

            dbContext.ResourceTypes.Add(type);
            await dbContext.SaveChangesAsync(cancellationToken);

            await LoadResourceTypes(dbContext, cancellationToken);
        }

        return type;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unable to retrieve resource '{Resource}' from the resource registry.")]
        internal static partial void FailedToGetResource(ILogger logger, string resource);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to read stream of updated resources from the resource registry.")]
        internal static partial void FailedToReadFromStream(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to retrieve list of service owners from the resource registry.")]
        internal static partial void FailedToReadResourceOwners(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "failed to write update subject {SubjectUrn} for resource {ResourceId} .")]
        internal static partial void FailedToWriteUpdateSubjectForResource(ILogger logger, Exception ex, string subjectUrn, string resourceId);
    }
}
