using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static AssignmentPackageDto Convert(AssignmentPackage obj)
    {
        return new AssignmentPackageDto()
        {
            Id = obj.Id,
            AssignmentId = obj.AssignmentId,
            PackageId = obj.PackageId,
        };
    }

    public static AssignmentResourceDto Convert(AssignmentResource obj)
    {
        return new AssignmentResourceDto()
        {
            Id = obj.Id,
            AssignmentId = obj.AssignmentId,
            ResourceId = obj.ResourceId,
        };
    }
}
