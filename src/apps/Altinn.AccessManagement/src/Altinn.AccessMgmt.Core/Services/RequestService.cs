using System.Diagnostics;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class RequestService(AppDbContext db, IOptions<CoreAppsettings> appsettings) : IRequestService
{
    /// <inheritdoc/>
    public async Task<Result<RequestDto>> GetRequest(Guid requestId, CancellationToken ct = default)
    {
        ValidationErrorBuilder error = default;

        var requestResource = await db.RequestAssignmentResources.AsNoTracking()
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(t => t.Id == requestId, ct);

        if (requestResource != null)    
        {
            return DtoMapper.Convert(requestResource);
        }

        var requestPackage = await db.RequestAssignmentPackages.AsNoTracking()
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Package)
            .FirstOrDefaultAsync(t => t.Id == requestId, ct);

        if (requestPackage != null)
        {
            return DtoMapper.Convert(requestPackage);
        }

        error.Add(ValidationErrors.RequestNotFound);
        if (error.TryBuild(out var problems))
        {
            return problems;
        }

        throw new UnreachableException();
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<RequestDto>>> GetSentRequests(Guid partyId, Guid? toId, IEnumerable<RequestStatus> status, string? type, CancellationToken ct = default)
    {
        var filter = QuerySentFilter(partyId, toId);
        var requestResources = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResource(filter, status, ct) : new List<RequestAssignmentResource>();
        var requestPackages = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackage(filter, status, ct) : new List<RequestAssignmentPackage>();

        var result = requestResources.Select(DtoMapper.Convert)
            .Union(requestPackages.Select(DtoMapper.Convert));

        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<RequestDto>>> GetReceivedRequests(Guid partyId, Guid? fromId, IEnumerable<RequestStatus> status, string? type, CancellationToken ct = default)
    {
        var filter = QueryReceivedFilter(partyId, fromId);
        var requestResources = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResource(filter, status, ct) : new List<RequestAssignmentResource>();
        var requestPackages = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackage(filter, status, ct) : new List<RequestAssignmentPackage>();

        var result = requestResources.Select(DtoMapper.Convert)
            .Union(requestPackages.Select(DtoMapper.Convert));

        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<int>> GetSentRequestsCount(Guid partyId, Guid? toId, IEnumerable<RequestStatus> status, string? type, CancellationToken ct = default)
    {
        var filter = QuerySentFilter(partyId, toId);
        var resourceCount = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResourceCount(filter, status, ct) : 0;
        var packageCount = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackageCount(filter, status, ct) : 0;

        return resourceCount + packageCount;
    }

    /// <inheritdoc/>
    public async Task<Result<int>> GetReceivedRequestsCount(Guid partyId, Guid? fromId, IEnumerable<RequestStatus> status, string? type, CancellationToken ct = default)
    {
        var filter = QueryReceivedFilter(partyId, fromId);
        var resourceCount = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResourceCount(filter, status, ct) : 0;
        var packageCount = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackageCount(filter, status, ct) : 0;

        return resourceCount + packageCount;
    }

    /// <inheritdoc/>
    public async Task<Result<RequestDto>> UpdateRequest(Guid partyUuid, Guid requestId, RequestStatus status, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var requestResult = await GetRequest(requestId, ct);
        if (requestResult.IsProblem)
        {
            return requestResult.Problem;
        }

        var verify = VerifyRequestStatusUpdate(request: requestResult.Value, partyUuid, status);
        if (verify.IsProblem)
        {
            return verify.Problem;
        }

        switch (requestResult.Value.Type)
        {
            case "resource":
                return await UpdateResourceRequestStatus(requestId, status, ct);
            case "package":
                return await UpdatePackageRequestStatus(requestId, status, ct);
        }

        errorBuilder.Add(ValidationErrors.RequestNotFound);
        errorBuilder.TryBuild(out var problems);
        return problems;
    }

    /// <inheritdoc/>
    public async Task<Result<RequestDto>> CreateResourceRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, Guid resourceId, RequestStatus status = RequestStatus.Pending, CancellationToken ct = default)
    {
        var problem = ValidationComposer.Validate(
            PackageValidation.SelfAssignmentNotAllowed(fromId, toId)
        );

        if (problem is { })
        {
            return problem;
        }

        var requestAssignmentResult = await GetOrCreateRequestAssignment(
           fromId: fromId,
           toId: toId,
           roleId: roleId,
           ct: ct
           );
        var requestAssignment = requestAssignmentResult.Value;

        return await CreateResourceRequest(requestAssignment.Id, resourceId, status, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<RequestDto>> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, string package, RequestStatus status = RequestStatus.Pending, CancellationToken ct = default)
    {
        var to = await db.Entities.Include(t => t.Type).FirstOrDefaultAsync(e => e.Id == toId, ct);
        var from = await db.Entities.Include(t => t.Type).FirstOrDefaultAsync(e => e.Id == fromId, ct);

        var problem = ValidationComposer.Validate(
            EntityValidation.ToExists(to),
            EntityValidation.ToExists(from),
            PackageValidation.PackageIsAssignable(package),
            PackageValidation.PackageIsAssignableTo([package], from?.Type),
            PackageValidation.PackageIsAssignableFrom([package], to?.Type),
            PackageValidation.SelfAssignmentNotAllowed(fromId, toId)
        );

        if (problem is { })
        {
            return problem;
        }

        var requestAssignmentResult = await GetOrCreateRequestAssignment(
            fromId: fromId,
            toId: toId,
            roleId: roleId,
            ct: ct
        );

        var requestAssignment = requestAssignmentResult.Value;

        return await CreatePackageRequest(requestAssignment.Id, package, status, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<RequestDto>> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, Guid packageId, RequestStatus status = RequestStatus.Pending, CancellationToken ct = default)
    {
        return await CreatePackageRequest(toId, fromId, byId, roleId, packageId.ToString(), status, ct);
    }

    #region privates
    private async Task<Result<RequestDto>> CreateResourceRequest(Guid assignmentId, Guid resourceId, RequestStatus initialStatus = RequestStatus.Pending, CancellationToken ct = default)
    {
        var request = await db.RequestAssignmentResources
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.ResourceId == resourceId && r.Status == initialStatus, ct);

        if (request is null)
        {
            request = new RequestAssignmentResource
            {
                Status = initialStatus,
                AssignmentId = assignmentId,
                ResourceId = resourceId
            };
            db.RequestAssignmentResources.Add(request);
            await RequestPendingNotification.Upsert(
                db,
                request.Assignment.FromId,
                request.Assignment.ToId,
                resourceId,
                null,
                appsettings?.Value?.Request?.NotifyRequestPendingInSeconds ?? 60 * 15,
                ct
            );

            var res = await db.SaveChangesAsync(ct);

            if (res == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);
    }

    private async Task<Result<RequestDto>> CreatePackageRequest(Guid assignmentId, string package, RequestStatus initialStatus = RequestStatus.Pending, CancellationToken ct = default)
    {
        if (!PackageConstants.TryGetByAll(package, out var packageObj))
        {
            throw new ArgumentException($"Package '{package}' not found", nameof(package));
        }

        var packageId = packageObj.Id;

        var request = await db.RequestAssignmentPackages
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.PackageId == packageId && r.Status == initialStatus, cancellationToken: ct);

        if (request is null)
        {
            request = new RequestAssignmentPackage
            {
                Status = initialStatus,
                AssignmentId = assignmentId,
                PackageId = packageId,
            };
            db.RequestAssignmentPackages.Add(request);
            await RequestPendingNotification.Upsert(
                db,
                request.Assignment.FromId,
                request.Assignment.ToId,
                null,
                packageId,
                appsettings?.Value?.Request?.NotifyRequestPendingInSeconds ?? 60 * 15,
                ct
            );

            var res = await db.SaveChangesAsync(ct);
            if (res == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);
    }

    private static Result<RequestDto> VerifyRequestStatusUpdate(RequestDto request, Guid partyUuid, RequestStatus status)
    {
        ValidationErrorBuilder errorBuilder = default;

        switch (request.Status)
        {
            case RequestStatus.Draft:
                ValidateDraft(request, partyUuid, status, ref errorBuilder);
                break;

            case RequestStatus.Pending:
                ValidatePending(request, partyUuid, status, ref errorBuilder);
                break;

            case RequestStatus.Approved:
            case RequestStatus.Rejected:
            case RequestStatus.Withdrawn:
                AddUnsupportedStatusError(request, status, ref errorBuilder);
                break;
        }

        errorBuilder.TryBuild(out var problems);

        return problems != null ? problems : request;
    }

    private static void ValidateDraft(RequestDto request, Guid partyUuid, RequestStatus status, ref ValidationErrorBuilder errorBuilder)
    {
        if (status != RequestStatus.Pending && status != RequestStatus.Withdrawn)
        {
            AddUnsupportedStatusError(request, status, ref errorBuilder);
            return;
        }

        if (request.From.Id != partyUuid)
        {
            AddUnauthorizedStatusError(request, status, ref errorBuilder);
        }
    }

    private static void ValidatePending(RequestDto request, Guid partyUuid, RequestStatus status, ref ValidationErrorBuilder errorBuilder)
    {
        switch (status)
        {
            case RequestStatus.Withdrawn:
                if (request.From.Id != partyUuid)
                {
                    AddUnauthorizedStatusError(request, status, ref errorBuilder);
                }

                break;

            case RequestStatus.Approved:
            case RequestStatus.Rejected:
                if (request.To.Id != partyUuid)
                {
                    AddUnauthorizedStatusError(request, status, ref errorBuilder);
                }

                break;

            default:
                AddUnsupportedStatusError(request, status, ref errorBuilder);
                break;
        }
    }

    private static void AddUnsupportedStatusError(RequestDto request, RequestStatus status, ref ValidationErrorBuilder errorBuilder)
    {
        var paramName = "party";

        errorBuilder.Add(
            ValidationErrors.RequestUnsupportedStatusUpdate,
            $"$QUERY/{paramName}",
            [new(paramName, $"Changing request status from {request.Status} to {status} is not allowed.")]
        );
    }

    private static void AddUnauthorizedStatusError(RequestDto request, RequestStatus status, ref ValidationErrorBuilder errorBuilder)
    {
        var paramName = "party";

        errorBuilder.Add(
            ValidationErrors.RequestUnauthorizedStatusUpdate,
            $"$QUERY/{paramName}",
            [new(paramName, $"Unable to update {request.Id} from {request.Status} to {status}")]
        );
    }

    private async Task<Result<RequestDto>> UpdatePackageRequestStatus(Guid id, RequestStatus status, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var request = await db.RequestAssignmentPackages
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (request is not { })
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsFound, nameof(db.RequestAssignmentPackages));
            errorBuilder.TryBuild(out var errors);
            if (errors != null)
            {
                return errors;
            }
        }

        request.Status = status;
        if (status == RequestStatus.Withdrawn)
        {
            await RequestPendingNotification.RemoveValue(
                db,
                request.Assignment.FromId,
                request.Assignment.ToId,
                null,
                request.PackageId,
                ct
            );
        }

        if (status == RequestStatus.Approved || status == RequestStatus.Rejected)
        {
            // ToId: organization / person that request approves / declines request.
            // FromId: organization / person that request access.
            await RequestReviewedNotification.Upsert(
                db,
                request.Assignment.ToId,
                request.Assignment.FromId,
                null,
                request.PackageId,
                status == RequestStatus.Approved,
                appsettings?.Value?.Request?.NotifyRequestApprovedInSeconds ?? (60 * 15),
                ct
            );
        }

        var res = await db.SaveChangesAsync(ct);

        if (res == 0)
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsAffected, nameof(db.RequestAssignmentPackages));
        }

        errorBuilder.TryBuild(out var problems);
        if (problems != null)
        {
            return problems;
        }

        return await GetRequest(id, ct);
    }

    private async Task<Result<RequestDto>> UpdateResourceRequestStatus(Guid id, RequestStatus status, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var request = await db.RequestAssignmentResources
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (request is not { })
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsFound, nameof(db.RequestAssignmentResources));
            errorBuilder.TryBuild(out var errors);
            if (errors != null)
            {
                return errors;
            }
        }

        request.Status = status;
        if (status == RequestStatus.Withdrawn)
        {
            await RequestPendingNotification.RemoveValue(
                db,
                request.Assignment.FromId,
                request.Assignment.ToId,
                request.ResourceId,
                null,
                ct
            );
        }

        if (status == RequestStatus.Approved || status == RequestStatus.Rejected)
        {
            // ToId: organization / person that request approves / declines request.
            // FromId: organization / person that request access.
            await RequestReviewedNotification.Upsert(
                db,
                request.Assignment.ToId,
                request.Assignment.FromId,
                request.ResourceId,
                null,
                status == RequestStatus.Approved,
                appsettings?.Value?.Request?.NotifyRequestApprovedInSeconds ?? (60 * 15),
                ct
            );
        }

        var res = await db.SaveChangesAsync(ct);

        if (res == 0)
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsAffected, nameof(db.RequestAssignmentResources));
        }

        errorBuilder.TryBuild(out var problems);
        if (problems != null)
        {
            return problems;
        }

        return await GetRequest(id, ct);
    }

    private async Task<IEnumerable<RequestAssignmentResource>> GetRequestAssignmentResource(RequestFilter filter, IEnumerable<RequestStatus> status, CancellationToken ct)
    {
        ValidateFilter(filter);

        return await BuildRequestAssignmentResourceQuery(filter, status)
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Resource)
            .ToListAsync(cancellationToken: ct);
    }

    private async Task<IEnumerable<RequestAssignmentPackage>> GetRequestAssignmentPackage(RequestFilter filter, IEnumerable<RequestStatus> status, CancellationToken ct)
    {
        ValidateFilter(filter);

        return await BuildRequestAssignmentPackageQuery(filter, status)
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Package)
            .ToListAsync(cancellationToken: ct);
    }

    private async Task<int> GetRequestAssignmentResourceCount(RequestFilter filter, IEnumerable<RequestStatus> status, CancellationToken ct)
    {
        ValidateFilter(filter);
        return await BuildRequestAssignmentResourceQuery(filter, status).CountAsync(cancellationToken: ct);
    }

    private async Task<int> GetRequestAssignmentPackageCount(RequestFilter filter, IEnumerable<RequestStatus> status, CancellationToken ct)
    {
        ValidateFilter(filter);
        return await BuildRequestAssignmentPackageQuery(filter, status).CountAsync(cancellationToken: ct);
    }

    private IQueryable<RequestAssignmentResource> BuildRequestAssignmentResourceQuery(RequestFilter filter, IEnumerable<RequestStatus> status)
    {
        return db.RequestAssignmentResources
            .WhereIf(filter.FromId.HasValue, r => r.Assignment.FromId == filter.FromId.Value)
            .WhereIf(filter.ToId.HasValue, r => r.Assignment.ToId == filter.ToId.Value)
            .WhereIf(status?.Any() == true, r => status.Contains(r.Status));
    }

    private IQueryable<RequestAssignmentPackage> BuildRequestAssignmentPackageQuery(RequestFilter filter, IEnumerable<RequestStatus> status)
    {
        return db.RequestAssignmentPackages
            .WhereIf(filter.FromId.HasValue, r => r.Assignment.FromId == filter.FromId.Value)
            .WhereIf(filter.ToId.HasValue, r => r.Assignment.ToId == filter.ToId.Value)
            .WhereIf(status?.Any() == true, r => status.Contains(r.Status));
    }

    private static void ValidateFilter(RequestFilter filter)
    {
        if (!filter.FromId.HasValue && !filter.ToId.HasValue)
        {
            throw new ArgumentException("At least one of fromId or toId must be provided");
        }
    }

    private async Task<Result<RequestAssignment>> GetOrCreateRequestAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken ct = default)
    {
        /*
        A Request from Kari by NAV to BakerAS for AppResource01.
        Will create an Assignment from BakerAS to Kari with an AssignmentResource for AppResource01.
        */

        var request = await db.RequestAssignments.FirstOrDefaultAsync(r => r.FromId == fromId && r.ToId == toId && r.RoleId == roleId, ct);
        if (request == null)
        {
            request = new RequestAssignment
            {
                FromId = fromId,
                ToId = toId,
                RoleId = roleId,
            };
            db.RequestAssignments.Add(request);

            var res = await db.SaveChangesAsync(ct);

            if (res == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return request;
    }

    private static RequestFilter QuerySentFilter(Guid party, Guid? toId)
    {
        return new RequestFilter(party, toId);
    }

    private static RequestFilter QueryReceivedFilter(Guid party, Guid? fromId)
    {
        return new RequestFilter(fromId, party);
    }

    internal record RequestFilter(Guid? FromId, Guid? ToId);

    #endregion
}
