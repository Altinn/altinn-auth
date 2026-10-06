using System.Net.Mime;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.Base;

/// <summary>
/// One area of the activity log: the slice of events it serves, the filter fields it offers
/// and the catalog entries it accepts as typeId input. The area is the authorization
/// boundary — the concrete controller declares route, feature flag and policies, and the
/// base constrains every query to the area.
/// </summary>
/// <param name="Types">The main record types the area serves.</param>
/// <param name="Subtypes">The subtypes the area accepts in typeId input and lists under
/// types, or null for all. Query rows are already scoped by <paramref name="Types"/> and
/// <paramref name="ForcedRoleIds"/>.</param>
/// <param name="ForcedRoleIds">Role ids forced onto every query (the Maskinporten area pins
/// the Supplier role), or null to let the caller's role filter apply.</param>
/// <param name="IncludeMaskinportenSchema">Whether the area serves the Maskinporten schema
/// events every other area hides.</param>
/// <param name="Fields">The filter fields the area offers; filter lookups on other fields
/// are rejected.</param>
public sealed record ActivityLogArea(
    IReadOnlyList<ActivityLogType> Types,
    IReadOnlyList<ActivityLogSubtype?> Subtypes,
    IReadOnlyList<Guid> ForcedRoleIds,
    bool IncludeMaskinportenSchema,
    IReadOnlyList<ActivityLogFilterField> Fields)
{
    /// <summary>
    /// Whether a catalog entry belongs to this area.
    /// </summary>
    public bool AcceptsCatalogEntry(ActivityLogType type, ActivityLogSubtype? subtype)
        => Types.Contains(type) && (Subtypes is null || Subtypes.Contains(subtype));
}

/// <summary>
/// The activity log areas: connections (assignments and delegations), request (access
/// requests) and maskinporten (Maskinporten schema delegations — the Supplier-role slice).
/// </summary>
public static class ActivityLogAreas
{
    /// <summary>
    /// Assignment and delegation events, with Maskinporten schema events excluded as
    /// everywhere else.
    /// </summary>
    public static readonly ActivityLogArea Connections = new(
        Types: [ActivityLogType.Assignment, ActivityLogType.Delegation],
        Subtypes: null,
        ForcedRoleIds: null,
        IncludeMaskinportenSchema: false,
        Fields:
        [
            ActivityLogFilterField.From,
            ActivityLogFilterField.To,
            ActivityLogFilterField.Via,
            ActivityLogFilterField.By,
            ActivityLogFilterField.Role,
            ActivityLogFilterField.Package,
            ActivityLogFilterField.Resource,
            ActivityLogFilterField.Source,
            ActivityLogFilterField.ActivityType,
        ]);

    /// <summary>
    /// Access request events, including their package/resource children and status changes.
    /// </summary>
    public static readonly ActivityLogArea Request = new(
        Types: [ActivityLogType.Request],
        Subtypes: null,
        ForcedRoleIds: null,
        IncludeMaskinportenSchema: false,
        Fields:
        [
            ActivityLogFilterField.From,
            ActivityLogFilterField.To,
            ActivityLogFilterField.By,
            ActivityLogFilterField.Package,
            ActivityLogFilterField.Resource,
            ActivityLogFilterField.Source,
            ActivityLogFilterField.ActivityType,
        ]);

    /// <summary>
    /// Maskinporten schema delegation events: assignment main-row and resource events carrying
    /// the Supplier role, which every other area hides.
    /// </summary>
    public static readonly ActivityLogArea Maskinporten = new(
        Types: [ActivityLogType.Assignment],
        Subtypes: [null, ActivityLogSubtype.Resource],
        ForcedRoleIds: [RoleConstants.Supplier.Id],
        IncludeMaskinportenSchema: true,
        Fields:
        [
            ActivityLogFilterField.From,
            ActivityLogFilterField.To,
            ActivityLogFilterField.By,
            ActivityLogFilterField.Resource,
            ActivityLogFilterField.Source,
            ActivityLogFilterField.ActivityType,
        ]);
}

/// <summary>
/// Shared implementation for the per-area activity log endpoints. Every area exposes the same
/// four routes over the same query surface: the log itself, filter value lookups, the fields
/// the area offers, and the catalog entries it accepts. The authorized endpoints use the same
/// anchor convention as the connections endpoints — from=party (access given) or to=party
/// (access received) — because the directional scope policies and the person access-manager
/// rule key on those raw parameters. The two discovery endpoints are static, anonymous and
/// cached.
/// </summary>
public abstract class ActivityLogAreaControllerBase(IActivityLogService activityLogService, ActivityLogArea area) : ControllerBase
{
    private const int DefaultPageSize = 100;

    private const int MaxPageSize = 1000;

