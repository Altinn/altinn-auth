using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Azure.Core;
using System.Text;

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
            From = ConvertToPartyEntityDto(request.Assignment.From),
            To = ConvertToPartyEntityDto(request.Assignment.To),
            Status = request.Status,
            Package = new RequestRefrenceDto() { Id = request.PackageId, ReferenceId = request.Package?.Urn },
        };
    }

    public static RequestDto Convert(RequestAssignmentResource request)
    {
        return new RequestDto
        {
            Id = request.Id,
            Type = "resource",
            LastUpdated = request.Audit_ValidFrom,
            From = ConvertToPartyEntityDto(request.Assignment.From),
            To = ConvertToPartyEntityDto(request.Assignment.To),
            Status = request.Status,
            Resource = new RequestRefrenceDto() { Id = request.ResourceId, ReferenceId = request.Resource?.RefId },
        };
    }

    public static PartyEntityDto ConvertToPartyEntityDto(Entity entity)
    {
        return new PartyEntityDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type?.ToString(),
            Variant = entity.Variant?.ToString(),
            OrganizationIdentifier = entity.OrganizationIdentifier?.ToString(),
            PersonIdentifier = entity.PersonIdentifier?.ToString()
        };
    }
}
