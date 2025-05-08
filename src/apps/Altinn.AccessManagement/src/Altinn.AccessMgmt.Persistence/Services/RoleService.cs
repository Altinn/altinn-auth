using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.AccessMgmt.Repo.Definitions;

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

        return new RoleDto(extRole);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RoleDto>> GetAll()
    {
        var roles = await roleRepository.GetExtended();
        if (roles == null)
        {
            return null;
        }

        return roles.Select(t => new RoleDto(t));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByProvider(Guid providerId)
    {
        var roles = await roleRepository.GetExtended(t => t.ProviderId, providerId);
        if (roles == null)
        {
            return null;
        }

        return roles.Select(t => new RoleDto(t));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByCode(string code)
    {
        var roles = await roleRepository.GetExtended(t => t.Code, code);
        if (roles == null)
        {
            return null;
        }

        return roles.Select(t => new RoleDto(t));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetByKeyValue(string key, string value)
    {
        var filter = roleLookupRepository.CreateFilterBuilder();
        filter.Add(t => t.Key, key, Core.Helpers.FilterComparer.Like);
        filter.Add(t => t.Value, value, Core.Helpers.FilterComparer.Like);
        var res = await roleLookupRepository.GetExtended(filter);
        if (res == null || !res.Any())
        {
            return null;
        }

        return res.Select(t => new RoleDto(t.Role));
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
        if (rolePackages == null)
        {
            return null;
        }

        return rolePackages.Select(t => new RolePackageDto(t));
    }
}
