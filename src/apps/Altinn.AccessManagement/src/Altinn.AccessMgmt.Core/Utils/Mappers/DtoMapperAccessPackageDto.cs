using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static AccessPackageDto Convert(PackageDto obj)
    {
        return new AccessPackageDto()
        {
            Id = obj.Id,
            AreaId = obj.Area.Id,
            Urn = obj.Urn,
        };
    }
}
