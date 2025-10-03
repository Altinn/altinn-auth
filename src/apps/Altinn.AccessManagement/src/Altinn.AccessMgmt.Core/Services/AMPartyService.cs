using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessMgmt.Core.Services;

/// <summary>
/// Service to look up party information
/// </summary>
public class AMPartyService(IEntityService entityService) : IAmPartyRepository
{
    /// <inheritdoc />
    public async Task<MinimalParty> GetByOrgNo(OrganizationNumber orgNo, CancellationToken cancellationToken = default)
    {
        var entity = await entityService.GetByOrgNo(orgNo.ToString(), cancellationToken: cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return new MinimalParty()
        {
            Name = entity.Name,
            PartyUuid = entity.Id,
            OrganizationId = entity.RefId,
            PartyType = entity.TypeId
        };
    }

    /// <inheritdoc />
    public async Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default)
    {
        var entity = await entityService.GetByPartyId(partyId.ToString(), cancellationToken: cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var party = new MinimalParty()
        {
            Name = entity.Name,
            PartyUuid = entity.Id,
            PartyId = partyId,
            PartyType = entity.TypeId
        };

        if (entity.Type.Id == EntityTypeConstants.Organisation.Id)
        {
            party.OrganizationId = entity.RefId;
        }

        if (entity.Type.Id == EntityTypeConstants.Person.Id)
        {
            party.PersonId = entity.RefId;
        }

        return party;
    }

    /// <inheritdoc />
    public async Task<MinimalParty> GetByPersonNo(PersonIdentifier personNo, CancellationToken cancellationToken = default)
    {
        var entity = await entityService.GetByPersNo(personNo.ToString(), cancellationToken: cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return new MinimalParty()
        {
            Name = entity.Name,
            PartyUuid = entity.Id,
            PersonId = entity.RefId,
            PartyType = entity.TypeId
        };
    }

    public async Task<MinimalParty> GetByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var entity = await entityService.GetByUserId(userId.ToString(), cancellationToken: cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var party = new MinimalParty()
        {
            Name = entity.Name,
            PartyUuid = entity.Id,
            PartyType = entity.TypeId
        };

        if (entity.Type.Id == EntityTypeConstants.Person.Id)
        {
            party.PersonId = entity.RefId;
        }

        return party;
    }

    /// <inheritdoc />
    public async Task<MinimalParty> GetByUuid(Guid partyUuid, CancellationToken cancellationToken = default)
    {
        var entity = await entityService.GetEntity(partyUuid, cancellationToken: cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var party = new MinimalParty()
        {
            Name = entity.Name,
            PartyUuid = entity.Id,
            PartyType = entity.TypeId
        };

        if (entity.Type.Id == EntityTypeConstants.Organisation.Id)
        {
            party.OrganizationId = entity.RefId;
        }

        if (entity.Type.Id == EntityTypeConstants.Person.Id)
        {
            party.PersonId = entity.RefId;
        }

        return party;
    }
}
