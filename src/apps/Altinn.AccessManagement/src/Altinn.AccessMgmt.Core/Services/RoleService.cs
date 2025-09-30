using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public class RoleService(AppDbContext db) : IRoleService
{
    /// <inheritdoc />
    public async Task<RoleDto> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).SingleAsync(t => t.Id == id, cancellationToken);
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
        var roles = await db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).ToListAsync(cancellationToken);
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
        var roles = await db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).Where(t => t.ProviderId == providerId).ToListAsync(cancellationToken);
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
        var roles = await db.Roles.AsNoTracking().Include(t => t.Provider).Include(t => t.EntityType).Where(t => t.Code == code).ToListAsync(cancellationToken);
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
        var res = await db.RoleLookups.AsNoTracking().Where(t => t.Key.Contains(key) && t.Value.Contains(value))
            .Include(t => t.Role).ThenInclude(t => t.Provider)
            .Include(t => t.Role).ThenInclude(t => t.EntityType)
            .FirstOrDefaultAsync(cancellationToken);

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
        return await db.RoleLookups.AsNoTracking().Select(t => t.Key).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetRoleResources(Guid id, CancellationToken cancellationToken)
    {
        return await db.RoleResources.AsNoTracking().Where(t => t.RoleId == id).Include(t => t.Resource).Select(t => t.Resource).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetRolePackageResources(Guid id, CancellationToken cancellationToken)
    {
        var packages = await GetPackagesForRole(id, cancellationToken);
        return await db.PackageResources.AsNoTracking().Where(t => packages.Select(p => p.Id).Contains(t.PackageId)).Select(r => r.Resource).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RolePackageDto>> GetPackagesForRole(Guid id, CancellationToken cancellationToken = default)
    {
        var rolePackages = await db.RolePackages.AsNoTracking().Where(t => t.RoleId == id)
            .Include(t => t.Role)
            .Include(t => t.Package)
            .Include(t => t.EntityVariant)
            .ToListAsync(cancellationToken);

        if (rolePackages == null)
        {
            return null;
        }

        var roleDto = DtoMapper.Convert(rolePackages.First().Role);
        await GetSingleLegacyRoleCodeAndUrn(roleDto, cancellationToken);

        var rolePackageDtos = new List<RolePackageDto>();
        foreach (var rolePackage in rolePackages)
        {
            var rolePackageDto = DtoMapper.Convert(rolePackage);
            rolePackageDto.Role = roleDto;
            rolePackageDtos.Add(rolePackageDto);
        }

        return rolePackageDtos;
    }

    private async Task<IEnumerable<RoleDto>> GetLegacyRoleCodeAndUrn(IEnumerable<RoleDto> roles, CancellationToken cancellationToken = default)
    {
        var roleLookup = await db.RoleLookups.AsNoTracking().ToListAsync();
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
        var legacyRoleCode = await db.RoleLookups.AsNoTracking().Where(t => t.RoleId == extRole.Id && t.Key == "LegacyCode").FirstOrDefaultAsync();
        if (legacyRoleCode != null)
        {
            extRole.LegacyRoleCode = legacyRoleCode.Value;
            extRole.LegacyUrn = $"urn:altinn:rolecode:{legacyRoleCode.Value}";
        }
    }
}
