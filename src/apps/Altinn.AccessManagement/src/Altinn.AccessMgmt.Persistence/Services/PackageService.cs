using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class PackageService : IPackageService
{
    private readonly IPackageRepository packageRepository;
    private readonly IAreaGroupRepository areaGroupRepository;
    private readonly IAreaRepository areaRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageService"/> class.
    /// </summary>
    /// <param name="packageRepository">IPackageRepository</param>
    /// <param name="areaGroupRepository">IAreaGroupRepository</param>
    /// <param name="areaRepository">IAreaRepository</param>
    public PackageService(IPackageRepository packageRepository, IAreaGroupRepository areaGroupRepository, IAreaRepository areaRepository)
    {
        this.packageRepository = packageRepository;
        this.areaGroupRepository = areaGroupRepository;
        this.areaRepository = areaRepository;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaGroupDto>> GetAreaGroupDtos()
    {
        var groups = await areaGroupRepository.GetExtended();
        var areas = await areaRepository.GetExtended();
        var packages = await packageRepository.GetExtended();

        var result = new List<AreaGroupDto>();
        foreach (var package in packages)
        {
            var grpId = package.Area.GroupId;
            var areaId = package.Area.Id;

            if (result.Count(t => t.Id == grpId) == 0)
            {
                result.Add(ConvertToDto(groups.First(t => t.Id == grpId)));
            }

            var grp = result.First(t => t.Id == grpId);
            if (grp.Areas.Count() == 0)
            {
                if (grp.Areas.Count(t => t.Id == areaId) == 0)
                {
                    grp.Areas.Add(ConvertToDto(areas.First(t => t.Id == areaId)));
                }
            }

            var area = grp.Areas.First(t => t.Id == areaId);
            area.Packages.Add(ConvertToDto(package));
        }

        return result;
    }

    private AreaGroupDto ConvertToDto(ExtAreaGroup areaGroup)
    {
        return new AreaGroupDto()
        {
            Id = areaGroup.Id,
            Name = areaGroup.Name,
            Description = areaGroup.Description,
            Type = areaGroup.EntityType.Name,
            Urn = areaGroup.Urn,
            Areas = new List<AreaDto>()
        };
    }

    private AreaDto ConvertToDto(ExtArea area)
    {
        return new AreaDto()
        {
            Id = area.Id,
            Name = area.Name,
            Urn = area.Urn,
            Description = area.Description,
            Icon = area.IconName,
            Packages = new List<PackageDto>()
        };
    }

    private PackageDto ConvertToDto(ExtPackage package)
    {
        return new PackageDto()
        {
            Id = package.Id,
            Name = package.Name,
            Urn = package.Urn,
            Description = package.Description
        };
    }
}
