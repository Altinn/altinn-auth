using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static AssignmentDto Convert(Assignment obj)
    {
        return new AssignmentDto()
        {
            Id = obj.Id,
            FromId = obj.FromId,
            RoleId = obj.RoleId,
            ToId = obj.ToId,
        };
    }
}
