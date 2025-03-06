using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Exceptions;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Altinn.Platform.Events.Tests.Mocks
{
    public class RegisterServiceMock : IRegisterService
    {
        private readonly int _partiesCollection;
        private IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

        public RegisterServiceMock(int partiesCollection = 1)
        {
            _partiesCollection = partiesCollection;
        }

        public async Task<Party> GetParty(int partyId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"p:{partyId}";
            if (!_memoryCache.TryGetValue(cacheKey, out Party party))
            {
                string partyPath = GetPartyPath(partyId);
                if (File.Exists(partyPath))
                {
                    string content = await File.ReadAllTextAsync(partyPath, cancellationToken);
                    party = JsonConvert.DeserializeObject<Party>(content);
                }

                if (party != null)
                {
                    PutInCache(cacheKey, 10, party);
                }
            }

            return await Task.FromResult(party);
        }

        public Task<List<Party>> GetPartiesAsync(List<int> partyIds, bool includeSubunits = false, CancellationToken cancellationToken = default)
        {
            List<Party> parties = new List<Party>();
            List<int> partyIdsNotInCache = new List<int>();

            foreach (int partyId in partyIds.Distinct())
            {
                if (_memoryCache.TryGetValue($"p:{partyId}|inclSubunits:{includeSubunits}", out Party party))
                {
                    parties.Add(party);
                }
                else
                {
                    partyIdsNotInCache.Add(partyId);
                }
            }

            if (partyIdsNotInCache.Count == 0)
            {
                return Task.FromResult(parties);
            }

            List<Party> remainingParties = new();
            foreach (int partyId in partyIdsNotInCache)
            {
                Party party = GetParty(partyId, cancellationToken).Result;
                if (party != null)
                {
                    remainingParties.Add(party);
                }
            }

            if (remainingParties.Count > 0)
            {
                foreach (Party party in remainingParties)
                {
                    if (party?.PartyId != null)
                    {
                        parties.Add(party);
                        PutInCache($"p:{party.PartyId}|inclSubunits:{includeSubunits}", 10, party);
                    }
                }
            }

            return Task.FromResult(parties);
        }

        public Task<List<Party>> GetPartiesAsync(List<Guid> partyUuids, bool includeSubunits = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Party> PartyLookup(string orgNo, string person, CancellationToken cancellationToken = default)
        {
            string cacheKey;
            PartyLookup partyLookup;

            if (!string.IsNullOrWhiteSpace(orgNo))
            {
                cacheKey = $"org:{orgNo}";
                partyLookup = new PartyLookup { OrgNo = orgNo };
            }
            else if (!string.IsNullOrWhiteSpace(person))
            {
                cacheKey = $"fnr:{person}";
                partyLookup = new PartyLookup { Ssn = person };
            }
            else
            {
                return null;
            }

            if (!_memoryCache.TryGetValue(cacheKey, out Party party))
            {
                string eventsPath = Path.Combine(GetPartiesPath(), $@"{_partiesCollection}.json");

                if (File.Exists(eventsPath))
                {
                    string content = await File.ReadAllTextAsync(eventsPath, cancellationToken);
                    List<Party> parties = JsonConvert.DeserializeObject<List<Party>>(content);

                    if (!string.IsNullOrEmpty(orgNo))
                    {
                        party = parties.Where(p => p.OrgNumber != null && p.OrgNumber.Equals(orgNo)).FirstOrDefault();
                    }
                    else
                    {
                        party = parties.Where(p => p.SSN != null && p.SSN.Equals(person)).FirstOrDefault();
                    }
                }

                if (party != null)
                {
                    PutInCache(cacheKey, 10, party);
                }
            }

            return party != null
                ? await Task.FromResult(party)
                : throw await PlatformHttpException.CreateAsync(new HttpResponseMessage
                { Content = new StringContent(string.Empty), StatusCode = System.Net.HttpStatusCode.NotFound });
        }

        private static string GetPartiesPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(RegisterServiceMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Register");
        }

        private static string GetPartyPath(int partyId)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(RegisterServiceMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Register", partyId.ToString() + ".json");
        }

        private void PutInCache(string cachekey, int cacheTimeout, object cacheObject)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, cacheTimeout, 0));

            _memoryCache.Set(cachekey, cacheObject, cacheEntryOptions);
        }
    }
}
