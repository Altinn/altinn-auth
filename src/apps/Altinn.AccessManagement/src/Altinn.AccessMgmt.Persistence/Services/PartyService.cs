using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services
{
    public class PartyService(
        IEntityRepository entityRepository,
        IEntityTypeRepository entityTypeRepository,
        IEntityVariantRepository entityVariantRepository
        ) : IPartyService
    {
        private readonly IEntityRepository _entityRepository = entityRepository;
        private readonly IEntityTypeRepository _entityTypeRepository = entityTypeRepository;
        private readonly IEntityVariantRepository _entityVariantRepository = entityVariantRepository;

        public async Task<Result<AddPartyResult>> AddParty(PartyBaseInternal party, ChangeRequestOptions options, CancellationToken cancellationToken = default)
        {
            AddPartyResult result = new() { PartyUuid = party.PartyUuid, PartyCreated = false };

            var entityExist = await _entityRepository.Get(e => e.Id, party.PartyUuid, cancellationToken: cancellationToken);

            if (!entityExist.Any())
            {
                /*
                 * Info: For now this is restricted to SystemUser as it does not have any logic to set the correct RefId based on entity type Organisation number for Organisation and Fødselsnummer for Person. 
                 * Also there should be a matching record in the EntityLookup table for required lookup values SystemUSer does not have any lookup values as the only id it has is its PartyUuid.
                 */
                if (!party.EntityType.Equals("Systembruker", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Problems.UnsupportedEntityType.Create([new("entityType", party.EntityType.ToString())]);
                }

                var entityType = (await _entityTypeRepository.Get(t => t.Name, party.EntityType)).FirstOrDefault();
                if (entityType == null)
                {
                    return Problems.EntityTypeNotFound.Create([new("entityType", party.EntityType)]);
                }

                var entityVariant = (await _entityVariantRepository.Get(t => t.TypeId, entityType.Id)).FirstOrDefault(t => t.Name.Equals(party.EntityVariantType, StringComparison.OrdinalIgnoreCase));
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
                    RefId = party.PartyUuid.ToString()
                };

                var res = await _entityRepository.Create(entity, options, cancellationToken);

                if (res > 0)
                {
                    result.PartyCreated = true;
                }                
            }
            
            return result;
        }
    }
}
