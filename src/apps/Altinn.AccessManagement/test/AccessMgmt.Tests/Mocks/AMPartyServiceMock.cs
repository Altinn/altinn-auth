using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Platform.Register.Models;

namespace Altinn.AccessManagement.Tests.Mocks
{
    public class AMPartyServiceMock : IAMPartyService
    {
        private static readonly Guid OrganizationPartyType = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d");
        private static readonly Guid PersonPartyType = Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a");

        public Task<MinimalParty> GetByOrgNo(OrganizationNumber orgNo, CancellationToken cancellationToken = default)
        {
            Party party = TestDataParties.Value.Find(p => string.Equals(p.OrgNumber, orgNo.ToString(), StringComparison.Ordinal));
            return Task.FromResult(party is null ? null : ToMinimalParty(party));
        }

        public Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default)
        {
            switch (partyId)
            {
                case 50001337:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 50001337,
                        PartyUuid = Guid.Parse("26de0d55-3b52-4703-9a4d-78cd60353daa"),
                        Name = "Test Org",
                        OrganizationId = "888888888",
                        PartyType = OrganizationPartyType
                    });
                case 50001336:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 50001336,
                        PartyUuid = Guid.Parse("7a07a73e-f0ff-432f-8275-b660f591dc26"),
                        Name = "Test Org2",
                        OrganizationId = "999999999",
                        PartyType = OrganizationPartyType
                    });
                default:
                    Party party = TestDataParties.Value.Find(p => p.PartyId == partyId);
                    return Task.FromResult(party is null ? null : ToMinimalParty(party));
            }
        }

        public Task<MinimalParty> GetByPersonNo(PersonIdentifier personNo, CancellationToken cancellationToken = default)
        {
            Party party = TestDataParties.Value.Find(p => string.Equals(p.SSN, personNo.ToString(), StringComparison.Ordinal));
            return Task.FromResult(party is null ? null : ToMinimalParty(party));
        }

        public Task<MinimalParty> GetByUid(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            Party party = TestDataParties.Value.Find(p => p.PartyUuid == partyUuid);
            return Task.FromResult(party is null ? null : ToMinimalParty(party));
        }

        public Task<MinimalParty> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            switch (userId)
            {
                case 20001337:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 501337,
                        PartyUuid = Guid.Parse("9c257fa7-0daa-4f69-be17-fd6805b15484"),
                        Name = "Test Person",
                        PartyType = PersonPartyType
                    });
                case 20001336:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 501336,
                        PartyUuid = Guid.Parse("58241333-3438-4652-8750-4faff77f6046"),
                        Name = "Test Person",
                        PartyType = PersonPartyType
                    });
                default:
                    throw new NotImplementedException();
            }
        }

        private static MinimalParty ToMinimalParty(Party party)
        {
            return new MinimalParty
            {
                PartyId = party.PartyId,
                PartyUuid = party.PartyUuid ?? Guid.Empty,
                Name = party.Name,
                OrganizationId = party.OrgNumber,
                PartyType = party.PartyTypeName == Altinn.Platform.Register.Enums.PartyType.Person ? PersonPartyType : OrganizationPartyType
            };
        }

        private static readonly Lazy<List<Party>> TestDataParties = new(() =>
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AMPartyServiceMock).Assembly.Location).LocalPath);
            string partiesPath = Path.Combine(unitTestFolder, "Data", "Parties", "parties.json");
            if (!File.Exists(partiesPath))
            {
                return new List<Party>();
            }

            string content = File.ReadAllText(partiesPath);
            List<Party> roots = JsonSerializer.Deserialize<List<Party>>(content) ?? new List<Party>();
            return Flatten(roots).ToList();
        });

        // parties.json nests sub-units under ChildParties; flatten so lookups resolve nested parties too.
        private static IEnumerable<Party> Flatten(IEnumerable<Party> parties)
        {
            foreach (Party party in parties)
            {
                yield return party;
                if (party.ChildParties != null)
                {
                    foreach (Party child in Flatten(party.ChildParties))
                    {
                        yield return child;
                    }
                }
            }
        }
    }
}
