using System.Diagnostics;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class RequestService(
    AppDbContext db,
    IOptions<CoreAppsettings> appsettings,
    IFeatureManager featureManager,
    IPolicyRetrievalPoint policyRetrievalPoint) : IRequestService
{
    /// <inheritdoc/>
    public async Task<Result<AccessRequest>> GetRequest(Guid requestId, CancellationToken ct = default)
    {
        ValidationErrorBuilder error = default;

        var requestResource = await db.RequestAssignmentResources.AsNoTracking()
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Assignment).ThenInclude(a => a.By)
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(t => t.Id == requestId, ct);

        if (requestResource != null)
        {
            await ResolveLastUpdatedBy([requestResource], ct);
            return DtoMapper.Convert(requestResource);
        }

        var requestPackage = await db.RequestAssignmentPackages.AsNoTracking()
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Assignment).ThenInclude(a => a.By)
            .Include(r => r.Package)
            .FirstOrDefaultAsync(t => t.Id == requestId, ct);

        if (requestPackage != null)
        {
            await ResolveLastUpdatedBy([requestPackage], ct);
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
    public async Task<Result<IEnumerable<AccessRequest>>> GetSentRequests(Guid partyId, Guid? toId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default)
    {
        var filter = QuerySentFilter(partyId, toId);
        var requestResources = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResource(filter, status, ct) : new List<RequestAssignmentResource>();
        var requestPackages = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackage(filter, status, ct) : new List<RequestAssignmentPackage>();

        var result = requestResources.Select(DtoMapper.Convert)
            .Union(requestPackages.Select(DtoMapper.Convert));

        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AccessRequest>>> GetReceivedRequests(Guid partyId, Guid? fromId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default)
    {
        var filter = QueryReceivedFilter(partyId, fromId);
        var requestResources = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResource(filter, status, ct) : new List<RequestAssignmentResource>();
        var requestPackages = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackage(filter, status, ct) : new List<RequestAssignmentPackage>();

        var result = requestResources.Select(DtoMapper.Convert)
            .Union(requestPackages.Select(DtoMapper.Convert));

        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<int>> GetSentRequestsCount(Guid partyId, Guid? toId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default)
    {
        var filter = QuerySentFilter(partyId, toId);
        var resourceCount = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResourceCount(filter, status, ct) : 0;
        var packageCount = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackageCount(filter, status, ct) : 0;

        return resourceCount + packageCount;
    }

    /// <inheritdoc/>
    public async Task<Result<int>> GetReceivedRequestsCount(Guid partyId, Guid? fromId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default)
    {
        var filter = QueryReceivedFilter(partyId, fromId);
        var resourceCount = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentResourceCount(filter, status, ct) : 0;
        var packageCount = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase) ? await GetRequestAssignmentPackageCount(filter, status, ct) : 0;

        return resourceCount + packageCount;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ReceivedRequestCountSubunitDto>>> GetReceivedRequestsCountForSubunits(Guid partyId, Guid? fromId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default)
    {
        var subunits = await db.Entities.AsNoTracking()
            .Where(e => e.ParentId == partyId)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

        if (subunits.Count == 0)
        {
            return new List<ReceivedRequestCountSubunitDto>();
        }

        var filter = new RequestFilter(fromId, null, subunits.Select(s => s.Id).ToList());
        var resourceCounts = string.IsNullOrEmpty(type) || type.Equals("resource", StringComparison.OrdinalIgnoreCase)
            ? await BuildRequestAssignmentResourceQuery(filter, status)
                .GroupBy(r => r.Assignment.ToId)
                .Select(g => new { ToId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ToId, g => g.Count, ct)
            : [];
        var packageCounts = string.IsNullOrEmpty(type) || type.Equals("package", StringComparison.OrdinalIgnoreCase)
            ? await BuildRequestAssignmentPackageQuery(filter, status)
                .GroupBy(r => r.Assignment.ToId)
                .Select(g => new { ToId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ToId, g => g.Count, ct)
            : [];

        return subunits.Select(s => new ReceivedRequestCountSubunitDto
        {
            Party = DtoMapper.ConvertToIdentifiedParty(s),
            Count = resourceCounts.GetValueOrDefault(s.Id) + packageCounts.GetValueOrDefault(s.Id),
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<AccessRequest>> UpdateRequest(Guid partyUuid, Guid requestId, RequestStatusV2 status, CancellationToken ct = default)
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
    public async Task<Result<AccessRequest>> CreateResourceRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, Guid resourceId, IEnumerable<string>? requestedRightKeys, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default)
    {
        var systemUserProblem = await ValidateRecipientNotSystemUser(toId, ct);
        if (systemUserProblem is { } resourceGuard)
        {
            return resourceGuard;
        }

        var resource = await db.Resources.Include(r => r.Type).FirstOrDefaultAsync(r => r.Id == resourceId, ct);

        var problem = ValidationComposer.Validate(
            PackageValidation.SelfAssignmentNotAllowed(fromId, toId),
            PackageValidation.ResourceIsAssignable(resource)
        );

        if (problem is { })
        {
            return problem;
        }

        var requestAssignmentResult = await GetOrCreateRequestAssignment(
           fromId: fromId,
           toId: toId,
           roleId: roleId,
           byId: byId,
           ct: ct
           );
        var requestAssignment = requestAssignmentResult.Value;

        return await CreateResourceRequest(requestAssignment, resourceId, requestedRightKeys, status, ct);
    }

    /// <inheritdoc/>
    public async Task<ValidationProblemInstance?> ValidateResourceRightKeys(string resource, IEnumerable<string>? rightKeys, CancellationToken ct = default)
    {
        var requestedKeys = rightKeys?.ToList() ?? [];
        if (requestedKeys.Count == 0)
        {
            return null;
        }

        var policy = DelegationCheckHelper.IsAppResource(resource, out string org, out string app)
            ? await policyRetrievalPoint.GetPolicyAsync(org, app, ct)
            : await policyRetrievalPoint.GetPolicyAsync(resource, ct);

        // A key exists if any rule in the policy mentions it. It is delegable if a Permit rule gives it to a role or
        // access package; keys given only to other subjects, such as the service owner org, or only mentioned
        // in Deny rules, are not.
        HashSet<string> policyKeys = policy is null ? [] : [.. policy.Rules.SelectMany(rule => DelegationCheckHelper.CalculateRightKeys(rule, resource))];
        HashSet<string> delegableKeys = policy is null ? [] : [.. DelegationCheckHelper.DecomposePolicy(WithPermitRulesOnly(policy), resource).Select(right => right.Key)];

        ValidationErrorBuilder errors = default;
        for (int i = 0; i < requestedKeys.Count; i++)
        {
            var key = requestedKeys[i];
            if (key is null || !policyKeys.Contains(key))
            {
                errors.Add(ValidationErrors.RightKeyNotFound, $"/requestedRightKeys/{i}", [new("rightKey", $"Right key '{key}' does not exist in the policy for resource '{resource}'.")]);
            }
            else if (!delegableKeys.Contains(key))
            {
                errors.Add(ValidationErrors.RightKeyNotDelegable, $"/requestedRightKeys/{i}", [new("rightKey", $"Right key '{key}' cannot be delegated for resource '{resource}'.")]);
            }
        }

        errors.TryBuild(out var problem);
        return problem;
    }

    /// <inheritdoc/>
    public async Task<Result<AccessRequest>> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, string package, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default)
    {
        var systemUserProblem = await ValidateRecipientNotSystemUser(toId, ct);
        if (systemUserProblem is { } packageGuard)
        {
            return packageGuard;
        }

        return await CreatePackageRequestInternal(toId, fromId, byId, roleId, package, status, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<AccessRequest>> CreateSystemUserPackageRequest(string organizationNo, Guid systemUserId, string consumerOrgNo, Guid roleId, string package, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default)
    {
        var systemUserProblem = await ValidateSystemUserSender(systemUserId, consumerOrgNo, ct);
        if (systemUserProblem is { } senderGuard)
        {
            return senderGuard;
        }

        var toId = await GetOrganizationIdByOrgNo(organizationNo, ct);

        if (toId is null)
        {
            return OrganizationNotFoundProblem();
        }

        return await CreatePackageRequestInternal(toId.Value, systemUserId, systemUserId, roleId, package, status, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AccessRequest>>> GetSystemUserSentRequests(Guid systemUserId, string consumerOrgNo, string? toOrganizationNo, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default)
    {
        var senderProblem = await ValidateSystemUserSender(systemUserId, consumerOrgNo, ct);
        if (senderProblem is { } senderGuard)
        {
            return senderGuard;
        }

        Guid? toId = null;
        if (!string.IsNullOrEmpty(toOrganizationNo))
        {
            toId = await GetOrganizationIdByOrgNo(toOrganizationNo, ct);
            if (toId is null)
            {
                return OrganizationNotFoundProblem();
            }
        }

        return await GetSentRequests(systemUserId, toId, status, type, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<AccessRequest>> WithdrawSystemUserRequest(Guid systemUserId, string consumerOrgNo, Guid requestId, CancellationToken ct = default)
    {
        var senderProblem = await ValidateSystemUserSender(systemUserId, consumerOrgNo, ct);
        if (senderProblem is { } senderGuard)
        {
            return senderGuard;
        }

        return await UpdateRequest(systemUserId, requestId, RequestStatusV2.Withdrawn, ct);
    }

    private async Task<Guid?> GetOrganizationIdByOrgNo(string organizationNo, CancellationToken ct)
    {
        var organizationTypeId = EntityTypeConstants.Organization.Id;
        return await db.Entities.AsNoTracking()
            .Where(e => e.OrganizationIdentifier == organizationNo && e.TypeId == organizationTypeId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Returns a copy of the policy with only its Permit rules, since <see cref="DelegationCheckHelper.DecomposePolicy"/>
    /// does not check the rule effect. The retrieved policy is left untouched, as it may be cached.
    /// </summary>
    private static XacmlPolicy WithPermitRulesOnly(XacmlPolicy policy)
    {
        var permitPolicy = new XacmlPolicy(policy.PolicyId, policy.RuleCombiningAlgId, policy.Target);
        foreach (var rule in policy.Rules.Where(rule => rule.Effect == XacmlEffectType.Permit))
        {
            permitPolicy.Rules.Add(rule);
        }

        return permitPolicy;
    }

    private static ValidationProblemInstance OrganizationNotFoundProblem()
    {
        ValidationErrorBuilder errors = default;
        errors.Add(ValidationErrors.EntityNotExists, "QUERY/organization");
        errors.TryBuild(out var notFound);
        return notFound;
    }

    private async Task<Result<AccessRequest>> CreatePackageRequestInternal(Guid toId, Guid fromId, Guid byId, Guid roleId, string package, RequestStatusV2 status, CancellationToken ct)
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
            byId: byId,
            ct: ct
        );

        var requestAssignment = requestAssignmentResult.Value;

        return await CreatePackageRequest(requestAssignment, package, status, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<AccessRequest>> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, Guid packageId, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default)
    {
        return await CreatePackageRequest(toId, fromId, byId, roleId, packageId.ToString(), status, ct);
    }

    #region privates

    /// <summary>
    /// Enforces the recipient hard-block that applies to every request type:
    /// a system user can never be the recipient (<paramref name="toId"/>) of a request.
    /// </summary>
    /// <returns>A <see cref="Problems.SystemUserRequestNotAllowed"/> descriptor when the request is not allowed; otherwise <c>null</c>.</returns>
    private async Task<ProblemDescriptor?> ValidateRecipientNotSystemUser(Guid toId, CancellationToken ct)
    {
        var systemUserTypeId = EntityTypeConstants.SystemUser.Id;

        var toIsSystemUser = await db.Entities.AsNoTracking()
            .AnyAsync(e => e.Id == toId && e.TypeId == systemUserTypeId, ct);
        if (toIsSystemUser)
        {
            return Problems.SystemUserRequestNotAllowed;
        }

        return null;
    }

    /// <summary>
    /// Enforces the system-user sender rules for the dedicated system-user request path:
    /// the <see cref="AccessMgmtFeatureFlags.EnableSystemUserRequests"/> feature must be enabled, the
    /// requester (<paramref name="fromId"/>) must resolve to a system user entity and the consumer
    /// organization (<paramref name="consumerOrgNo"/>) must be a KOMM, FYLK or STAT organization.
    /// </summary>
    /// <returns>A <see cref="Problems.SystemUserRequestNotAllowed"/> descriptor when the request is not allowed; otherwise <c>null</c>.</returns>
    private async Task<ProblemDescriptor?> ValidateSystemUserSender(Guid fromId, string consumerOrgNo, CancellationToken ct)
    {
        var systemUserTypeId = EntityTypeConstants.SystemUser.Id;
        var organizationTypeId = EntityTypeConstants.Organization.Id;
        Guid[] allowedConsumerVariants = [EntityVariantConstants.KOMM.Id, EntityVariantConstants.FYLK.Id, EntityVariantConstants.STAT.Id];

        var featureEnabled = await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.EnableSystemUserRequests);
        if (!featureEnabled)
        {
            return Problems.SystemUserRequestNotAllowed;
        }

        // The requester must actually be a system user entity.
        var fromIsSystemUser = fromId != Guid.Empty && await db.Entities.AsNoTracking()
            .AnyAsync(e => e.Id == fromId && e.TypeId == systemUserTypeId, ct);
        if (!fromIsSystemUser)
        {
            return Problems.SystemUserRequestNotAllowed;
        }

        // Only system users owned by public sector consumers (KOMM, FYLK, STAT) may send requests.
        var consumerIsAllowed = !string.IsNullOrEmpty(consumerOrgNo) && await db.Entities.AsNoTracking()
            .AnyAsync(e => e.OrganizationIdentifier == consumerOrgNo && e.TypeId == organizationTypeId && allowedConsumerVariants.Contains(e.VariantId), ct);
        if (!consumerIsAllowed)
        {
            return Problems.SystemUserRequestNotAllowed;
        }

        return null;
    }

    private async Task<Result<AccessRequest>> CreateResourceRequest(RequestAssignment assignment, Guid resourceId, IEnumerable<string>? requestedRightKeys, RequestStatusV2 initialStatus = RequestStatusV2.Pending, CancellationToken ct = default)
    {
        var request = await db.RequestAssignmentResources
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.AssignmentId == assignment.Id && r.ResourceId == resourceId && r.Status == initialStatus, ct);

        if (request is null)
        {
            request = new RequestAssignmentResource
            {
                Status = initialStatus,
                AssignmentId = assignment.Id,
                ResourceId = resourceId,
                RequestedRightKeys = NormalizeRightKeys(requestedRightKeys),
            };
            db.RequestAssignmentResources.Add(request);

            if (initialStatus == RequestStatusV2.Pending)
            {
                await UpsertOutboxMessage(ct);
                if (await db.SaveChangesWithOutboxRetry(() => UpsertOutboxMessage(ct), ct) == 0)
                {
                    return Problems.RequestCreationFailed;
                }
            }
            else if (await db.SaveChangesAsync(ct) == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);

        async Task UpsertOutboxMessage(CancellationToken ct)
        {
            await RequestPendingNotification.Upsert(
                db,
                assignment.FromId,
                assignment.ToId,
                resourceId,
                null,
                appsettings?.Value?.Request?.NotifyRequestPendingInSeconds ?? 60 * 15,
                ct
            );
        }
    }

    /// <summary>
    /// Removes duplicate keys, keeping the order they were given in. An empty list is stored as null,
    /// so null always means that no specific rights were requested.
    /// </summary>
    private static List<string>? NormalizeRightKeys(IEnumerable<string>? rightKeys)
    {
        var keys = rightKeys?.Distinct(StringComparer.Ordinal).ToList();
        return keys is { Count: > 0 } ? keys : null;
    }

    private async Task<Result<AccessRequest>> CreatePackageRequest(RequestAssignment assignment, string package, RequestStatusV2 initialStatus = RequestStatusV2.Pending, CancellationToken ct = default)
    {
        if (!PackageConstants.TryGetByAll(package, out var packageObj))
        {
            throw new ArgumentException($"Package '{package}' not found", nameof(package));
        }

        var packageId = packageObj.Id;

        var request = await db.RequestAssignmentPackages
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.AssignmentId == assignment.Id && r.PackageId == packageId && r.Status == initialStatus, cancellationToken: ct);

        if (request is null)
        {
            request = new RequestAssignmentPackage
            {
                Status = initialStatus,
                AssignmentId = assignment.Id,
                PackageId = packageId,
            };
            db.RequestAssignmentPackages.Add(request);

            if (initialStatus == RequestStatusV2.Pending)
            {
                await UpsertOutboxMessage(ct);
                if (await db.SaveChangesWithOutboxRetry(() => UpsertOutboxMessage(ct), ct) == 0)
                {
                    return Problems.RequestCreationFailed;
                }
            }
            else if (await db.SaveChangesAsync(ct) == 0)
            {
                return Problems.RequestCreationFailed;
            }
        }

        return await GetRequest(request.Id, ct);

        async Task UpsertOutboxMessage(CancellationToken ct)
        {
            await RequestPendingNotification.Upsert(
                db,
                assignment.FromId,
                assignment.ToId,
                null,
                packageId,
                appsettings?.Value?.Request?.NotifyRequestPendingInSeconds ?? 60 * 15,
                ct
            );
        }
    }

    private static Result<AccessRequest> VerifyRequestStatusUpdate(AccessRequest request, Guid partyUuid, RequestStatusV2 status)
    {
        ValidationErrorBuilder errorBuilder = default;

        switch (request.Status)
        {
            case RequestStatusV2.Draft:
                ValidateDraft(request, partyUuid, status, ref errorBuilder);
                break;

            case RequestStatusV2.Pending:
                ValidatePending(request, partyUuid, status, ref errorBuilder);
                break;

            case RequestStatusV2.Approved:
            case RequestStatusV2.PartialApprove:
            case RequestStatusV2.Rejected:
            case RequestStatusV2.Withdrawn:
                AddUnsupportedStatusError(request, status, ref errorBuilder);
                break;
        }

        errorBuilder.TryBuild(out var problems);

        return problems != null ? problems : request;
    }

    private static void ValidateDraft(AccessRequest request, Guid partyUuid, RequestStatusV2 status, ref ValidationErrorBuilder errorBuilder)
    {
        if (status != RequestStatusV2.Pending && status != RequestStatusV2.Withdrawn)
        {
            AddUnsupportedStatusError(request, status, ref errorBuilder);
            return;
        }

        if (request.From.Id != partyUuid)
        {
            AddUnauthorizedStatusError(request, status, ref errorBuilder);
        }
    }

    private static void ValidatePending(AccessRequest request, Guid partyUuid, RequestStatusV2 status, ref ValidationErrorBuilder errorBuilder)
    {
        switch (status)
        {
            case RequestStatusV2.Withdrawn:
                if (request.From.Id != partyUuid)
                {
                    AddUnauthorizedStatusError(request, status, ref errorBuilder);
                }

                break;

            case RequestStatusV2.Approved:
            case RequestStatusV2.Rejected:
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

    private static void AddUnsupportedStatusError(AccessRequest request, RequestStatusV2 status, ref ValidationErrorBuilder errorBuilder)
    {
        var paramName = "party";

        errorBuilder.Add(
            ValidationErrors.RequestUnsupportedStatusUpdate,
            $"$QUERY/{paramName}",
            [new(paramName, $"Changing request status from {request.Status} to {status} is not allowed.")]
        );
    }

    private static void AddUnauthorizedStatusError(AccessRequest request, RequestStatusV2 status, ref ValidationErrorBuilder errorBuilder)
    {
        var paramName = "party";

        errorBuilder.Add(
            ValidationErrors.RequestUnauthorizedStatusUpdate,
            $"$QUERY/{paramName}",
            [new(paramName, $"Unable to update {request.Id} from {request.Status} to {status}")]
        );
    }

    /// <summary>
    /// System users cannot receive notifications, so notifications addressed to them are suppressed.
    /// </summary>
    private Task<bool> IsSystemUser(Guid entityId, CancellationToken ct)
    {
        var systemUserTypeId = EntityTypeConstants.SystemUser.Id;
        return db.Entities.AsNoTracking().AnyAsync(e => e.Id == entityId && e.TypeId == systemUserTypeId, ct);
    }

    private async Task<Result<AccessRequest>> UpdatePackageRequestStatus(Guid id, RequestStatusV2 status, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var request = await db.RequestAssignmentPackages
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (request is not { })
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsFound, nameof(db.RequestAssignmentPackages));
            errorBuilder.TryBuild(out var errors);
            return errors;
        }

        var isRequestFromDraftToPending = request.Status == RequestStatusV2.Draft && status == RequestStatusV2.Pending;
        if (isRequestFromDraftToPending)
        {
            await UpsertPendingOutboxMessage(request, ct);
        }

        request.Status = status;
        if (status == RequestStatusV2.Withdrawn)
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

        var notifyRequester = !await IsSystemUser(request.Assignment.FromId, ct);
        if (notifyRequester && (status == RequestStatusV2.Approved || status == RequestStatusV2.Rejected))
        {
            await UpsertReviewedOutboxMessage(request, ct);
        }

        int saved = 0;
        if (isRequestFromDraftToPending)
        {
            saved = await db.SaveChangesWithOutboxRetry(() => UpsertPendingOutboxMessage(request, ct), ct);
        }
        else if (notifyRequester)
        {
            saved = await db.SaveChangesWithOutboxRetry(() => UpsertReviewedOutboxMessage(request, ct), ct);
        }
        else
        {
            saved = await db.SaveChangesAsync(ct);
        }

        if (saved == 0)
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsAffected, nameof(db.RequestAssignmentPackages));
        }

        if (errorBuilder.TryBuild(out var problems))
        {
            return problems;
        }

        return await GetRequest(id, ct);

        async Task UpsertPendingOutboxMessage(RequestAssignmentPackage request, CancellationToken ct)
        {
            await RequestPendingNotification.Upsert(
                db,
                request.Assignment.FromId,
                request.Assignment.ToId,
                null,
                request.PackageId,
                appsettings?.Value?.Request?.NotifyRequestPendingInSeconds ?? 60 * 15,
                ct
            );
        }

        async Task UpsertReviewedOutboxMessage(RequestAssignmentPackage request, CancellationToken ct)
        {
            await RequestReviewedNotification.Upsert(
                db,
                request.Assignment.ToId,
                request.Assignment.FromId,
                null,
                request.PackageId,
                status == RequestStatusV2.Approved,
                appsettings?.Value?.Request?.NotifyRequestApprovedInSeconds ?? (60 * 15),
                ct
            );
        }
    }

    private async Task<Result<AccessRequest>> UpdateResourceRequestStatus(Guid id, RequestStatusV2 status, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var request = await db.RequestAssignmentResources
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (request is not { })
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsFound, nameof(db.RequestAssignmentResources));
            errorBuilder.TryBuild(out var errors);
            return errors;
        }

        var isRequestFromDraftToPending = request.Status == RequestStatusV2.Draft && status == RequestStatusV2.Pending;
        if (isRequestFromDraftToPending)
        {
            await UpsertPendingOutboxMessage(request, ct);
        }

        request.Status = status;
        if (status == RequestStatusV2.Withdrawn)
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

        var notifyRequester = !await IsSystemUser(request.Assignment.FromId, ct);
        if (notifyRequester && (status == RequestStatusV2.Approved || status == RequestStatusV2.Rejected))
        {
            await UpsertReviewedOutboxMessage(request, ct);
        }

        int saved;
        if (isRequestFromDraftToPending)
        {
            saved = await db.SaveChangesWithOutboxRetry(() => UpsertPendingOutboxMessage(request, ct), ct);
        }
        else if (notifyRequester)
        {
            saved = await db.SaveChangesWithOutboxRetry(() => UpsertReviewedOutboxMessage(request, ct), ct);
        }
        else
        {
            saved = await db.SaveChangesAsync(ct);
        }

        if (saved == 0)
        {
            errorBuilder.Add(ValidationErrors.DbNoRowsAffected, nameof(db.RequestAssignmentResources));
        }

        if (errorBuilder.TryBuild(out var problems))
        {
            return problems;
        }

        return await GetRequest(id, ct);

        async Task UpsertPendingOutboxMessage(RequestAssignmentResource request, CancellationToken ct)
        {
            await RequestPendingNotification.Upsert(
                db,
                request.Assignment.FromId,
                request.Assignment.ToId,
                request.ResourceId,
                null,
                appsettings?.Value?.Request?.NotifyRequestPendingInSeconds ?? 60 * 15,
                ct
            );
        }

        async Task UpsertReviewedOutboxMessage(RequestAssignmentResource request, CancellationToken ct)
        {
            await RequestReviewedNotification.Upsert(
                db,
                request.Assignment.ToId,
                request.Assignment.FromId,
                request.ResourceId,
                null,
                status == RequestStatusV2.Approved,
                appsettings?.Value?.Request?.NotifyRequestApprovedInSeconds ?? (60 * 15),
                ct
            );
        }
    }

    private async Task<IEnumerable<RequestAssignmentResource>> GetRequestAssignmentResource(RequestFilter filter, IEnumerable<RequestStatusV2> status, CancellationToken ct)
    {
        ValidateFilter(filter);

        var requests = await BuildRequestAssignmentResourceQuery(filter, status)
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Assignment).ThenInclude(a => a.By)
            .Include(r => r.Resource)
            .ToListAsync(cancellationToken: ct);

        await ResolveLastUpdatedBy(requests, ct);
        return requests;
    }

    private async Task<IEnumerable<RequestAssignmentPackage>> GetRequestAssignmentPackage(RequestFilter filter, IEnumerable<RequestStatusV2> status, CancellationToken ct)
    {
        ValidateFilter(filter);

        var requests = await BuildRequestAssignmentPackageQuery(filter, status)
            .Include(r => r.Assignment).ThenInclude(a => a.From)
            .Include(r => r.Assignment).ThenInclude(a => a.To)
            .Include(r => r.Assignment).ThenInclude(a => a.Role)
            .Include(r => r.Assignment).ThenInclude(a => a.By)
            .Include(r => r.Package)
            .ToListAsync(cancellationToken: ct);

        await ResolveLastUpdatedBy(requests, ct);
        return requests;
    }

    /// <summary>
    /// Fills in <see cref="IHasLastUpdatedBy.LastUpdatedBy"/> from the rows' audit values.
    /// </summary>
    /// <remarks>
    /// Audit_ChangedBy is not a foreign key, so the party is looked up here rather than through
    /// an EF relationship. One batched query covers the whole result set, and an audit value with
    /// no matching entity simply leaves the navigation null, which the mapper renders as a bare id.
    /// </remarks>
    private async Task ResolveLastUpdatedBy<T>(IReadOnlyCollection<T> requests, CancellationToken ct)
        where T : IHasLastUpdatedBy
    {
        var ids = requests
            .Select(r => r.Audit_ChangedBy)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var parties = await db.Entities
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        foreach (var request in requests)
        {
            if (request.Audit_ChangedBy is { } id && parties.TryGetValue(id, out var party))
            {
                request.LastUpdatedBy = party;
            }
        }
    }

    private async Task<int> GetRequestAssignmentResourceCount(RequestFilter filter, IEnumerable<RequestStatusV2> status, CancellationToken ct)
    {
        ValidateFilter(filter);
        return await BuildRequestAssignmentResourceQuery(filter, status).CountAsync(cancellationToken: ct);
    }

    private async Task<int> GetRequestAssignmentPackageCount(RequestFilter filter, IEnumerable<RequestStatusV2> status, CancellationToken ct)
    {
        ValidateFilter(filter);
        return await BuildRequestAssignmentPackageQuery(filter, status).CountAsync(cancellationToken: ct);
    }

    private IQueryable<RequestAssignmentResource> BuildRequestAssignmentResourceQuery(RequestFilter filter, IEnumerable<RequestStatusV2> status)
    {
        return db.RequestAssignmentResources
            .WhereIf(filter.FromId.HasValue, r => r.Assignment.FromId == filter.FromId.Value)
            .WhereIf(filter.ToId.HasValue, r => r.Assignment.ToId == filter.ToId.Value)
            .WhereIf(filter.ToIds is { Count: > 0 }, r => filter.ToIds.Contains(r.Assignment.ToId))
            .WhereIf(status?.Any() == true, r => status.Contains(r.Status));
    }

    private IQueryable<RequestAssignmentPackage> BuildRequestAssignmentPackageQuery(RequestFilter filter, IEnumerable<RequestStatusV2> status)
    {
        return db.RequestAssignmentPackages
            .WhereIf(filter.FromId.HasValue, r => r.Assignment.FromId == filter.FromId.Value)
            .WhereIf(filter.ToId.HasValue, r => r.Assignment.ToId == filter.ToId.Value)
            .WhereIf(filter.ToIds is { Count: > 0 }, r => filter.ToIds.Contains(r.Assignment.ToId))
            .WhereIf(status?.Any() == true, r => status.Contains(r.Status));
    }

    private static void ValidateFilter(RequestFilter filter)
    {
        if (!filter.FromId.HasValue && !filter.ToId.HasValue && filter.ToIds is not { Count: > 0 })
        {
            throw new ArgumentException("At least one of fromId, toId or toIds must be provided");
        }
    }

    private async Task<Result<RequestAssignment>> GetOrCreateRequestAssignment(Guid fromId, Guid toId, Guid roleId, Guid byId, CancellationToken ct = default)
    {
        /*
        A Request from Kari by NAV to BakerAS for AppResource01.
        Will create an Assignment from BakerAS to Kari with an AssignmentResource for AppResource01.
        */

        var request = await db.RequestAssignments.FirstOrDefaultAsync(r => r.FromId == fromId && r.ToId == toId && r.RoleId == roleId && r.ById == byId, ct);
        if (request == null)
        {
            request = new RequestAssignment
            {
                FromId = fromId,
                ToId = toId,
                RoleId = roleId,
                ById = byId
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

    internal record RequestFilter(Guid? FromId, Guid? ToId, IReadOnlyCollection<Guid>? ToIds = null);

    #endregion
}
