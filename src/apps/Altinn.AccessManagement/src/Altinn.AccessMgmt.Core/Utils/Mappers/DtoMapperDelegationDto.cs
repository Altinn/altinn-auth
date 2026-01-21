using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static DelegationDto ConvertToDelegationDto(Delegation model, Guid packageId, Guid roleId)
    {
        return new DelegationDto()
        {
            ToId = model.ToId,
            FromId = model.FromId,
            ViaId = model.FacilitatorId,
            PackageId = packageId,
            RoleId = roleId,
        };
    }

    public static DelegationDto ConvertToDelegationDto(ConnectionQueryExtendedRecord model, Guid packageId, Guid roleId)
    {
        return new DelegationDto()
        {
            ToId = model.ToId,
            RoleId = model.RoleId,
            FromId = model.FromId,
            ViaId = (Guid)model.ViaId,
            PackageId = packageId
        };
    }
}
