using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// The DtoMapper is a partial class for converting database models and dto models
/// Create a new file for the diffrent areas
/// </summary>
public partial class DtoMapper : IDtoMapper
{
    public static RequestDto Convert(RequestAssignmentPackage request)
    {
        return new RequestDto
        {
            Id = request.Id,
            Type = "package",
            LastUpdated = request.Audit_ValidFrom,
            LastUpdatedBy = request.Audit_ChangedBy,
            From = ConvertToPartyEntityDto(request.Assignment.From),
            To = ConvertToPartyEntityDto(request.Assignment.To),
            By = ConvertToPartyEntityDtoOrStub(request.Assignment.By, request.Assignment.ById),
            Status = request.Status,
            Package = new RequestReferenceDto() { Id = request.PackageId, ReferenceId = request.Package?.Urn },
        };
    }

    public static RequestDto Convert(RequestAssignmentResource request)
    {
        return new RequestDto
        {
            Id = request.Id,
            Type = "resource",
            LastUpdated = request.Audit_ValidFrom,
            LastUpdatedBy = request.Audit_ChangedBy,
            From = ConvertToPartyEntityDto(request.Assignment.From),
            To = ConvertToPartyEntityDto(request.Assignment.To),
            By = ConvertToPartyEntityDtoOrStub(request.Assignment.By, request.Assignment.ById),
            Status = request.Status,
            Resource = new RequestReferenceDto() { Id = request.ResourceId, ReferenceId = request.Resource?.RefId },
        };
    }

    public static PartyEntityDto ConvertToPartyEntityDto(Entity entity)
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

    public static PartyEntityDto? ConvertToPartyEntityDtoOrStub(Entity? entity, Guid? fallbackId)
    {
        if (entity is not null)
        {
            return ConvertToPartyEntityDto(entity);
        }

        return fallbackId is { } id ? new PartyEntityDto { Id = id } : null;
    }
}
