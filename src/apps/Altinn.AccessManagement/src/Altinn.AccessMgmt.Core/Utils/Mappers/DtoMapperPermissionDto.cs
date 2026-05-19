using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static PermissionDto ConvertToPermission(Connection connection)
    {
        return new PermissionDto()
        {
            From = Convert(connection.From),
            To = Convert(connection.To),
            Via = Convert(connection.Via),
            ViaRole = ConvertCompactRole(connection.ViaRole),
            Role = ConvertCompactRole(connection.Role)
        };
    }

    public static AccessReason ConvertToAccessReason(ConnectionReason reason)
    {
        switch (reason)
        {
            case ConnectionReason.Assignment:
                return AccessReasonFlag.Direct;
            case ConnectionReason.Delegation:
                return AccessReasonFlag.ClientDelegation;
            case ConnectionReason.Hierarchy:
                return AccessReasonFlag.Parent;
            case ConnectionReason.RoleMap:
                return AccessReasonFlag.RoleMap;
            case ConnectionReason.KeyRole:
                return AccessReasonFlag.KeyRole;
            default:
                return AccessReasonFlag.None;
        }        
    }

    public static PermissionDto ConvertToPermission(ConnectionQueryExtendedRecord connection)
    {
        return new PermissionDto()
        {
            From = Convert(connection.From),
            To = Convert(connection.To),
            Via = Convert(connection.Via),
            Reason = ConvertToAccessReason(connection.Reason),
            ViaRole = ConvertCompactRole(connection.ViaRole),
            Role = connection.AssignmentId.HasValue ? ConvertCompactRole(connection.Role) : null
        };
    }

    public CompactPermission ConvertToCompactPermission(Connection connection)
    {
        return new CompactPermission()
        {
            From = connection.From,
            To = connection.To
        };
    }

    public static PermissionDto ConvertToPermission(Assignment assignment)
    {
        return new PermissionDto()
        {
            From = Convert(assignment.From),
            To = Convert(assignment.To),
            Role = ConvertCompactRole(assignment.Role)
        };
    }
}
