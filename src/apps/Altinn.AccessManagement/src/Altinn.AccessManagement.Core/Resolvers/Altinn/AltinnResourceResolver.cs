using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Resolvers.Extensions;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Core.Resolvers;

/// <summary>
/// Resolves attributes for <see cref="BaseUrn.Altinn.Resource"/>
/// </summary>
public class AltinnResourceResolver : AttributeResolver
{
    private readonly IContextRetrievalService _contextRetrievalService;
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// ctor
    /// </summary>
    public AltinnResourceResolver(IContextRetrievalService contextRetrievalService, AppDbContext dbContext) : base(BaseUrn.Altinn.Resource.String())
    {
        AddLeaf([BaseUrn.Altinn.Resource.AppOwner, BaseUrn.Altinn.Resource.AppId], [BaseUrn.Altinn.Resource.Delegable, BaseUrn.Altinn.Resource.Type, BaseUrn.Altinn.Resource.ResourceRegistryId], ResolveAppOwnerAndAppId());
        AddLeaf([BaseUrn.Altinn.Resource.ResourceRegistryId], [BaseUrn.Altinn.Resource.Delegable, BaseUrn.Altinn.Resource.Type], ResolveResourceRegistryId());
        _contextRetrievalService = contextRetrievalService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Resolves a resource if given <see cref="BaseUrn.Altinn.Resource.AppOwner"/> and <see cref="BaseUrn.Altinn.Resource.AppId"/>
    /// </summary>
    public LeafResolver ResolveAppOwnerAndAppId() => async (attributes, cancellationToken) =>
    {
        var resource = await _contextRetrievalService.GetResourceFromResourceList(null, attributes.GetRequiredString(BaseUrn.Altinn.Resource.AppOwner), attributes.GetRequiredString(BaseUrn.Altinn.Resource.AppId), null, null);
        if (resource != null)
        {
            return
            [
                new(BaseUrn.Altinn.Resource.Delegable, resource.Delegable),
                new(BaseUrn.Altinn.Resource.Type, resource.ResourceType),
                new(BaseUrn.Altinn.Resource.ResourceRegistryId, resource.Identifier),
            ];
        }

        return [];
    };

    /// <summary>
    /// Resolves a resource if given <see cref="BaseUrn.Altinn.Resource.ResourceRegistryId"/>. Delegable and type are read
    /// from the local resource, which is imported from the resource registry.
    /// </summary>
    public LeafResolver ResolveResourceRegistryId() => async (attributes, cancellationToken) =>
    {
        var resourceId = attributes.GetRequiredString(BaseUrn.Altinn.Resource.ResourceRegistryId);
        var resource = await _dbContext.Resources
            .AsNoTracking()
            .Where(r => r.RefId == resourceId)
            .Select(r => new { r.Delegable, TypeName = r.Type.Name })
            .FirstOrDefaultAsync(cancellationToken);

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
