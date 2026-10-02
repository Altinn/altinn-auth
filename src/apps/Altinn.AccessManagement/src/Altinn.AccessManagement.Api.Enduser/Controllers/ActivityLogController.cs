using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for the enduser activity log over assignments, delegations and requests.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/activitylog")]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserActivityLogApi)]
public class ActivityLogController(IActivityLogService activityLogService) : ControllerBase
{
    private const int DefaultPageSize = 100;

    private const int MaxPageSize = 1000;

    /// <summary>
    /// Get activity log entries involving the specified party, newest first. All filter
    /// parameters accept multiple values. The optional direction anchors the party on the
    /// from (given), to (received) or via (facilitator) side; without it any involvement
    /// matches. typeId values reference the activity type catalog and expand to whole
    /// combinations OR'ed together. Paging is page-based via pageSize and pageNo; without
    /// them the first 100 entries are returned.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_ACTIVITYLOG_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<ActivityLogDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActivityLog(
        [FromQuery] ActivityLogQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (query.Party == Guid.Empty)
        {
            ModelState.AddModelError("party", "party must be a non-empty guid.");
            return ValidationProblem(ModelState);
        }

        if (!ActivityLogQueryMapper.TryResolveTypeKeys(query.TypeId, out var activityTypeKeys, out var unknownTypeId))
        {
            ModelState.AddModelError("typeId", $"Unknown activity type id '{unknownTypeId}'.");
            return ValidationProblem(ModelState);
        }

        var filter = ActivityLogQueryMapper.BuildFilter(query, activityTypeKeys);

        var size = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var page = Math.Clamp(query.PageNo ?? 0, 0, (int.MaxValue / size) - 1);

        var result = await activityLogService.GetActivityLog(
            query.Party,
            query.Direction,
            filter,
            size,
            page,
            cancellationToken: cancellationToken);

        return Ok(PaginatedResult.Create(result.Items, result.HasMore ? NextLink(size, page + 1) : null));
    }

    /// <summary>
    /// Get the values occurring in the party's activity log for one filter field, as (id, name)
    /// pairs to populate a filter picker. Takes the same filter parameters as the main endpoint
    /// (the looked-up field's own filter values are ignored so more can be added; the party
    /// anchor never is), pages the same way, and term matches names case-insensitively.
    /// The same id can recur with different names, since names are point-in-time snapshots.
    /// </summary>
    [HttpGet("filters/{field}")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_ACTIVITYLOG_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<ActivityLogFilterValueDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActivityLogFilterValues(
        [FromRoute(Name = "field")] ActivityLogFilterField field,
        [FromQuery] ActivityLogFilterValueQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (query.Party == Guid.Empty)
        {
            ModelState.AddModelError("party", "party must be a non-empty guid.");
            return ValidationProblem(ModelState);
        }

        if (!ActivityLogQueryMapper.TryResolveTypeKeys(query.TypeId, out var activityTypeKeys, out var unknownTypeId))
        {
            ModelState.AddModelError("typeId", $"Unknown activity type id '{unknownTypeId}'.");
            return ValidationProblem(ModelState);
        }

        var filter = ActivityLogQueryMapper.BuildFilter(query, activityTypeKeys);

        var size = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var page = Math.Clamp(query.PageNo ?? 0, 0, (int.MaxValue / size) - 1);

        var result = await activityLogService.GetActivityLogFilterValues(
            query.Party,
            query.Direction,
            field,
            filter,
            query.Term,
            query.OrderBy,
            size,
            page,
            cancellationToken: cancellationToken);

        return Ok(PaginatedResult.Create(result.Items, result.HasMore ? NextLink(size, page + 1) : null));
    }

    /// <summary>
    /// Get the activity type catalog: every valid activity log combination with display name
    /// and description. Static metadata without personal data, hence anonymous; the content
    /// only changes on deploy.
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<List<ActivityTypeDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetActivityTypes()
        => Ok(ActivityTypeConstants.AllEntities().Select(DtoMapper.ToActivityTypeDto).ToList());

    private string NextLink(int pageSize, int nextPageNo)
    {
        var query = QueryHelpers.ParseQuery(Request.QueryString.Value);
        query["pageSize"] = pageSize.ToString();
        query["pageNo"] = nextPageNo.ToString();
        return UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, Request.Path, QueryString.Create(query));
    }
}
