using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class EntityService(IEntityRepository entityRepository, IEntityLookupRepository entityLookupRepository) : IEntityService
{
    private readonly IEntityRepository entityRepository = entityRepository;
    private readonly IEntityLookupRepository entityLookupRepository = entityLookupRepository;

    /// <inheritdoc/>
    public async Task<Entity> GetByOrgNo(string orgNo)
    {
        var filter = entityLookupRepository.CreateFilterBuilder();
        filter.Add(t => t.Key, "OrgNo", Core.Helpers.FilterComparer.Contains);
        filter.Equal(t => t.Value, orgNo);

        var res = await entityLookupRepository.GetExtended(filter);

        if (res == null || !res.Any())
        {
            return null;
        }

        if (res.Count() > 1)
        {
            throw new Exception("Multiple matches");
        }

        return res.First().Entity;
    }

    /// <inheritdoc/>
    public Task<Entity> GetByPersNo(string persNo)
    {
        var res = entityRepository.Get(new RequestOptions()
        {
            AsOf = DateTimeOffset.Now.AddHours(-1),

            Language = "eng",

            OrderBy = "name",
            PageNumber = 1,
            PageSize = 10,
            UsePaging = true,
        });

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<Entity> GetByProfile(string profileId)
    {
        throw new NotImplementedException();
    }
}
