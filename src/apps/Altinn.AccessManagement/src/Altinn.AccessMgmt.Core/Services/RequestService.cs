using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class RequestService(AppDbContext db) : IRequestService
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
        error.TryBuild(out var problems);

        return problems;
    }
    
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<RequestDto>>> GetRequests(Guid? fromId, Guid? toId, IEnumerable<RequestStatus> status, string? type, CancellationToken ct = default)
    {
        ValidationErrorBuilder error = default;

        if (!fromId.HasValue && !toId.HasValue)
        {
            error.Add(ValidationErrors.RequestMissingFromOrTo);
        }

        var requestResources = string.IsNullOrEmpty(type) || type == "resource" ? await GetRequestAssignmentResource(fromId, toId, status, ct) : default;
        var requestPackages = string.IsNullOrEmpty(type) || type == "package" ? await GetRequestAssignmentPackage(fromId, toId, status, ct) : default;

        if (!requestResources.Any() && !requestPackages.Any())
        {
            error.Add(ValidationErrors.RequestNotFound);
        }

        if (error.TryBuild(out var problems))
        {
            return problems;
        }

        var result = requestResources.Select(DtoMapper.Convert).Union(requestPackages.Select(DtoMapper.Convert));
        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<RequestDto>> CreateRequest(CreateRequestDto request, CancellationToken ct = default)
    {
        ValidationErrorBuilder error = default;

        if (!request.Resource.HasValue && !request.Package.HasValue)
        {
            error.Add(ValidationErrors.RequestMissingResourceOrPackage);
            error.TryBuild(out var inputProblems);
            return inputProblems;
        }

        var requestAssignmentResult = await GetOrCreateRequestAssignment(request.From, request.To, request.Role, ct);
        var requestAssignment = requestAssignmentResult.Value;

        if (request.Resource.HasValue)
        {
            return await CreateRequestAssignmentResource(requestAssignment.Id, request.Resource.Value, request.Status, ct);
        }

        if (request.Package.HasValue)
        {
            return await CreateRequestAssignmentPackage(requestAssignment.Id, request.Package.Value, request.Status, ct);
        }

        error.Add(ValidationErrors.RequestFailedToCreateRequest);
        error.TryBuild(out var problems);
        return problems;
    }

    /// <inheritdoc/>
    public async Task<Result<RequestDto>> UpdateRequest(Guid partyUuid, Guid requestId, RequestStatus status, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var requestResult = await GetRequest(requestId, ct);
        if (requestResult.IsProblem)
        {
            return requestResult;
        }

        var request = requestResult.Value;
        
        if (request.From.Id != partyUuid)
        {
            errorBuilder.Add(ValidationErrors.RequestNotFound, "$QUERY/requestId", [new("RequestId", $"Request {requestId} does not exists")]);
        }

        var statusRules = new Dictionary<RequestStatus, List<RequestStatus>>
        {
            { RequestStatus.Draft, [RequestStatus.Pending, RequestStatus.Withdrawn] },
            { RequestStatus.Pending, [RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Withdrawn] },
            { RequestStatus.Approved, [] },
            { RequestStatus.Rejected, [] },
            { RequestStatus.Withdrawn, [] }
        };

        foreach (var rule in statusRules)
        {
            if (request.Status == rule.Key && !rule.Value.Contains(status))
            {
                errorBuilder.Add(ValidationErrors.RequestUnsupportedStatusUpdate, "$QUERY/status", [new("Status", $"Request cannot change from {request.Status} to {status}.")]);
            }
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem;
        }

        switch (request.Type)
        {
            case "resource":
                await UpdateResourceRequestStatus(requestId, status);
                break;
            case "package":
                await UpdatePackageRequestStatus(requestId, status);
                break;
        }

        return await GetRequest(requestId, ct);
    }

    #region privates

    private async Task<bool> UpdatePackageRequestStatus(Guid id, RequestStatus status)
    {
        var request = await db.RequestAssignmentPackages.FirstOrDefaultAsync(t => t.Id == id);
        if (request is { })
        {
            request.Status = status;
            await db.SaveChangesAsync();
            return true;
        }

        return false;
    }

    private async Task<bool> UpdateResourceRequestStatus(Guid id, RequestStatus status)
    {
        var request = await db.RequestAssignmentResources.FirstOrDefaultAsync(t => t.Id == id);
        if (request is { })
        {
            request.Status = status;
            await db.SaveChangesAsync();
            return true;
        }

        return false;
    }

    private async Task<IEnumerable<RequestAssignmentResource>> GetRequestAssignmentResource(Guid? fromId, Guid? toId, IEnumerable<RequestStatus> status, CancellationToken ct)
    {
        if (!fromId.HasValue && !toId.HasValue)
        {
            throw new ArgumentException("At least one of fromId, toId or requestedBy must be provided");
        }

        return await db.RequestAssignmentResources
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Resource)
            .WhereIf(fromId.HasValue, r => r.Assignment.FromId == fromId.Value)
            .WhereIf(toId.HasValue, r => r.Assignment.ToId == toId.Value)
            .WhereIf(status?.Any() == true, r => status.Contains(r.Status))
            .ToListAsync(cancellationToken: ct);
    }
    
    private async Task<IEnumerable<RequestAssignmentPackage>> GetRequestAssignmentPackage(Guid? fromId, Guid? toId, IEnumerable<RequestStatus> status, CancellationToken ct)
    {
        if (!fromId.HasValue && !toId.HasValue)
        {
            throw new ArgumentException("At least one of fromId, toId or requestedBy must be provided");
        }

        return await db.RequestAssignmentPackages
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Package)
            .WhereIf(fromId.HasValue, r => r.Assignment.FromId == fromId.Value)
            .WhereIf(toId.HasValue, r => r.Assignment.ToId == toId.Value)
            .WhereIf(status?.Any() == true, r => status.Contains(r.Status))
            .ToListAsync(cancellationToken: ct);
    }

    private async Task<Result<RequestAssignment>> GetOrCreateRequestAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken ct = default)
    {
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

    private async Task<Result<RequestDto>> CreateRequestAssignmentResource(Guid assignmentId, Guid resourceId, RequestStatus initialStatus = RequestStatus.Pending, CancellationToken ct = default)
    {
        var request = await db.RequestAssignmentResources.FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.ResourceId == resourceId && r.Status == initialStatus, ct);
        if (request == null)
        {
            request = new RequestAssignmentResource
            {
                Status = initialStatus,
                AssignmentId = assignmentId,
                ResourceId = resourceId
            };
            db.RequestAssignmentResources.Add(request);

            var res = await db.SaveChangesAsync(ct);

            if (res == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);
    }

    private async Task<Result<RequestDto>> CreateRequestAssignmentPackage(Guid assignmentId, Guid packageId, RequestStatus initialStatus = RequestStatus.Pending, CancellationToken ct = default)
    {
        var request = await db.RequestAssignmentPackages.FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.PackageId == packageId && r.Status == initialStatus, cancellationToken: ct);
        if (request is null)
        {
            request = new RequestAssignmentPackage
            {
                Status = initialStatus,
                AssignmentId = assignmentId,
                PackageId = packageId,
            };
            db.RequestAssignmentPackages.Add(request);

            var res = await db.SaveChangesAsync(ct);
            if (res == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);
    }
    #endregion
}
