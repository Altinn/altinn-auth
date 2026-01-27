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

    public static PermissionDto ConvertToPermission(ConnectionQueryExtendedRecord connection)
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
