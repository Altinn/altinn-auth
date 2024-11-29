using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Register.Models;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class PartiesMock : IParties
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public Task<List<int>> GetKeyRoleParties(int userId, CancellationToken cancellationToken = default)
        {
            List<int> result = new List<int>();
            switch (userId)
            {
                case 20001336:
                    result.Add(50001336);
                    break;
                case 20000095:
                    result.Add(50004222);
                    break;
                default:
                    break;
            }

            return Task.FromResult(result);
        }

        public Task<List<MainUnit>> GetMainUnits(MainUnitQuery subunitPartyIds, CancellationToken cancellationToken = default)
        {
            List<MainUnit> result = new List<MainUnit>();
            foreach (int subunitPartyId in subunitPartyIds.PartyIds)
            {
                switch (subunitPartyId)
                {
                    case 50001335:
                        result.Add(new MainUnit { PartyId = 50001337, SubunitPartyId = 50001335 });
                        break;
                    default:
                        break;
                }
            }            

            return Task.FromResult(result);
        }

        public Task<Party> GetParty(int partyId, CancellationToken cancellationToken = default)
        {
            Party party = null;
            if (partyId == 50740574)
            {
                party = new Party { PartyId = partyId, SSN = "11895696716" };
            }

            return Task.FromResult(party);
        }

        public Task<List<Party>> GetParties(int userId, CancellationToken cancellationToken = default)
        {
            string authorizedPartiesPath = GetAuthorizedPartiesPath(userId);
            if (File.Exists(authorizedPartiesPath))
            {
                string content = File.ReadAllText(authorizedPartiesPath);
                return Task.FromResult((List<Party>)JsonSerializer.Deserialize(content, typeof(List<Party>), _jsonOptions));
            }

            return Task.FromResult<List<Party>>([]);
        }

        public async Task<bool> ValidateSelectedParty(int userId, int partyId, CancellationToken cancellationToken = default)
        {
            bool result = false;

            List<Party> partyList = await GetParties(userId, cancellationToken);

            if (partyList.Count > 0)
            {
                result = partyList.Any(p => p.PartyId == partyId) || partyList.Any(p => p.ChildParties != null && p.ChildParties.Count > 0 && p.ChildParties.Any(cp => cp.PartyId == partyId));
            }

            return result;
        }

        private static string GetAuthorizedPartiesPath(int userId)
        {
            return Path.Combine("Data", "Parties", $"{userId}.json");
        }
    }
}
