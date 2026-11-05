using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using RequestDtoV2 = Altinn.Authorization.Api.Contracts.AccessManagement.V2.RequestDto;
using RequestResourceReferenceDto = Altinn.Authorization.Api.Contracts.AccessManagement.V2.RequestResourceReferenceDto;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// The DtoMapper is a partial class for converting database models and dto models
/// Create a new file for the diffrent areas
/// </summary>
public partial class DtoMapper : IDtoMapper
{
    public static AccessRequest Convert(RequestAssignmentPackage request)
    {
        return new AccessRequest
        {
            Id = request.Id,
            Type = "package",
            LastUpdated = request.Audit_ValidFrom,
            LastUpdatedBy = ConvertToPartyReferenceOrStub(request.LastUpdatedBy, request.Audit_ChangedBy),
            From = ConvertToIdentifiedParty(request.Assignment.From),
            To = ConvertToIdentifiedParty(request.Assignment.To),
            By = ConvertToIdentifiedPartyOrStub(request.Assignment.By, request.Assignment.ById),
            Status = request.Status,
            Package = new RequestReferenceDto() { Id = request.PackageId, ReferenceId = request.Package?.Urn },
        };
    }

    public static AccessRequest Convert(RequestAssignmentResource request)
    {
        return new AccessRequest
        {
            Id = request.Id,
            Type = "resource",
            LastUpdated = request.Audit_ValidFrom,
            LastUpdatedBy = ConvertToPartyReferenceOrStub(request.LastUpdatedBy, request.Audit_ChangedBy),
            From = ConvertToIdentifiedParty(request.Assignment.From),
            To = ConvertToIdentifiedParty(request.Assignment.To),
            By = ConvertToIdentifiedPartyOrStub(request.Assignment.By, request.Assignment.ById),
            Status = request.Status,
            Resource = new RequestReferenceDto() { Id = request.ResourceId, ReferenceId = request.Resource?.RefId },
            RequestedRightKeys = request.RequestedRightKeys,
        };
    }

    /// <summary>
    /// Maps a request to the version 1 response, which is also used by the service owner and system user APIs.
    /// <see cref="RequestDto.Status"/> keeps to the version 1 values, and <see cref="RequestDto.StatusV2"/> has the full status.
    /// </summary>
    public static RequestDto ConvertToV1(AccessRequest request)
    {
        return new RequestDto
        {
            Id = request.Id,
            Status = ConvertToV1Status(request.Status),
            StatusV2 = request.Status,
            Type = request.Type,
            LastUpdated = request.LastUpdated,
            LastUpdatedBy = request.LastUpdatedBy,
            Resource = request.Resource,
            Package = request.Package,
            From = request.From,
            To = request.To,
            By = request.By,
        };
    }

    /// <summary>
    /// Maps a request to the version 2 response, where the status is the full, non-exhaustive status
    /// and the requested rights are part of the resource.
    /// </summary>
    public static RequestDtoV2 ConvertToV2(AccessRequest request)
    {
        return new RequestDtoV2
        {
            Id = request.Id,
            Status = request.Status,
            Type = request.Type,
            LastUpdated = request.LastUpdated,
            LastUpdatedBy = request.LastUpdatedBy,
            Resource = request.Resource is null ? null : new RequestResourceReferenceDto
            {
                Id = request.Resource.Id,
                ReferenceId = request.Resource.ReferenceId,
                RequestedRightKeys = request.RequestedRightKeys,
            },
            Package = request.Package,
            From = request.From,
            To = request.To,
            By = request.By,
        };
    }

    /// <summary>
    /// The status reported in <see cref="RequestDto.Status"/>, which v1 clients may check exhaustively:
    /// statuses added later are reported as their closest version 1 value.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A status without a version 1 value, so a new status must be mapped here.</exception>
    public static RequestStatus ConvertToV1Status(RequestStatusV2 status) => status switch
    {
        RequestStatusV2.None => RequestStatus.None,
        RequestStatusV2.Draft => RequestStatus.Draft,
        RequestStatusV2.Pending => RequestStatus.Pending,
        RequestStatusV2.Approved => RequestStatus.Approved,
        RequestStatusV2.Rejected => RequestStatus.Rejected,
        RequestStatusV2.Withdrawn => RequestStatus.Withdrawn,
        RequestStatusV2.PartialApprove => RequestStatus.Approved,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "The status has no version 1 value."),
    };

    /// <summary>
    /// The statuses to filter on for a status filter from a v1 client. Since <see cref="ConvertToV1Status"/>
    /// reports PartialApprove as Approved, filtering on Approved also includes PartialApprove.
    /// </summary>
    public static IEnumerable<RequestStatusV2> ConvertFromV1StatusFilter(IEnumerable<RequestStatus> statuses)
    {
        var result = statuses.Select(status => (RequestStatusV2)status).ToHashSet();
        if (result.Contains(RequestStatusV2.Approved))
        {
            result.Add(RequestStatusV2.PartialApprove);
        }

        return result;
    }

    /// <summary>
    /// Maps a party the caller is entitled to identify, including its organisation number or
    /// national identity number. Only use this where the caller is itself a party to what the
    /// DTO describes; for anyone else use <see cref="ConvertToPartyReferenceOrStub"/>.
    /// </summary>
    public static PartyEntityDto ConvertToIdentifiedParty(Entity entity)
    {
        return new PartyEntityDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = EntityTypeConstants.TryGetById(entity.TypeId, out var type) ? type.Entity.Name : null,
            Variant = EntityVariantConstants.TryGetById(entity.VariantId, out var variant) ? variant.Entity.Name : null,
            OrganizationIdentifier = entity.OrganizationIdentifier?.ToString(),
            PersonIdentifier = entity.PersonIdentifier?.ToString()
        };
    }

    /// <summary>
    /// As <see cref="ConvertToIdentifiedParty"/>, but falls back to a bare id when the entity
    /// was not loaded.
    /// </summary>
    public static PartyEntityDto? ConvertToIdentifiedPartyOrStub(Entity? entity, Guid? fallbackId)
    {
        if (entity is not null)
        {
            return ConvertToIdentifiedParty(entity);
        }

        return fallbackId is { } id ? new PartyEntityDto { Id = id } : null;
    }

    /// <summary>
    /// Maps a party the caller may see named but must not be able to identify. Used for
    /// LastUpdatedBy, because the design in #3884 states that whoever sent a request must not
    /// learn which individual in the receiving organisation is its access manager.
    /// </summary>
    public static PartyReferenceDto? ConvertToPartyReferenceOrStub(Entity? entity, Guid? fallbackId)
    {
        if (entity is not null)
        {
            return new PartyReferenceDto { Id = entity.Id, Name = entity.Name };
        }

        return fallbackId is { } id ? new PartyReferenceDto { Id = id } : null;
    }
}
