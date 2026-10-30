using System.Diagnostics;
using System.Net;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Telemetry;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Enums;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <inheritdoc />
public partial class ResourceSyncService : IResourceSyncService
{
    private const string ResourceUrnPrefix = "urn:altinn:resource:";
    private const string MaskinportenSchemaResourceType = "MaskinportenSchema";
    private const string MaskinportenScopeReferenceType = "MaskinportenScope";
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

        // Phase 1 (Fetch resources): resource metadata from the change feed. Runs first so that resources referenced by
        // subject relations in phase 2 already exist, and phase 2 is skipped when it does not complete.
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();
        var useChangeFeed = await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesResourceRegistrySyncChangeFeed, cancellationToken);
        if (useChangeFeed && !await SyncResourceChanges(dbContext, lease, leaseData, activity, cancellationToken))
        {
            return;
        }

        // Phase 2 (Fetch policies): subject relations (role and access package) from the updated feed. With the change feed,
        // resources are usually synced by phase 1 and only looked up here; without it, they are upserted.
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
                        var resource = useChangeFeed
                            ? await FindOrFetchResource(dbContext, updatedResource, cancellationToken)
                            : await UpsertResource(dbContext, updatedResource, cancellationToken);
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

    /// <summary>
    /// Drains the resource change feed and upserts each changed resource. The position is stored in the lease
    /// after each page. A page without a next link means the feed has no more changes yet, so the stored
    /// link is kept and the next run resumes from it.
    /// </summary>
    /// <remarks>
    /// Deleted resources are not handled. The change feed leaves them out, and deleting a resource in the
    /// resource registry does not mark its subjects deleted in the updated feed either, so a deleted resource
    /// and its role and access package links stay in the database. Exposing deletes in the change feed is an
    /// open question in https://github.com/Altinn/altinn-auth/issues/4256, and handling them here is tracked in
    /// https://github.com/Altinn/altinn-auth/issues/4621.
    /// </remarks>
    /// <returns>true when the feed was drained; false when it stopped on an error and phase 2 should not run.</returns>
    private async Task<bool> SyncResourceChanges(AppDbContext dbContext, ILease lease, ResourceRegistryLease leaseData, Activity? activity, CancellationToken cancellationToken)
    {
        try
        {
            // The try covers the whole enumeration, since opening the feed and fetching later pages can throw too.
            var pages = await _resourceRegistry.StreamResourceChanges(leaseData.ResourceChangesNextPageLink, cancellationToken);
            await foreach (var page in pages.WithCancellation(cancellationToken))
            {
                if (page.IsProblem)
                {
                    Log.FailedToReadFromChangeStream(_logger);
                    RecordFailure(activity);
                    return false;
                }

                foreach (var change in page.Content.Data)
                {
                    try
                    {
                        var response = await _resourceRegistry.GetResource(change.ResourceId, cancellationToken: cancellationToken);
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            // Deleted after the change was listed; the feed does not surface deletes.
                            Log.ChangedResourceNotFound(_logger, change.ResourceId);
                            continue;
                        }

                        if (response.IsProblem)
                        {
                            // Stop without moving the cursor, so the page is retried on the next run.
                            Log.FailedToGetResource(_logger, change.ResourceId);
                            RecordFailure(activity);
                            return false;
                        }

                        // An unknown service owner is skipped rather than retried, as in the updated feed: blocking
                        // here would stop the feed until the owner exists, which may never happen.
                        if (await UpsertResource(dbContext, response.Content, cancellationToken) is null)
                        {
                            Log.ChangedResourceNotSynced(_logger, change.ResourceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.FailedToUpsertChangedResource(_logger, ex, change.ResourceId);
                        throw;
                    }
                }

                if (!string.IsNullOrEmpty(page.Content.Links.Next))
                {
                    leaseData.ResourceChangesNextPageLink = page.Content.Links.Next;
                    await lease.Update(leaseData, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            RecordFailure(activity);
            return false;
        }

        return true;
    }

    private void RecordFailure(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        CoreTelemetry.HostedServicesFailures.Add(1, OtelTags);
        CoreTelemetry.HostedServicesOk.Record(0, OtelTags);
    }

    private Task UpsertUpdatedSubject(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken) => updatedResource.SubjectUrn switch
    {
        var s when s.StartsWith("urn:altinn:rolecode:", StringComparison.OrdinalIgnoreCase) => UpsertRoleCodeResource(dbContext, updatedResource, resource, cancellationToken),
        var s when s.StartsWith("urn:altinn:role:", StringComparison.OrdinalIgnoreCase) => UpsertRoleCodeResource(dbContext, updatedResource, resource, cancellationToken),
        var s when s.StartsWith("urn:altinn:accesspackage:", StringComparison.OrdinalIgnoreCase) => UpsertAccessPackageResource(dbContext, updatedResource, resource, cancellationToken),
        _ => Task.CompletedTask,
    };

    private Task DeleteUpdatedSubject(AppDbContext dbContext, ResourceUpdatedModel updatedResource, Resource resource, CancellationToken cancellationToken) => updatedResource.SubjectUrn switch
    {
        var s when s.StartsWith("urn:altinn:rolecode:", StringComparison.OrdinalIgnoreCase) => DeleteRoleCodeResource(dbContext, updatedResource, resource, cancellationToken),
        var s when s.StartsWith("urn:altinn:role:", StringComparison.OrdinalIgnoreCase) => DeleteRoleCodeResource(dbContext, updatedResource, resource, cancellationToken),
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

        return await UpsertResource(dbContext, response.Content, cancellationToken);
    }

    /// <summary>
    /// Looks up a resource that phase 1 has synced from the change feed, and fetches it when it is missing.
    /// The two feeds do not share a snapshot, so a resource published after phase 1 drained can already have
    /// subject relations in the updated feed. Skipping those would lose them: the cursor moves past them, and
    /// importing the resource on a later run does not replay its relations.
    /// </summary>
    /// <remarks>
    /// A missing resource is skipped when the relation is a delete, since no link can exist; when the resource
    /// registry no longer has it; and when its service owner is unknown, as in phase 1. Any other failure to
    /// fetch it throws, so the page is not checkpointed and is retried on the next run.
    /// If an app exist only in storage and not in Resource-Registry it will be created the first time we fetch the policy
    /// but it will never be updated if owner want it to be updated it must be published to ResourceRegistry
    /// </remarks>
    private async Task<Resource?> FindOrFetchResource(AppDbContext dbContext, ResourceUpdatedModel resourceUpdated, CancellationToken cancellationToken)
    {
        var identifier = ParseResourceIdentifier(resourceUpdated.ResourceUrn);
        if (string.IsNullOrEmpty(identifier))
        {
            return null;
        }

        var resource = await dbContext.Resources.FirstOrDefaultAsync(t => t.RefId == identifier, cancellationToken);
        if (resource is { } || resourceUpdated.Deleted)
        {
            return resource;
        }

        Log.FetchingResourceNotSynced(_logger, resourceUpdated.ResourceUrn, resourceUpdated.SubjectUrn);
        var response = await _resourceRegistry.GetResource(identifier, cancellationToken: cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Log.SubjectResourceNotFound(_logger, resourceUpdated.ResourceUrn, resourceUpdated.SubjectUrn);
            return null;
        }

        if (response.IsProblem)
        {
            Log.FailedToGetResource(_logger, resourceUpdated.ResourceUrn);
            throw new InvalidOperationException($"Unable to retrieve resource '{resourceUpdated.ResourceUrn}' from the resource registry.");
        }

        resource = await UpsertResource(dbContext, response.Content, cancellationToken);
        if (resource is null)
        {
            Log.SubjectResourceNotSynced(_logger, resourceUpdated.ResourceUrn, resourceUpdated.SubjectUrn);
        }

        return resource;
    }

    private async Task<Resource?> UpsertResource(AppDbContext dbContext, ResourceModel model, CancellationToken cancellationToken)
    {
        var convertedResource = await ConvertToResource(dbContext, model, cancellationToken);
        if (convertedResource is null)
        {
            return null;
        }

        var resource = await dbContext.Resources
            .Include(t => t.Scopes)
            .FirstOrDefaultAsync(t => t.RefId == convertedResource.RefId, cancellationToken);
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
        resource.Delegable = convertedResource.Delegable;
        resource.AccessListMode = convertedResource.AccessListMode;

        var incomingScopes = convertedResource.Scopes.Select(s => s.Scope).ToHashSet(StringComparer.Ordinal);
        var existingScopes = resource.Scopes.Select(s => s.Scope).ToHashSet(StringComparer.Ordinal);
        foreach (var removed in resource.Scopes.Where(s => !incomingScopes.Contains(s.Scope)).ToList())
        {
            dbContext.ResourceScopes.Remove(removed);
        }

        foreach (var added in incomingScopes.Where(s => !existingScopes.Contains(s)))
        {
            dbContext.ResourceScopes.Add(new ResourceScope { ResourceId = resource.Id, Scope = added });
        }

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
        var resource = new Resource()
        {
            Name = model.Title?.Nb ?? model.Identifier,
            Description = model.Description?.Nb ?? "-",
            RefId = model.Identifier,
            ProviderId = provider.Id,
            TypeId = resourceType.Id,
            Delegable = model.Delegable,
            AccessListMode = ParseAccessListMode(model),
        };

        foreach (var scope in GetMaskinportenScopes(model))
        {
            resource.Scopes.Add(new ResourceScope { ResourceId = resource.Id, Scope = scope });
        }

        return resource;
    }

    /// <summary>
    /// The resource registry defaults to <see cref="ResourceAccessListMode.Disabled"/>, so a missing value is disabled too.
    /// An unknown value is also stored as disabled, rather than stopping the sync, and is logged.
    /// </summary>
    private ResourceAccessListMode ParseAccessListMode(ResourceModel model)
    {
        if (string.IsNullOrEmpty(model.AccessListMode))
        {
            return ResourceAccessListMode.Disabled;
        }

        if (Enum.TryParse<ResourceAccessListMode>(model.AccessListMode, ignoreCase: true, out var mode) && Enum.IsDefined(mode))
        {
            return mode;
        }

        Log.UnknownAccessListMode(_logger, model.AccessListMode, model.Identifier);
        return ResourceAccessListMode.Disabled;
    }

    /// <summary>
    /// Maskinporten scopes only apply to Maskinporten schemas, so other resource types get none even when they have scope references.
    /// </summary>
    private static IEnumerable<string> GetMaskinportenScopes(ResourceModel model)
    {
        if (!string.Equals(model.ResourceType, MaskinportenSchemaResourceType, StringComparison.OrdinalIgnoreCase) || model.ResourceReferences is null)
        {
            return [];
        }

        return model.ResourceReferences
            .Where(r => string.Equals(r.ReferenceType, MaskinportenScopeReferenceType, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(r.Reference))
            .Select(r => r.Reference)
            .Distinct(StringComparer.Ordinal);
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

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to read stream of changed resources from the resource registry.")]
        internal static partial void FailedToReadFromChangeStream(ILogger logger);

        [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Changed resource '{ResourceId}' no longer exists in the resource registry and was skipped.")]
        internal static partial void ChangedResourceNotFound(ILogger logger, string resourceId);

        [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Failed to upsert changed resource '{ResourceId}'.")]
        internal static partial void FailedToUpsertChangedResource(ILogger logger, Exception ex, string resourceId);

        [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Resource '{ResourceUrn}' could not be synced, for example because its service owner is unknown, so subject {SubjectUrn} was skipped.")]
        internal static partial void SubjectResourceNotSynced(ILogger logger, string resourceUrn, string subjectUrn);

        [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Resource '{ResourceUrn}' for subject {SubjectUrn} was not synced from the change feed and is fetched from the resource registry.")]
        internal static partial void FetchingResourceNotSynced(ILogger logger, string resourceUrn, string subjectUrn);

        [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Resource '{ResourceUrn}' no longer exists in the resource registry, so subject {SubjectUrn} was skipped.")]
        internal static partial void SubjectResourceNotFound(ILogger logger, string resourceUrn, string subjectUrn);

        [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Changed resource '{ResourceId}' could not be synced, for example because its service owner is unknown, and was skipped.")]
        internal static partial void ChangedResourceNotSynced(ILogger logger, string resourceId);

        [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Unknown access list mode '{AccessListMode}' on resource '{ResourceId}' was stored as Disabled.")]
        internal static partial void UnknownAccessListMode(ILogger logger, string accessListMode, string resourceId);
    }
}
