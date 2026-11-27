using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Resolvers.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Core.Resolvers;

/// <summary>
/// Resolves attributes for <see cref="BaseUrn.Altinn.Resource"/>
/// </summary>
public class AltinnResourceResolver : AttributeResolver
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// <see cref="AttributeResolver"/> runs leaves concurrently, and both leaves query the same scoped <see cref="AppDbContext"/>,
    /// which does not support concurrent operations. Queries are serialized through this lock.
    /// </summary>
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    /// <summary>
    /// ctor
    /// </summary>
    public AltinnResourceResolver(AppDbContext dbContext) : base(BaseUrn.Altinn.Resource.String())
    {
        AddLeaf([BaseUrn.Altinn.Resource.AppOwner, BaseUrn.Altinn.Resource.AppId], [BaseUrn.Altinn.Resource.Delegable, BaseUrn.Altinn.Resource.Type, BaseUrn.Altinn.Resource.ResourceRegistryId], ResolveAppOwnerAndAppId());
        AddLeaf([BaseUrn.Altinn.Resource.ResourceRegistryId], [BaseUrn.Altinn.Resource.Delegable, BaseUrn.Altinn.Resource.Type], ResolveResourceRegistryId());
        _dbContext = dbContext;
    }

    /// <summary>
    /// Resolves a resource if given <see cref="BaseUrn.Altinn.Resource.AppOwner"/> and <see cref="BaseUrn.Altinn.Resource.AppId"/>.
    /// Resource Registry requires the identifier of an app resource to be <c>app_{org}_{app}</c>, so the local resource
    /// is found by that identifier, case-insensitively, among resources of type <see cref="ResourceType.AltinnApp"/>
    /// or <see cref="ResourceType.MigratedApp"/>. Resource Registry builds app identifiers in lower case, so if identifiers
    /// differing only in case exist, the lower-case one is preferred and the rest are ordered by identifier.
    /// </summary>
    public LeafResolver ResolveAppOwnerAndAppId() => async (attributes, cancellationToken) =>
    {
        var org = attributes.GetRequiredString(BaseUrn.Altinn.Resource.AppOwner);
        var app = attributes.GetRequiredString(BaseUrn.Altinn.Resource.AppId);
        var resourceId = $"app_{org}_{app}".ToLowerInvariant();
        var altinnApp = nameof(ResourceType.AltinnApp).ToLowerInvariant();
        var migratedApp = nameof(ResourceType.MigratedApp).ToLowerInvariant();

        var resource = await Query(
            ct => _dbContext.Resources
                .AsNoTracking()
                .Where(r => r.RefId.ToLower() == resourceId)
                .Where(r => r.Type.Name.ToLower() == altinnApp || r.Type.Name.ToLower() == migratedApp)
                .OrderBy(r => r.RefId == resourceId ? 0 : 1)
                .ThenBy(r => r.RefId)
                .Select(r => new { r.RefId, r.Delegable, TypeName = r.Type.Name })
                .FirstOrDefaultAsync(ct),
            cancellationToken);

        if (resource is null || !TryMapResourceType(resource.TypeName, out var resourceType))
        {
            return [];
        }

        return
        [
            new(BaseUrn.Altinn.Resource.Delegable, resource.Delegable),
            new(BaseUrn.Altinn.Resource.Type, resourceType),
            new(BaseUrn.Altinn.Resource.ResourceRegistryId, resource.RefId),
        ];
    };

    /// <summary>
    /// Resolves a resource if given <see cref="BaseUrn.Altinn.Resource.ResourceRegistryId"/>. Delegable and type are read
    /// from the local resource, which is imported from the resource registry.
    /// </summary>
    public LeafResolver ResolveResourceRegistryId() => async (attributes, cancellationToken) =>
    {
        var resourceId = attributes.GetRequiredString(BaseUrn.Altinn.Resource.ResourceRegistryId);
        var resource = await Query(
            ct => _dbContext.Resources
                .AsNoTracking()
                .Where(r => r.RefId == resourceId)
                .Select(r => new { r.Delegable, TypeName = r.Type.Name })
                .FirstOrDefaultAsync(ct),
            cancellationToken);

        if (resource is null)
        {
            return [];
        }

        List<AttributeMatch> result = [new(BaseUrn.Altinn.Resource.Delegable, resource.Delegable)];
        if (TryMapResourceType(resource.TypeName, out var resourceType))
        {
            result.Add(new(BaseUrn.Altinn.Resource.Type, resourceType));
        }

        return result;
    };

    /// <summary>
    /// Runs a query on <see cref="_dbContext"/> while holding <see cref="_dbLock"/>.
    /// </summary>
    private async Task<T> Query<T>(Func<CancellationToken, Task<T>> query, CancellationToken cancellationToken)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            return await query(cancellationToken);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// The import stores the resource registry type by its enum name, so map it back to <see cref="ResourceType"/>.
    /// Names that are not a single defined type, such as numbers or flag combinations, are not mapped.
    /// </summary>
    private static bool TryMapResourceType(string name, out ResourceType resourceType)
    {
        if (!string.IsNullOrWhiteSpace(name)
            && char.IsAsciiLetter(name[0])
            && !name.Contains(',')
            && Enum.TryParse(name, ignoreCase: true, out resourceType)
            && Enum.IsDefined(resourceType))
        {
            return true;
        }

        resourceType = default;
        return false;
    }
}
