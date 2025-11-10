using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public class RoleService: IRoleService
{
    public RoleService(AppDbContext appDbContext)
    {
        Db = appDbContext;
    }

    public AppDbContext Db { get; }

    /// <inheritdoc />
    public async Task<RoleDto> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await Db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).SingleAsync(t => t.Id == id, cancellationToken);
        if (role == null)
        {
            return null;
        }

        var roleDto = DtoMapper.Convert(role);

        await GetSingleLegacyRoleCodeAndUrn(roleDto, cancellationToken);

        return roleDto;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RoleDto>> GetAll(CancellationToken cancellationToken = default)
    {
        var roles = await Db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).ToListAsync(cancellationToken);
        if (roles == null)
        {
            return null;
        }

        var roleDtos = roles.Select(DtoMapper.Convert);
        return await GetLegacyRoleCodeAndUrn(roleDtos, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByProvider(Guid providerId, CancellationToken cancellationToken = default)
    {
        var roles = await Db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).Where(t => t.ProviderId == providerId).ToListAsync(cancellationToken);
        if (roles == null)
        {
            return null;
        }

        var roleDtos = roles.Select(DtoMapper.Convert);
        return await GetLegacyRoleCodeAndUrn(roleDtos, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        var roles = await Db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).Where(t => t.Code == code).ToListAsync(cancellationToken);
        if (roles == null)
        {
            return null;
        }

        var roleDtos = roles.Select(DtoMapper.Convert);
        return await GetLegacyRoleCodeAndUrn(roleDtos, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RoleDto> GetByKeyValue(string key, string value, CancellationToken cancellationToken = default)
    {
        var res = (await Db.RoleLookups.AsNoTracking()
            .Include(t => t.Role).ThenInclude(t => t.Provider)
            .Include(t => t.Role).ThenInclude(t => t.EntityType)
            .ToListAsync(cancellationToken)
            ).FirstOrDefault(t => t.Key.Contains(key, StringComparison.OrdinalIgnoreCase) && t.Value.Contains(value, StringComparison.OrdinalIgnoreCase));

        if (res == null)
        {
            return null;
        }

        var roleDto = DtoMapper.Convert(res.Role);
        await GetSingleLegacyRoleCodeAndUrn(roleDto, cancellationToken);
        return roleDto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetLookupKeys(CancellationToken cancellationToken = default)
    {
        return await Db.RoleLookups.AsNoTracking().Select(t => t.Key).Distinct().ToListAsync(cancellationToken);
    }

    private async Task<IEnumerable<RoleDto>> GetLegacyRoleCodeAndUrn(IEnumerable<RoleDto> roles, CancellationToken cancellationToken = default)
    {
        var roleLookup = await Db.RoleLookups.AsNoTracking().ToListAsync();
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var legacyRoleCode = roleLookup.FirstOrDefault(x => x.RoleId == role.Id && x.Key == "LegacyCode");
            if (legacyRoleCode != null)
            {
                role.LegacyRoleCode = legacyRoleCode.Value;
                role.LegacyUrn = $"urn:altinn:rolecode:{legacyRoleCode.Value}";
            }

            roleDtos.Add(role);
        }

        return roleDtos;
    }

    private async Task GetSingleLegacyRoleCodeAndUrn(RoleDto extRole, CancellationToken cancellationToken = default)
    {
        var legacyRoleCode = await Db.RoleLookups.AsNoTracking().Where(t => t.RoleId == extRole.Id && t.Key == "LegacyCode").FirstOrDefaultAsync();
        if (legacyRoleCode != null)
        {
            extRole.LegacyRoleCode = legacyRoleCode.Value;
            extRole.LegacyUrn = $"urn:altinn:rolecode:{legacyRoleCode.Value}";
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PackageDto>> GetRolePackages(Guid id, Guid? variantId = null, bool includeResources = false, CancellationToken cancellationToken = default)
    {
        return await GetRolePackagesQuery(id, variantId, includeResources).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ResourceDto>> GetRoleResources(Guid id, Guid? variantId = null, bool includePackageResources = false, CancellationToken cancellationToken = default)
    {
        var roleResources = await Db.RoleResources.AsNoTracking()
            .Where(rr => rr.RoleId == id)
            .Join(
                Db.Resources,
                rr => rr.ResourceId,
                r => r.Id,
                (rr, r) => DtoMapper.Convert(r))
            .ToListAsync(cancellationToken);

        if (!includePackageResources)
        {
            return roleResources;
        }

        var packageResources = await GetRolePackagesQuery(id, variantId, true)
            .SelectMany(p => p.Resources)
            .ToListAsync(cancellationToken);

        return roleResources.Concat(packageResources).DistinctBy(r => r.Id);
    }
    
    private IQueryable<PackageDto> GetRolePackagesQuery(Guid roleId, Guid? variantId = null, bool includeResources = false)
    {
        var rolePackages = Db.RolePackages.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .WhereIf(!variantId.HasValue, rp => rp.EntityVariantId == null)
            .WhereIf(variantId.HasValue, rp => (rp.EntityVariantId == null || rp.EntityVariantId == variantId.Value))
            .Join(Db.Packages, rp => rp.PackageId, p => p.Id, (rp, p) => p);

        if (!includeResources)
        {
            return rolePackages.Select(p => new PackageDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                IsDelegable = p.IsDelegable,
                IsAssignable = p.IsAssignable,
                Urn = p.Urn
            });
        }

        // Når vi skal ha med ressurser:
        return rolePackages
            .GroupJoin(
                Db.PackageResources, 
                package => package.Id, 
                pr => pr.PackageId,
                (package, packageResources) => new PackageDto
                {
                    Id = package.Id,
                    Name = package.Name,
                    Description = package.Description,
                    IsDelegable = package.IsDelegable,
                    IsAssignable = package.IsAssignable,
                    Urn = package.Urn,
                    Resources = packageResources
                        .Join(
                            Db.Resources,
                            pr => pr.ResourceId,
                            r => r.Id,
                            (pr, r) => DtoMapper.Convert(r))
                        .DistinctBy(r => r.Id)
                        .ToList()
                });
    }
}
