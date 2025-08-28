using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Dto Mapping
/// </summary>
public partial class DtoMapper
{
    public static AccessPackageDto.Check Convert(PackageDelegationCheck obj)
    {
        return new AccessPackageDto.Check()
        {
            Result = obj.Result,
            Package = Convert(obj.Package),
            Reasons = new List<AccessPackageDto.Check.Reason>() { Convert(obj.Reason) }
        };
    }

    public static AccessPackageDto.Check.Reason Convert(PackageDelegationCheckReason obj)
    {
        return new AccessPackageDto.Check.Reason()
        {
            Description = obj.Description,
            FromId = obj.FromId,
            FromName = obj.FromName,
            RoleId = obj.RoleId,
            RoleUrn = obj.RoleUrn,
            ToId = obj.ToId,
            ToName = obj.ToName,
            ViaId = obj.ViaId,
            ViaName = obj.ViaName,
            ViaRoleId = obj.ViaRoleId,
            ViaRoleUrn = obj.ViaRoleUrn,
        };
    }

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
