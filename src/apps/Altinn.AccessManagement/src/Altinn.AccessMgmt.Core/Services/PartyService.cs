using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.Party;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public class PartyService(AppDbContext db) : IPartyService
{
    /// <inheritdoc />
    public async Task<Result<AddPartyResultDto>> AddParty(PartyBaseDto party, CancellationToken cancellationToken = default)
    {
        AddPartyResultDto result = new() { PartyUuid = party.PartyUuid, PartyCreated = false };

        var exists = await db.Entities
            .AnyAsync(e => e.Id == party.PartyUuid, cancellationToken);

        if (!exists)
        {
            // Info: For now this is restricted to SystemUser, SI_EDU and SI_EMAIL as it does not have any logic to set the correct RefId based on entity type Organisation number for Organisation and Fødselsnummer for Person. 
            if (!IsValidPartyType(party))
            {
                return Problems.UnsupportedEntityType.Create([new("entityType", party.EntityType)]);
            }

            var entityType = await db.EntityTypes
                .FirstOrDefaultAsync(t => t.Name == party.EntityType, cancellationToken);

            if (entityType == null)
            {
                return Problems.EntityTypeNotFound.Create([new("entityType", party.EntityType)]);
            }

            var entityVariant = await db.EntityVariants
                .Where(t => t.TypeId == entityType.Id && t.Name.ToUpper() == party.EntityVariantType.ToUpper())
                .FirstOrDefaultAsync(cancellationToken);

            if (entityVariant == null)
            {
                return Problems.EntityVariantNotFoundOrInvalid.Create([new("entityVariantId", party.EntityVariantType)]);
            }

            Entity entity = new Entity
            {
                Id = party.PartyUuid,
                Name = party.DisplayName,
                TypeId = entityType.Id,
                VariantId = entityVariant.Id,
                RefId = party.EntityType.Equals(EntityTypeConstants.SystemUser.Entity.Name, StringComparison.InvariantCultureIgnoreCase) ? party.PartyUuid.ToString() : null,
                PartyId = party.PartyId,
                UserId = party.UserId,
                EmailIdentifier = party.EmailIdentifier?.Trim()?.ToLowerInvariant(),
            };

            db.Entities.Add(entity);
            var res = await db.SaveChangesAsync(cancellationToken);

            if (res > 0)
            {
                result.PartyCreated = true;
            }
        }

        return result;
    }

    private static bool IsValidPartyType(PartyBaseDto party)
    {
        if (party.EntityType.Equals(EntityTypeConstants.SystemUser.Entity.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!party.EntityType.Equals(EntityTypeConstants.SelfIdentified.Entity.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }
               
        if (party.EntityVariantType.Equals(EntityVariantConstants.SI_EDU.Entity.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (party.EntityVariantType.Equals(EntityVariantConstants.SI_EMAIL.Entity.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
