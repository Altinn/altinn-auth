using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessMgmt.Persistence.Services
{
    /// <summary>
    /// Repository service to lookupp party information
    /// </summary>
    public class AMPartyService(IEntityLookupRepository entityLookupRepository) : IAmPartyRepository
    {
        private readonly IEntityLookupRepository entityLookupRepository = entityLookupRepository;

        /// <inheritdoc />
        public async Task<MinimalParty> GetByOrgNo(Authorization.Api.Contracts.Register.OrganizationNumber orgNo, CancellationToken cancellationToken = default)
        {
            GenericFilterBuilder<EntityLookup> filter = entityLookupRepository.CreateFilterBuilder();
            filter.Equal(t => t.Key, "OrganizationIdentifier");
            filter.Equal(t => t.Value, orgNo.ToString());

            IEnumerable<ExtEntityLookup> res = await entityLookupRepository.GetExtended(filter, cancellationToken: cancellationToken);

            if (res == null || !res.Any())
            {
                return null;
            }

            if (res.Count() > 1)
            {
                throw new InvalidOperationException("Multiple matches found for the given criteria.Should never happen.");
            }

            ExtEntityLookup extEntityLookup = res.First();

            return new MinimalParty()
            {
                Name = extEntityLookup.Entity.Name,
                PartyUuid = extEntityLookup.Entity.Id,
                OrganizationId = extEntityLookup.Value,
            };
        }

        /// <inheritdoc />
        public Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<MinimalParty> GetByPersonNo(PersonIdentifier personNo, CancellationToken cancellationToken = default)
        {
            GenericFilterBuilder<EntityLookup> filter = entityLookupRepository.CreateFilterBuilder();
            filter.Equal(t => t.Key, "PersonIdentifier");
            filter.Equal(t => t.Value, personNo.ToString());

            IEnumerable<ExtEntityLookup> res = await entityLookupRepository.GetExtended(filter, cancellationToken: cancellationToken);

            if (res == null || !res.Any())
            {
                return null;
            }

            if (res.Count() > 1)
            {
                throw new InvalidOperationException("Multiple matches found for the given criteria. Should never happen.");
            }

            ExtEntityLookup extEntityLookup = res.First();

            return new MinimalParty()
            {
                Name = extEntityLookup.Entity.Name,
                PartyUuid = extEntityLookup.Entity.Id,
                OrganizationId = extEntityLookup.Value,
            };
        }

        /// <inheritdoc />
        public async Task<MinimalParty> GetByUuid(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            IEnumerable<ExtEntityLookup> parties = await entityLookupRepository.GetExtended(t => t.EntityId, partyUuid, cancellationToken: cancellationToken);
            var res = parties.ToDictionary(t => t.Key, t => t.Value);

            if (res.Count == 0)
            {
                return null;
            }

            MinimalParty party = new()
            {
                PartyUuid = partyUuid,
                Name = parties.First().Entity.Name
            };

            if (res.TryGetValue("OrganizationIdentifier", out string orgNo))
            {
                party.OrganizationId = orgNo;
            }

            if (res.TryGetValue("PersonIdentifier", out string personNo))
            {
                party.PersonId = personNo;
            }

            if (res.TryGetValue("Name", out string name))
            {
                party.Name = name;
            }

            return party;
        }
    }
}
