using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class RoleService(IRoleRepository roleRepository, IRoleLookupRepository roleLookupRepository) : IRoleService
{
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IRoleLookupRepository roleLookupRepository = roleLookupRepository;
    
    /// <inheritdoc />
    public async Task<ExtRole> GetById(Guid id)
    {
        return await roleRepository.GetExtended(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtRole>> GetByProvider(Guid providerId)
    {
        return await roleRepository.GetExtended(t => t.ProviderId, providerId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> GetByCode(string code)
    {
        return await roleRepository.GetExtended(t => t.Code, code);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> GetByKeyValue(string key, string value)
    {
        var filter = roleLookupRepository.CreateFilterBuilder();
        filter.Equal(t => t.Key, key);
        filter.Equal(t => t.Value, value);
        var res = await roleLookupRepository.GetExtended(filter);
        if (res == null || !res.Any())
        {
            return null;
        }

        return res.Select(t => t.Role);
    }
}
