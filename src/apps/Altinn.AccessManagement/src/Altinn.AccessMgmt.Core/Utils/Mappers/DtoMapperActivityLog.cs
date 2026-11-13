using System.Text.Json;
using System.Text.RegularExpressions;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// The activity log part of the mapper: log entries, filter values and catalog entries to
/// their external contracts.
/// </summary>
public partial class DtoMapper : IDtoMapper
{
    /// <summary>
    /// Maps an activity log entry to its external contract. The mapping is 1:1 with the
    /// denormalized table except <c>SourceName</c>, which is resolved from the static
    /// <see cref="SystemEntityConstants"/> catalog, and <c>Text</c>, which fills the resolved
    /// catalog entry's sentence template in the requested language.
    /// </summary>
    public static ActivityLogDto Convert(ActivityLog entry, string languageCode = null)
    {
        var activityType = ActivityTypeConstants.Resolve(entry.Type, entry.Subtype, entry.Trigger, entry.Status);
        return new()
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
            ActivityTypeId = activityType?.Id,
            Text = activityType is null ? null : RenderText(activityType.TranslateField(languageCode, "Text", activityType.Entity.Text), entry, languageCode),
        };
    }

    /// <summary>
    /// Maps a filter value to its external contract.
    /// </summary>
    public static ActivityLogFilterValueDto ToActivityLogFilterValueDto(ActivityLogFilterValue value) => new()
    {
        Id = value.Id,
        Name = value.Name,
    };

    /// <summary>
    /// Maps an activity type catalog constant to its external contract.
    /// </summary>
    public static ActivityTypeDto ToActivityTypeDto(ConstantDefinition<ActivityType> definition, string languageCode = null) => new()
    {
        Id = definition.Id,
        Type = definition.Entity.Type,
        Subtype = definition.Entity.Subtype,
        Trigger = definition.Entity.Trigger,
        Status = definition.Entity.Status,
        Name = definition.TranslateField(languageCode, "Name", definition.Entity.Name),
        Description = definition.TranslateField(languageCode, "Description", definition.Entity.Description),
    };

    /// <summary>
    /// Fills the template's placeholders from the entry's name snapshots in one pass, so a name
    /// that itself looks like a placeholder is never expanded. A missing name becomes a neutral
    /// word in the same language.
    /// </summary>
    private static string RenderText(string template, ActivityLog entry, string languageCode)
    {
        var unknown = languageCode switch
        {
            "eng" => "unknown",
            "nno" => "ukjend",
            _ => "ukjent",
        };

        return TextPlaceholder().Replace(template, match =>
        {
            var value = match.Groups[1].Value switch
            {
                "from" => entry.FromName,
                "to" => entry.ToName,
                "via" => entry.ViaName,
                "by" => entry.ByName,
                "role" => entry.RoleName,
                "package" => entry.PackageName,
                "resource" => entry.ResourceName,
                _ => match.Value,
            };

            return string.IsNullOrWhiteSpace(value) ? unknown : value;
        });
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex TextPlaceholder();

    private static string ResolveSystemEntityName(Guid? sourceId)
        => sourceId.HasValue && SystemEntityConstants.TryGetById(sourceId.Value, out var definition)
            ? definition.Entity.Name
            : null;

    private static JsonElement? ParseDetails(string details)
        => string.IsNullOrEmpty(details) ? null : JsonSerializer.Deserialize<JsonElement>(details);
}
