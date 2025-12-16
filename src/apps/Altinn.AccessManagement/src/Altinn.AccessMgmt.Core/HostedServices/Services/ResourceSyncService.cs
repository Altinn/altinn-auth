using System.Diagnostics;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <inheritdoc />
public partial class ResourceSyncService : IResourceSyncService
{
    private readonly ILogger<ResourceSyncService> _logger;
    private readonly IAltinnResourceRegistry _resourceRegistry;
    private readonly IServiceProvider _serviceProvider;

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

        var options = new AuditValues(SystemEntityConstants.ResourceRegistryImportSystem);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();

        var resourceOwners = new List<Provider>();
        foreach (var serviceOwner in serviceOwners.Content.Orgs)
        {
            // Check if exists without Id => RefId/Code
            resourceOwners.Add(new Provider()
            {
                Id = serviceOwner.Value.Id,
                LogoUrl = serviceOwner.Value.Logo,
                Name = serviceOwner.Value.Name.Nb,
                RefId = serviceOwner.Value.Orgnr,
                Code = serviceOwner.Key,
                TypeId = ProviderTypeConstants.ServiceOwner,
            });
        }

        // IngestService will map in Id property and update properties not matchaed
        await ingestService.IngestAndMergeData(resourceOwners, options, ["Id"], cancellationToken: cancellationToken);

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

        await foreach (var page in await _resourceRegistry.StreamResources(leaseData.Since, leaseData.ResourceNextPageLink, cancellationToken))
        {
            if (page.IsProblem)
            {
                Log.FailedToReadFromStream(_logger);
                return;
            }

            foreach (var updatedResource in page.Content.Data)
            {
                leaseData.Since = updatedResource.UpdatedAt;
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
                }
                catch (Exception ex)
                {
                    Log.FailedToWriteUpdateSubjectForResource(_logger, ex, updatedResource.SubjectUrn, updatedResource.ResourceUrn);
                }
            }

            leaseData.ResourceNextPageLink = page.Content.Links.Next;
            await lease.Update(leaseData, cancellationToken);
        }
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
    }

    private async Task DeleteAccessPackageResource(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken)
    {
        var packageResource = await dbContext.PackageResources
            .AsTracking()
            .Include(r => r.Package)
            .Where(r => r.Package.Urn == updatedResource.SubjectUrn)
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
            .SingleOrDefaultAsync(cancellationToken) ?? throw new Exception(string.Format("Role not found '{0}'", subjectUrnPart));

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
    }

    private async Task<Resource?> UpsertResource(AppDbContext dbContext, ResourceUpdatedModel resourceUpdated, CancellationToken cancellationToken)
    {
        var response = await _resourceRegistry.GetResource(resourceUpdated.ResourceUrn.Split(":").Last(), cancellationToken: cancellationToken);
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

        var resource = await dbContext.Resources.FirstOrDefaultAsync(t => t.RefId == convertedResource.RefId && t.ProviderId == convertedResource.ProviderId, cancellationToken);
        if (resource is null)
        {
            dbContext.Resources.Add(convertedResource);
            await dbContext.SaveChangesAsync(cancellationToken);
            return convertedResource;
        }

        resource.Name = convertedResource.Name;
        resource.Description = convertedResource.Description;
        resource.TypeId = convertedResource.TypeId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return resource;
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

        var resourceType = await GetOrCreateResourceType(dbContext, model, cancellationToken) ?? throw new Exception("Unable to get or create resourcetype");
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
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unable to retrieve resource '{resource}' from the resource registry.")]
        internal static partial void FailedToGetResource(ILogger logger, string resource);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to read stream of updated resources from the resource registry.")]
        internal static partial void FailedToReadFromStream(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to retrieve list of service owners from the resource registry.")]
        internal static partial void FailedToReadResourceOwners(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "failed to write update subject {subjectUrn} for resource {resourceId} .")]
        internal static partial void FailedToWriteUpdateSubjectForResource(ILogger logger, Exception ex, string subjectUrn, string resourceId);
    }
}
