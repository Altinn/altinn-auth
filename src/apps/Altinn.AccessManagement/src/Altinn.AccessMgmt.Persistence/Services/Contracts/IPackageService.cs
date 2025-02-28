using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

public interface IPackageService
{
    Task<IEnumerable<AreaGroupDto>> GetAreaGroupDtos();
}
