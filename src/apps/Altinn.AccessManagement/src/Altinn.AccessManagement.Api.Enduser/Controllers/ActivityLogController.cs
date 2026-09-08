using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
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
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "direction")] ActivityLogDirection? direction = null,
        [FromQuery(Name = "typeId")] List<Guid> typeId = null,
        [FromQuery(Name = "type")] List<ActivityLogType> type = null,
        [FromQuery(Name = "subtype")] List<ActivityLogSubtype> subtype = null,
        [FromQuery(Name = "trigger")] List<ActivityLogTrigger> trigger = null,
        [FromQuery(Name = "status")] List<RequestStatus> status = null,
        [FromQuery(Name = "by")] List<Guid> by = null,
        [FromQuery(Name = "source")] List<Guid> source = null,
        [FromQuery(Name = "operation")] List<string> operation = null,
        [FromQuery(Name = "from")] List<Guid> from = null,
        [FromQuery(Name = "to")] List<Guid> to = null,
        [FromQuery(Name = "via")] List<Guid> via = null,
        [FromQuery(Name = "role")] List<Guid> role = null,
        [FromQuery(Name = "package")] List<Guid> package = null,
        [FromQuery(Name = "resource")] List<Guid> resource = null,
        [FromQuery(Name = "instance")] List<string> instance = null,
        [FromQuery(Name = "itemId")] List<Guid> itemId = null,
        [FromQuery(Name = "parentId")] List<Guid> parentId = null,
        [FromQuery(Name = "after")] DateTimeOffset? after = null,
        [FromQuery(Name = "before")] DateTimeOffset? before = null,
        [FromQuery(Name = "pageSize")] int? pageSize = null,
        [FromQuery(Name = "pageNo")] int? pageNo = null,
        CancellationToken cancellationToken = default)
    {
        if (party == Guid.Empty)
        {
            ModelState.AddModelError("party", "party must be a non-empty guid.");
            return ValidationProblem(ModelState);
        }

        if (!TryResolveActivityTypeKeys(typeId, out var activityTypeKeys, out var typeIdError))
        {
            return typeIdError;
        }

        var filter = BuildFilter(activityTypeKeys, type, subtype, trigger, status, by, source, operation, from, to, via, role, package, resource, instance, itemId, parentId, after, before);

        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var page = Math.Max(pageNo ?? 0, 0);

        var result = await activityLogService.GetActivityLog(
            party,
            direction,
            filter,
            size,
            page,
            cancellationToken);

        return Ok(PaginatedResult.Create(result.Items, result.HasMore ? NextLink(size, page + 1) : null));
    }

    /// <summary>
    /// Get the values occurring in the party's activity log for one facet field, as (id, name)
    /// pairs to populate a filter picker. Takes the same filter parameters as the main endpoint
    /// (the faceted field's own filter values are ignored so more can be added; the party
    /// anchor never is), pages the same way, and term matches names case-insensitively.
    /// The same id can recur with different names, since names are point-in-time snapshots.
    /// </summary>
    [HttpGet("filters/{field}")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_ACTIVITYLOG_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<ActivityLogFacetDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActivityLogFacet(
        [FromRoute(Name = "field")] ActivityLogFacetField field,
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "term")] string term = null,
        [FromQuery(Name = "orderBy")] ActivityLogFacetOrder orderBy = ActivityLogFacetOrder.Name,
        [FromQuery(Name = "direction")] ActivityLogDirection? direction = null,
        [FromQuery(Name = "typeId")] List<Guid> typeId = null,
        [FromQuery(Name = "type")] List<ActivityLogType> type = null,
        [FromQuery(Name = "subtype")] List<ActivityLogSubtype> subtype = null,
        [FromQuery(Name = "trigger")] List<ActivityLogTrigger> trigger = null,
        [FromQuery(Name = "status")] List<RequestStatus> status = null,
        [FromQuery(Name = "by")] List<Guid> by = null,
        [FromQuery(Name = "source")] List<Guid> source = null,
        [FromQuery(Name = "operation")] List<string> operation = null,
        [FromQuery(Name = "from")] List<Guid> from = null,
        [FromQuery(Name = "to")] List<Guid> to = null,
        [FromQuery(Name = "via")] List<Guid> via = null,
        [FromQuery(Name = "role")] List<Guid> role = null,
        [FromQuery(Name = "package")] List<Guid> package = null,
        [FromQuery(Name = "resource")] List<Guid> resource = null,
        [FromQuery(Name = "instance")] List<string> instance = null,
        [FromQuery(Name = "itemId")] List<Guid> itemId = null,
        [FromQuery(Name = "parentId")] List<Guid> parentId = null,
        [FromQuery(Name = "after")] DateTimeOffset? after = null,
        [FromQuery(Name = "before")] DateTimeOffset? before = null,
        [FromQuery(Name = "pageSize")] int? pageSize = null,
        [FromQuery(Name = "pageNo")] int? pageNo = null,
        CancellationToken cancellationToken = default)
    {
        if (party == Guid.Empty)
        {
            ModelState.AddModelError("party", "party must be a non-empty guid.");
            return ValidationProblem(ModelState);
        }

        if (!TryResolveActivityTypeKeys(typeId, out var activityTypeKeys, out var typeIdError))
        {
            return typeIdError;
        }

        var filter = BuildFilter(activityTypeKeys, type, subtype, trigger, status, by, source, operation, from, to, via, role, package, resource, instance, itemId, parentId, after, before);

        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var page = Math.Max(pageNo ?? 0, 0);

        var result = await activityLogService.GetActivityLogFacet(
            party,
            direction,
            field,
            filter,
            term,
            orderBy,
            size,
            page,
            cancellationToken);

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

    private bool TryResolveActivityTypeKeys(List<Guid> typeId, out List<ActivityTypeKey> keys, out IActionResult error)
    {
        keys = null;
        error = null;

        if (typeId is not { Count: > 0 })
        {
            return true;
        }

        keys = new List<ActivityTypeKey>(typeId.Count);
        foreach (var id in typeId)
        {
            if (!ActivityTypeConstants.TryGetById(id, out var definition))
            {
                ModelState.AddModelError("typeId", $"Unknown activity type id '{id}'.");
                error = ValidationProblem(ModelState);
                keys = null;
                return false;
            }

            keys.Add(new ActivityTypeKey(definition.Entity.Type, definition.Entity.Subtype, definition.Entity.Trigger, definition.Entity.Status));
        }

        return true;
    }

    private static ActivityLogQueryFilter BuildFilter(
        List<ActivityTypeKey> activityTypeKeys,
        List<ActivityLogType> type,
        List<ActivityLogSubtype> subtype,
        List<ActivityLogTrigger> trigger,
        List<RequestStatus> status,
        List<Guid> by,
        List<Guid> source,
        List<string> operation,
        List<Guid> from,
        List<Guid> to,
        List<Guid> via,
        List<Guid> role,
        List<Guid> package,
        List<Guid> resource,
        List<string> instance,
        List<Guid> itemId,
        List<Guid> parentId,
        DateTimeOffset? after,
        DateTimeOffset? before) => new()
        {
            ActivityTypeKeys = activityTypeKeys,
            Types = type,
            Subtypes = subtype,
            Triggers = trigger,
            Statuses = status,
            ByIds = by,
            SourceIds = source,
            OperationIds = operation,
            FromIds = from,
            ToIds = to,
            ViaIds = via,
            RoleIds = role,
            PackageIds = package,
            ResourceIds = resource,
            InstanceIds = instance,
            ItemIds = itemId,
            ParentIds = parentId,
            After = after,
            Before = before,
        };

    private string NextLink(int pageSize, int nextPageNo)
    {
        var query = QueryHelpers.ParseQuery(Request.QueryString.Value);
        query["pageSize"] = pageSize.ToString();
        query["pageNo"] = nextPageNo.ToString();
        return UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, Request.Path, QueryString.Create(query));
    }
}
