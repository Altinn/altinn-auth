﻿using System.Diagnostics;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Outbox;
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

    #region privates

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

        if (request.To.Id != partyUuid)
        {
            AddUnauthorizedStatusError(request, status, ref errorBuilder);
        }
    }

    private static void ValidatePending(RequestDto request, Guid partyUuid, RequestStatus status, ref ValidationErrorBuilder errorBuilder)
    {
        switch (status)
        {
            case RequestStatus.Withdrawn:
                if (request.To.Id != partyUuid)
                {
                    AddUnauthorizedStatusError(request, status, ref errorBuilder);
                }

                break;

            case RequestStatus.Approved:
            case RequestStatus.Rejected:
                if (request.From.Id != partyUuid)
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
        if (status == RequestStatus.Approved)
        {
            await AddRequestApprovedToOutbox(request.Assignment.FromId, request.Assignment.ToId, null, request.PackageId, ct);
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
        if (status == RequestStatus.Approved)
        {
            await AddRequestApprovedToOutbox(request.Assignment.FromId, request.Assignment.ToId, request.ResourceId, null, ct);
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

            await AddRequestPendingToOutbox(request.Assignment.FromId, request.Assignment.ToId, resourceId, null, ct);

            var res = await db.SaveChangesAsync(ct);

            if (res == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);
    }

    private async Task AddRequestPendingToOutbox(Guid requesterId, Guid recipientId, Guid? resourceId, Guid? packageId, CancellationToken ct)
    {
        await db.UpsertOutboxAsync(
            refId: $"request_pending_{requesterId}_{recipientId}",
            handler: "request_pending",
            addValueFactory: msg => AddValue(requesterId, recipientId, resourceId, packageId, msg),
            updateValueFactory: (msg, data) => UpdateValue(requesterId, recipientId, resourceId, packageId, msg, data),
            cancellationToken: ct
        );

        static ResourceRequestPendingNotificationMessage UpdateValue(
            Guid requesterId,
            Guid recipientId,
            Guid? resourceId,
            Guid? packageId,
            OutboxMessage msg,
            ResourceRequestPendingNotificationMessage data)
        {
            if (data is null)
            {
                Activity.Current?.AddTag(nameof(AddRequestPendingToOutbox), $"Current outbox message {nameof(ResourceRequestPendingNotificationMessage)} is null? Creating new object.");
                return AddValue(requesterId, recipientId, resourceId, packageId, msg);
            }

            data.Updated++;

            if (resourceId.HasValue && !data.ResourceIds.Contains(resourceId.Value))
            {
                data.ResourceIds = data.ResourceIds.Append(resourceId.Value);
            }

            if (packageId.HasValue && !data.PackageIds.Contains(packageId.Value))
            {
                data.PackageIds = data.PackageIds.Append(packageId.Value);
            }

            var candidate = DateTime.UtcNow.AddMinutes(15.0 / (data.Updated + 1));
            msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

            return data;
        }

        static ResourceRequestPendingNotificationMessage AddValue(
            Guid requesterId,
            Guid recipientId,
            Guid? resourceId,
            Guid? packageId,
            OutboxMessage msg)
        {
            var schedule = DateTime.UtcNow.AddMinutes(15);
            msg.Schedule = schedule;
            msg.Timeout = TimeSpan.FromMinutes(1);

            return new ResourceRequestPendingNotificationMessage()
            {
                RecipientId = requesterId,
                RequesterId = recipientId,
                ResourceIds = resourceId.HasValue && resourceId.Value != Guid.Empty ? [resourceId.Value] : [],
                PackageIds = packageId.HasValue && packageId.Value != Guid.Empty ? [packageId.Value] : [],
                InitiatedAt = schedule,
                Updated = 1,
            };
        }
    }

    private async Task AddRequestApprovedToOutbox(Guid approverId, Guid recipientId, Guid? resourceId, Guid? packageId, CancellationToken ct)
    {
        await db.UpsertOutboxAsync(
            refId: $"request_approved_{approverId}_{recipientId}",
            handler: "request_approved",
            addValueFactory: msg => AddValue(approverId, recipientId, resourceId, packageId, msg),
            updateValueFactory: (msg, data) => UpdateValue(approverId, recipientId, resourceId, packageId, msg, data),
            cancellationToken: ct
        );

        static RequestApprovedNotificationMessage UpdateValue(
            Guid requesterId,
            Guid recipientId,
            Guid? resourceId,
            Guid? packageId,
            OutboxMessage msg,
            RequestApprovedNotificationMessage data)
        {
            if (data is null)
            {
                Activity.Current?.AddTag(nameof(AddRequestApprovedToOutbox), $"Current outbox message {nameof(ResourceRequestPendingNotificationMessage)} is null? Creating new object.");
                return AddValue(requesterId, recipientId, resourceId, packageId, msg);
            }

            data.Updated++;

            if (resourceId.HasValue && !data.ResourceIds.Contains(resourceId.Value))
            {
                data.ResourceIds = data.ResourceIds.Append(resourceId.Value);
            }

            if (packageId.HasValue && !data.PackageIds.Contains(packageId.Value))
            {
                data.PackageIds = data.PackageIds.Append(packageId.Value);
            }

            var candidate = DateTime.UtcNow.AddMinutes(5.0 / (data.Updated + 1));
            msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

            return data;
        }

        static RequestApprovedNotificationMessage AddValue(
            Guid approverId,
            Guid recipientId,
            Guid? resourceId,
            Guid? packageId,
            OutboxMessage msg)
        {
            var schedule = DateTime.UtcNow.AddMinutes(5);
            msg.Schedule = schedule;
            msg.Timeout = TimeSpan.FromMinutes(1);

            return new RequestApprovedNotificationMessage()
            {
                RecipientId = approverId,
                ApproverId = recipientId,
                ResourceIds = resourceId.HasValue && resourceId.Value != Guid.Empty ? [resourceId.Value] : [],
                PackageIds = packageId.HasValue && packageId.Value != Guid.Empty ? [packageId.Value] : [],
                InitiatedAt = schedule,
                Updated = 1,
            };
        }
    }

    private async Task<Result<RequestDto>> CreateRequestAssignmentPackage(Guid assignmentId, Guid packageId, RequestStatus initialStatus = RequestStatus.Pending, CancellationToken ct = default)
    {
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
            await AddRequestPendingToOutbox(request.Assignment.FromId, request.Assignment.ToId, null, packageId, ct);

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
