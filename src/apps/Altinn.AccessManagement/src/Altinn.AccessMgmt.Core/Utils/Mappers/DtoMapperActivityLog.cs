using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Utils;

public partial class DtoMapper : IDtoMapper
{
    /// <summary>
    /// Maps an activity log entry to its external contract. The mapping is 1:1 with the
    /// denormalized table except <c>SourceName</c>, which is resolved from the static
    /// <see cref="SystemEntityConstants"/> catalog.
    /// </summary>
    public static ActivityLogDto Convert(ActivityLog entry) => new()
    {
        Id = entry.Id,
        Type = entry.Type,
        Subtype = entry.Subtype,
        Trigger = entry.Trigger,
        Status = entry.Status,
        When = entry.When,
        ById = entry.ById,
        ByName = entry.ByName,
        SourceId = entry.SourceId,
        SourceName = ResolveSystemEntityName(entry.SourceId),
        OperationId = entry.OperationId,
        FromId = entry.FromId,
        FromName = entry.FromName,
        FromType = entry.FromType,
        ToId = entry.ToId,
        ToName = entry.ToName,
        ToType = entry.ToType,
        ViaId = entry.ViaId,
        ViaName = entry.ViaName,
        ViaType = entry.ViaType,
        RoleId = entry.RoleId,
        RoleName = entry.RoleName,
        ViaRoleId = entry.ViaRoleId,
        ViaRoleName = entry.ViaRoleName,
        PackageId = entry.PackageId,
        PackageName = entry.PackageName,
        ResourceId = entry.ResourceId,
        ResourceName = entry.ResourceName,
        InstanceId = entry.InstanceId,
        ItemId = entry.ItemId,
        ParentId = entry.ParentId,
        Details = ParseDetails(entry.Details),
        ActivityTypeId = ActivityTypeConstants.Resolve(entry.Type, entry.Subtype, entry.Trigger, entry.Status)?.Id,
    };

    /// <summary>
    /// Maps a facet value to its external contract.
    /// </summary>
    public static ActivityLogFacetDto ToActivityLogFacetDto(ActivityLogFacet facet) => new()
    {
        Id = facet.Id,
        Name = facet.Name,
    };

    /// <summary>
    /// Maps an activity type catalog constant to its external contract.
    /// </summary>
    public static ActivityTypeDto ToActivityTypeDto(ConstantDefinition<ActivityType> definition) => new()
    {
        Id = definition.Id,
        Type = definition.Entity.Type,
        Subtype = definition.Entity.Subtype,
        Trigger = definition.Entity.Trigger,
        Status = definition.Entity.Status,
        Name = definition.Entity.Name,
        Description = definition.Entity.Description,
    };

    private static string ResolveSystemEntityName(Guid? sourceId)
        => sourceId.HasValue && SystemEntityConstants.TryGetById(sourceId.Value, out var definition)
            ? definition.Entity.Name
            : null;

    private static JsonElement? ParseDetails(string details)
        => string.IsNullOrEmpty(details) ? null : JsonSerializer.Deserialize<JsonElement>(details);
}
