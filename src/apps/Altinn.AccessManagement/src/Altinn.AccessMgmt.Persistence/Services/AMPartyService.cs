using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Models;
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
                PartyType = extEntityLookup.Entity.TypeId
            };
        }

        /// <inheritdoc />
        public async Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default)
        {
            GenericFilterBuilder<EntityLookup> filter = entityLookupRepository.CreateFilterBuilder();
            filter.Equal(t => t.Key, "PartyId");
            filter.Equal(t => t.Value, partyId.ToString());

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
            Guid partyType = extEntityLookup.Entity.TypeId;
            string personId = null;
            string organizationId = null;
            
            if (partyType == Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a"))
            {
                personId = extEntityLookup.Entity.RefId;
            }

            if (partyType == Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"))
            {
                organizationId = extEntityLookup.Entity.RefId;
            }

            return new MinimalParty()
            {
                Name = extEntityLookup.Entity.Name,
                PartyUuid = extEntityLookup.Entity.Id,
                PartyId = partyId,
                PersonId = personId,
                OrganizationId = organizationId,
                PartyType = partyType
            };
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
                PartyType = extEntityLookup.Entity.TypeId
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
                Name = parties.First().Entity.Name,
                PartyType = parties.First().Entity.TypeId
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

        public async Task<MinimalParty> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            GenericFilterBuilder<EntityLookup> filter = entityLookupRepository.CreateFilterBuilder();
            filter.Equal(t => t.Key, "UserId");
            filter.Equal(t => t.Value, userId.ToString());

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
            Guid partyType = extEntityLookup.Entity.TypeId;
            string personId = null;
            
            if (partyType == Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a")) 
            {
                personId = extEntityLookup.Entity.RefId;
            }
            
            return new MinimalParty()
            {
                Name = extEntityLookup.Entity.Name,
                PartyUuid = extEntityLookup.Entity.Id,
                PersonId = personId,
                PartyType = partyType
            };
        }
    }
}
