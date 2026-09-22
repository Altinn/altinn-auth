using System.Net.Mime;
using Altinn.AccessManagement.Api.Internal.Utils;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services;
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

namespace Altinn.AccessManagement.Api.Internal.Controllers.Bff;

/// <summary>
/// Activity log endpoints for the Altinn Portal frontend — the early access surface while the
/// enduser API is still gated off. Accessible only with the portal scope. What the caller may
/// see is decided by <see cref="ActivityLogRoleMatrix"/>: their effective roles for the party
/// determine which parts of the log are visible, and every query is constrained to that set.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/bff/activitylog")]
[FeatureGate(AccessMgmtFeatureFlags.EnableBffActivityLogApi)]
public class ActivityLogController(IActivityLogService activityLogService, IConnectionService connectionService) : ControllerBase
{
    private const int DefaultPageSize = 100;

    private const int MaxPageSize = 1000;

    /// <summary>
    /// Get activity log entries involving the specified party, newest first, limited to the
    /// log types the caller's roles for the party may see. Same query surface as the enduser
    /// endpoint. Maskinporten schema events are never included.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
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

        var allowed = await ResolveAllowedTypes(query.Party, cancellationToken);
        if (allowed is null)
        {
            return Unauthorized();
        }

        if (allowed.Count == 0)
        {
            return Forbid();
        }

        var filter = ActivityLogQueryMapper.BuildFilter(query, activityTypeKeys);

        var size = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var page = Math.Max(query.PageNo ?? 0, 0);

        if (!ActivityLogRoleMatrix.TryConstrain(filter, allowed, out var constrained))
        {
            return Ok(PaginatedResult.Create(Array.Empty<ActivityLogDto>(), null));
        }

        var result = await activityLogService.GetActivityLog(
            query.Party,
            query.Direction,
            constrained,
            size,
            page,
            cancellationToken: cancellationToken);

        return Ok(PaginatedResult.Create(result.Items, result.HasMore ? NextLink(size, page + 1) : null));
    }

    /// <summary>
    /// Get the values occurring in the party's activity log for one filter field, limited to
    /// the log types the caller's roles for the party may see. Same semantics as the enduser
    /// filter value endpoint.
    /// </summary>
    [HttpGet("filters/{field}")]
    [Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
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

        var allowed = await ResolveAllowedTypes(query.Party, cancellationToken);
        if (allowed is null)
        {
            return Unauthorized();
        }

        if (allowed.Count == 0)
        {
            return Forbid();
        }

        var filter = ActivityLogQueryMapper.BuildFilter(query, activityTypeKeys);

        var size = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var page = Math.Max(query.PageNo ?? 0, 0);

        if (!ActivityLogRoleMatrix.TryConstrain(filter, allowed, out var constrained))
        {
            return Ok(PaginatedResult.Create(Array.Empty<ActivityLogFilterValueDto>(), null));
        }

        var result = await activityLogService.GetActivityLogFilterValues(
            query.Party,
            query.Direction,
            field,
            constrained,
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

    /// <summary>
    /// Resolves the caller's effective roles for the party (direct, keyrole and rolemap
    /// expansion via the connection query) into the set of visible log types. Null means the
    /// caller identity is missing; an empty set means no part of the log is visible.
    /// </summary>
    private async Task<IReadOnlySet<ActivityLogType>> ResolveAllowedTypes(Guid party, CancellationToken cancellationToken)
    {
        var userUuid = UserUtil.GetUserUuid(User);
        if (userUuid is null)
        {
            return null;
        }

        var connections = await connectionService.Get(party, fromId: party, toId: userUuid.Value, cancellationToken: cancellationToken);
        if (connections.IsProblem)
        {
            return ActivityLogRoleMatrix.AllowedTypes([]);
        }

        var roleIds = connections.Value.SelectMany(c => c.Roles).Select(r => r.Id);
        return ActivityLogRoleMatrix.AllowedTypes(roleIds);
    }

    private string NextLink(int pageSize, int nextPageNo)
    {
        var query = QueryHelpers.ParseQuery(Request.QueryString.Value);
        query["pageSize"] = pageSize.ToString();
        query["pageNo"] = nextPageNo.ToString();
        return UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, Request.Path, QueryString.Create(query));
    }
}
