using Altinn.AccessManagement.Core.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Maps internal entities to API DTOs.
/// </summary>
public partial class DtoMapper : IDtoMapper
{
    /// <summary>AuthorizedParty to AuthorizedPartyDto</summary>
    public static AuthorizedPartyDto ConvertToAuthorizedPartyDto(AuthorizedParty authorizedParty)
    {
        return new AuthorizedPartyDto()
        {
            PartyUuid = authorizedParty.PartyUuid,
            Name = authorizedParty.Name,
            OrganizationNumber = authorizedParty.OrganizationNumber,
            ParentId = authorizedParty.ParentId,
            PersonId = authorizedParty.PersonId,
            DateOfBirth = authorizedParty.DateOfBirth,
            PartyId = authorizedParty.PartyId,
            Type = (Authorization.Api.Contracts.AccessManagement.Enums.AuthorizedPartyTypeDto)authorizedParty.Type,
            UnitType = authorizedParty.UnitType,
            IsDeleted = authorizedParty.IsDeleted,
            OnlyHierarchyElementWithNoAccess = authorizedParty.OnlyHierarchyElementWithNoAccess,
            AuthorizedRoles = authorizedParty.AuthorizedRoles,
            AuthorizedAccessPackages = authorizedParty.AuthorizedAccessPackages,
            AuthorizedResources = authorizedParty.AuthorizedResources,
            AuthorizedInstances = authorizedParty.AuthorizedInstances.Select(instance => new AuthorizedPartyDto.AuthorizedResourceInstance()
            {
                ResourceId = instance.ResourceId,
                InstanceId = instance.InstanceId
            }).ToList(),
        };
    }

    public static IEnumerable<AuthorizedPartyDto> ConvertToAuthorizedPartiesDto(IEnumerable<AuthorizedParty> authorizedParties)
    {
        return authorizedParties.Select(ConvertToAuthorizedPartyDto);
    }
}
