using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessManagement.Api.Enduser.Mappers;

/// <summary>
/// Maps between <see cref="AssignmentExternal"/> and <see cref="Assignment"/>.
/// </summary>
public class AssignmentExternalMapper : IMapper<AssignmentExternal, Assignment>
{
    /// <inheritdoc/>
    public AssignmentExternal Map(Assignment from)
    {
        return new()
        {
            Id = from.Id,
            FromId = from.FromId,
            ToId = from.ToId,
            RoleId = from.RoleId,
        };
    }
}