    /// <summary>
    /// Get the area's activity log entries for the party, newest first. The query must be
    /// anchored with from=party (access given) or to=party (access received).
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PaginatedResult<ActivityLogDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery] ActivityLogQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!TryPrepare(query, out var filter, out var direction, out var size, out var page, out var error))
        {
            return error;
        }

        var result = await activityLogService.GetActivityLog(
            query.Party,
            direction,
            filter,
            size,
            page,
            includeMps: area.IncludeMaskinportenSchema,
            cancellationToken: cancellationToken);

        return Ok(PaginatedResult.Create(result.Items, result.HasMore ? NextLink(size, page + 1) : null));
    }

    /// <summary>
    /// Get the values occurring in the party's slice of the area for one filter field, as
    /// (id, name) pairs. The field must be one the area offers; the field's own filter values
    /// are ignored so more can be added.
    /// </summary>
    [HttpGet("filter/{field}")]
    [ProducesResponseType<PaginatedResult<ActivityLogFilterValueDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActivityLogFilterValues(
        [FromRoute(Name = "field")] ActivityLogFilterField field,
        [FromQuery] ActivityLogFilterValueQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!area.Fields.Contains(field))
        {
            ModelState.AddModelError("field", $"The field '{field}' is not available in this part of the activity log.");
            return ValidationProblem(ModelState);
        }

        if (!TryPrepare(query, out var filter, out var direction, out var size, out var page, out var error))
        {
            return error;
        }

        var result = await activityLogService.GetActivityLogFilterValues(
            query.Party,
            direction,
            field,
            filter,
            query.Term,
            query.OrderBy,
            size,
            page,
            includeMps: area.IncludeMaskinportenSchema,
            cancellationToken: cancellationToken);

        return Ok(PaginatedResult.Create(result.Items, result.HasMore ? NextLink(size, page + 1) : null));
    }

    /// <summary>
    /// Get the filter fields this area offers — the valid values for the filter route. Static
    /// metadata, hence anonymous and cached.
    /// </summary>
    [HttpGet("filter/fields")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<List<ActivityLogFilterField>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetFilterFields()
        => Ok(area.Fields);

    /// <summary>
    /// Get the activity type catalog entries this area accepts as typeId input, with display
    /// name and description. Static metadata, hence anonymous and cached.
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<List<ActivityTypeDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetActivityTypes()
        => Ok(ActivityTypeConstants.AllEntities()
            .Where(d => area.AcceptsCatalogEntry(d.Entity.Type, d.Entity.Subtype))
            .Select(DtoMapper.ToActivityTypeDto)
            .ToList());

    private bool TryPrepare(ActivityLogQueryParameters query, out ActivityLogQueryFilter filter, out ActivityLogDirection direction, out int size, out int page, out IActionResult error)
    {
        filter = null;
        direction = default;
        size = 0;
        page = 0;
        error = null;

        if (query.Party == Guid.Empty)
        {
            ModelState.AddModelError("party", "party must be a non-empty guid.");
            error = ValidationProblem(ModelState);
            return false;
        }

        if (query.Direction is not null)
        {
            ModelState.AddModelError("direction", "direction is not used on this endpoint; anchor the query with from or to equal to party.");
            error = ValidationProblem(ModelState);
            return false;
        }

        // Same anchor convention as the connections endpoints, because the directional scope
        // policies and the person access-manager rule key on the raw party/from/to query
        // parameters. from wins when both sides equal the party, mirroring the policy's rule
        // order; the non-anchoring side stays a counterpart filter.
        if (query.From is [var fromParty] && fromParty == query.Party)
        {
            direction = ActivityLogDirection.From;
        }
        else if (query.To is [var toParty] && toParty == query.Party)
        {
            direction = ActivityLogDirection.To;
        }
        else
        {
            ModelState.AddModelError("from", "The query must be anchored with from=party (access given) or to=party (access received).");
            error = ValidationProblem(ModelState);
            return false;
        }

        if (!ActivityLogQueryMapper.TryResolveTypeKeys(query.TypeId, out var activityTypeKeys, out var unknownTypeId))
        {
            ModelState.AddModelError("typeId", $"Unknown activity type id '{unknownTypeId}'.");
            error = ValidationProblem(ModelState);
            return false;
        }

        if (activityTypeKeys is not null && activityTypeKeys.Find(k => !area.AcceptsCatalogEntry(k.Type, k.Subtype)) is { } outsideArea)
        {
            ModelState.AddModelError("typeId", $"The activity type '{outsideArea.Type}/{outsideArea.Subtype}' is not available in this part of the activity log.");
            error = ValidationProblem(ModelState);
            return false;
        }

        filter = ActivityLogQueryMapper.BuildFilter(query, activityTypeKeys);

        var types = filter.Types is { Count: > 0 }
            ? filter.Types.Where(area.Types.Contains).ToList()
            : area.Types;

        if (types.Count == 0)
        {
            ModelState.AddModelError("type", "None of the requested types are available in this part of the activity log.");
            error = ValidationProblem(ModelState);
            return false;
        }

        filter = filter with { Types = types, RoleIds = area.ForcedRoleIds ?? filter.RoleIds };

        size = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        page = Math.Clamp(query.PageNo ?? 0, 0, (int.MaxValue / size) - 1);
        return true;
    }

    private string NextLink(int pageSize, int nextPageNo)
    {
        var query = QueryHelpers.ParseQuery(Request.QueryString.Value);
        query["pageSize"] = pageSize.ToString();
        query["pageNo"] = nextPageNo.ToString();
        return UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, Request.Path, QueryString.Create(query));
    }
}
