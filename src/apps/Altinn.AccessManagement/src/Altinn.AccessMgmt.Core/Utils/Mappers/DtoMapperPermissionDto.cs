using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
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

    /// <summary>
    /// Converts a connection to a permission, with who last changed the access and when.
    /// </summary>
    /// <param name="connection">The connection record.</param>
    /// <param name="changeInfo">Change info for exactly this combination of connection and package, resource or instance, or <c>null</c>.</param>
    /// <param name="performers">Entities that made changes, by id. <c>null</c> leaves <c>LastModifiedBy</c> empty.</param>
    public static PermissionDto ConvertToPermission(ConnectionQueryExtendedRecord connection, ConnectionQueryChangeInfo changeInfo, IReadOnlyDictionary<Guid, Entity> performers)
    {
        var permission = ConvertToPermission(connection);
        permission.AuditInfo = ConvertAuditInfo(changeInfo, performers);
        return permission;
    }

    /// <summary>
    /// Converts change info to <see cref="AuditInfoDto"/>, or <c>null</c> when there is none.
    /// </summary>
    /// <param name="changeInfo">Who last changed the access and when, or <c>null</c>.</param>
    /// <param name="performers">Entities that made changes, by id. <c>null</c> leaves <c>LastModifiedBy</c> empty.</param>
    public static AuditInfoDto ConvertAuditInfo(ConnectionQueryChangeInfo changeInfo, IReadOnlyDictionary<Guid, Entity> performers)
    {
        if (changeInfo is null)
        {
            return null;
        }

        var auditInfo = new AuditInfoDto() { LastModified = changeInfo.ValidFrom };
        if (performers is not null && changeInfo.ChangedBy is Guid changedBy && performers.TryGetValue(changedBy, out var performer))
        {
            auditInfo.LastModifiedBy = ConvertAuditEntity(performer);
        }

        return auditInfo;
    }

    /// <summary>
    /// Converts the entity that made a change. Fills only id, name, type and variant.
    /// <see cref="AuditEntityDto"/> has no fields for identifiers such as organization or person numbers,
    /// party ids, user ids, usernames or dates of birth.
    /// </summary>
    public static AuditEntityDto ConvertAuditEntity(Entity entity)
    {
        if (entity is null)
        {
            return null;
        }

        EntityTypeConstants.TryGetById(entity.TypeId, out var type);
        EntityVariantConstants.TryGetById(entity.VariantId, out var variant);

        return new AuditEntityDto()
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = type?.Entity.Name,
            Variant = variant?.Entity.Name,
        };
    }

    /// <summary>
    /// Collects the distinct ids of everyone who changed the packages, resources and instances on the connections.
    /// </summary>
    public static HashSet<Guid> CollectChangedBy(IEnumerable<ConnectionQueryExtendedRecord> connections)
    {
        var ids = new HashSet<Guid>();
        foreach (var connection in connections)
        {
            if (connection.PackageChangeInfo is not null)
            {
                foreach (var info in connection.PackageChangeInfo.Values)
                {
                    AddChangedBy(ids, info);
                }
            }

            foreach (var resource in connection.Resources ?? [])
            {
                AddChangedBy(ids, resource.ChangeInfo);
            }

            foreach (var instance in connection.Instances ?? [])
            {
                AddChangedBy(ids, instance.ChangeInfo);
            }
        }

        return ids;
    }

    private static void AddChangedBy(HashSet<Guid> ids, ConnectionQueryChangeInfo info)
    {
        if (info?.ChangedBy is Guid id)
        {
            ids.Add(id);
        }
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
