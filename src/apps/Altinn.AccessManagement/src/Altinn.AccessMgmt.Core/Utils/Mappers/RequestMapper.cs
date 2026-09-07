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
            LastUpdatedBy = ConvertToPartyReferenceOrStub(request.LastUpdatedBy, request.Audit_ChangedBy),
            From = ConvertToIdentifiedParty(request.Assignment.From),
            To = ConvertToIdentifiedParty(request.Assignment.To),
            By = ConvertToIdentifiedPartyOrStub(request.Assignment.By, request.Assignment.ById),
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
            LastUpdatedBy = ConvertToPartyReferenceOrStub(request.LastUpdatedBy, request.Audit_ChangedBy),
            From = ConvertToIdentifiedParty(request.Assignment.From),
            To = ConvertToIdentifiedParty(request.Assignment.To),
            By = ConvertToIdentifiedPartyOrStub(request.Assignment.By, request.Assignment.ById),
            Status = request.Status,
            Resource = new RequestReferenceDto() { Id = request.ResourceId, ReferenceId = request.Resource?.RefId },
        };
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
