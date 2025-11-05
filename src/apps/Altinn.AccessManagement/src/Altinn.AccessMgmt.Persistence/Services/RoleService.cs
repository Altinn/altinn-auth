using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.AccessMgmt.Repo.Definitions;
using Authorization.Platform.Authorization.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json.Linq;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class RoleService(IRoleRepository roleRepository, IRoleLookupRepository roleLookupRepository, IRolePackageRepository rolePackageRepository) : IRoleService
{
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IRoleLookupRepository roleLookupRepository = roleLookupRepository;
    private readonly IRolePackageRepository rolePackageRepository = rolePackageRepository;

    /// <inheritdoc />
    public async Task<RoleDto> GetById(Guid id)
    {
        ExtRole extRole = await roleRepository.GetExtended(id);
        if (extRole == null)
        {
            return null;
        }

        var roleDto = new RoleDto(extRole);

        await GetSingleLegacyRoleCodeAndUrn(roleDto);

        return roleDto;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RoleDto>> GetAll()
    {
        var roles = await roleRepository.GetExtended();
        if (roles == null)
        {
            return null;
        }

        var roleDtos = roles.Select(t => new RoleDto(t));
        return await GetLegacyRoleCodeAndUrn(roleDtos);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByProvider(Guid providerId)
    {
        var roles = await roleRepository.GetExtended(t => t.ProviderId, providerId);
        if (roles == null)
        {
            return null;
        }

        var roleDtos = roles.Select(t => new RoleDto(t));
        return await GetLegacyRoleCodeAndUrn(roleDtos);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByCode(string code)
    {
        var roles = await roleRepository.GetExtended(t => t.Code, code);
        if (roles == null)
        {
            return null;
        }

        var roleDtos = roles.Select(t => new RoleDto(t));
        return await GetLegacyRoleCodeAndUrn(roleDtos);
    }

    /// <inheritdoc />
    public async Task<RoleDto> GetByKeyValue(string key, string value)
    {
        var filter = roleLookupRepository.CreateFilterBuilder();
        filter.Add(t => t.Key, key, Core.Helpers.FilterComparer.Like);
        filter.Add(t => t.Value, value, Core.Helpers.FilterComparer.Like);
        var res = await roleLookupRepository.GetExtended(filter);
        if (res == null || !res.Any())
        {
            return null;
        }

        var id = res.Select(t => t.Role).First().Id;
        var roleDto = new RoleDto(await roleRepository.GetExtended(id));
        await GetSingleLegacyRoleCodeAndUrn(roleDto);
        return roleDto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetLookupKeys()
    {
        var res = await roleLookupRepository.Get();
        if (res == null || !res.Any())
        {
            return null;
        }

        return res.Select(t => t.Key).Distinct();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RolePackageDto>> GetPackagesForRole(Guid id)
    {
        var rolePackages = await rolePackageRepository.GetExtended(t => t.RoleId, id);
        if (rolePackages == null || !rolePackages.Any())
        {
            var role = await roleRepository.Get(id);
            if (role == null)
            {
                return null;
            }

            return [];
        }

        var roleDto = new RoleDto(rolePackages.First().Role);
        await GetSingleLegacyRoleCodeAndUrn(roleDto);

        var rolePackageDtos = new List<RolePackageDto>();
        foreach (var rolePackage in rolePackages)
        {
            var rolePackageDto = new RolePackageDto(rolePackage);
            rolePackageDto.Role = roleDto;
            rolePackageDtos.Add(rolePackageDto);
        }

        return rolePackageDtos;
    }

    private async Task<IEnumerable<RoleDto>> GetLegacyRoleCodeAndUrn(IEnumerable<RoleDto> roles)
    {
        var roleLookup = await roleLookupRepository.GetExtended();
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

    private async Task GetSingleLegacyRoleCodeAndUrn(RoleDto extRole)
    {
        var filter = roleLookupRepository.CreateFilterBuilder();
        filter.Add(t => t.RoleId, extRole.Id, Core.Helpers.FilterComparer.Equals);
        var res = await roleLookupRepository.GetExtended(filter);
        var legacyRoleCode = res.Data.FirstOrDefault(x => x.RoleId == extRole.Id && x.Key == "LegacyCode");
        if (legacyRoleCode != null)
        {
            extRole.LegacyRoleCode = legacyRoleCode.Value;
            extRole.LegacyUrn = $"urn:altinn:rolecode:{legacyRoleCode.Value}";
        }
    }
}
