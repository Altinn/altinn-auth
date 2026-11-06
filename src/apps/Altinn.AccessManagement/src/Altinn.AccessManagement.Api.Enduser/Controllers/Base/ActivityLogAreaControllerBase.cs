using System.Net.Mime;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Extensions;
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
/// Shared implementation for the per-area activity log endpoints. Every area exposes the same
/// four routes over the same query surface: the log itself, filter value lookups, the fields
/// the area offers, and the catalog entries it accepts. party and direction (from/to) are
/// required on the authorized endpoints — the directional scope policies and the person
/// access-manager rule key on the direction parameter, so from/to stay plain counterpart
/// filters and may equal the party. The two discovery endpoints are static, anonymous and
/// cached.
/// </summary>
public abstract class ActivityLogAreaControllerBase(IActivityLogService activityLogService, ActivityLogArea area) : ControllerBase
{
    private const int DefaultPageSize = 100;

    private const int MaxPageSize = 1000;

    /// <summary>
    /// Get the area's activity log entries for the party, newest first. direction is
    /// required: from (access given by the party) or to (access received by the party).
    /// </summary>
    // Name = "" pins the empty binding prefix on both bound models: the authorization handlers
    // read the raw party/direction keys, so binding must never prefer query.*-prefixed duplicates.
    [HttpGet]
    [ProducesResponseType<PaginatedResult<ActivityLogDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery(Name = "")] ActivityLogQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!TryPrepare(query, out var filter, out var direction, out var size, out var page, out var error))
        {
            return error;
        }

        var result = area.Slice switch
        {
            ActivityLogSlice.Maskinporten => await activityLogService.GetMaskinportenSchemaActivityLog(query.Party, direction, filter, size, page, cancellationToken),
            ActivityLogSlice.Request => await activityLogService.GetRequestActivityLog(query.Party, direction, filter, size, page, cancellationToken),
            _ => await activityLogService.GetConnectionsActivityLog(query.Party, direction, filter, size, page, cancellationToken),
        };

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
        [FromQuery(Name = "")] ActivityLogFilterValueQueryParameters query,
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

        var languageCode = this.GetLanguageCode();
        var result = area.Slice switch
        {
            ActivityLogSlice.Maskinporten => await activityLogService.GetMaskinportenSchemaActivityLogFilterValues(query.Party, direction, field, filter, query.Term, query.OrderBy, size, page, languageCode, cancellationToken),
            ActivityLogSlice.Request => await activityLogService.GetRequestActivityLogFilterValues(query.Party, direction, field, filter, query.Term, query.OrderBy, size, page, languageCode, cancellationToken),
            _ => await activityLogService.GetConnectionsActivityLogFilterValues(query.Party, direction, field, filter, query.Term, query.OrderBy, size, page, languageCode, cancellationToken),
        };

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
    /// name and description in the requested language. Static metadata, hence anonymous and
    /// cached per Accept-Language.
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Language")]
    [ProducesResponseType<List<ActivityTypeDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetActivityTypes()
    {
        var languageCode = this.GetLanguageCode();
        return Ok(ActivityTypeConstants.AllEntities()
            .Where(d => area.AcceptsCatalogEntry(d.Entity.Type, d.Entity.Subtype))
            .Select(d => DtoMapper.ToActivityTypeDto(d, languageCode))
            .ToList());
    }

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

        // The directional scope policies and the person access-manager rule key on this
        // parameter, so from/to stay plain counterpart filters and may equal the party
        // (self-events); the service overwrites the anchored side with the party.
        if (query.Direction is not (ActivityLogDirection.From or ActivityLogDirection.To))
        {
            ModelState.AddModelError("direction", "direction is required and must be 'from' or 'to'.");
            error = ValidationProblem(ModelState);
            return false;
        }

        direction = query.Direction.Value;

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

        filter = filter with { Types = types };

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
