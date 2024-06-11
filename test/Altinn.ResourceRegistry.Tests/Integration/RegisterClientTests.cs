using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Integration.Clients;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Integration;

public class RegisterClientTests
{
    private readonly MockRegisterPartyLookupHandler _handler;
    private readonly RegisterClient _client;

    private int _nextPartyId = 1;

    public RegisterClientTests()
    {
        _handler = new();
        
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://register.mock/api/"),
        };

        var logger = new Logger<RegisterClient>(new LoggerFactory());
        _client = new(httpClient, logger);
    }

    [Fact]
    public async Task RequestingNoParties_DoesNotSendRequest()
    {
        var result = await _client.GetPartyIdentifiers(Array.Empty<PartyUrn>()).ToListAsync();

        Assert.NotNull(result);
        result.Should().BeEmpty();
        _handler.NumRequests.Should().Be(0);
    }

    [Fact]
    public async Task RequestingLessThan50_SendsOneRequest()
    {
        var parties = Enumerable.Range(1, 10)
            .Select(i => PartyUrn.PartyId.Create(GetNextPartyId()))
            .ToArray();

        var result = await _client.GetPartyIdentifiers(parties).ToListAsync();

        Assert.NotNull(result);
        result.Should().HaveCount(parties.Length);
        _handler.NumRequests.Should().Be(1);
    }

    [Fact]
    public async Task RequestingMoreThan50_SendsMultipleRequests()
    {
        var parties = Enumerable.Range(1, 120)
            .Select(i => PartyUrn.PartyId.Create(GetNextPartyId()))
            .ToArray();

        var result = await _client.GetPartyIdentifiers(parties).ToListAsync();

        Assert.NotNull(result);
        result.Should().HaveCount(parties.Length);
        _handler.NumRequests.Should().Be(3);
    }

    [Fact]
    public async Task QueryParametersAreSplitCorrectly()
    {
        var parties = new List<PartyUrn>(60 * 3);

        for (var i = 0; i < 60; i++)
        {
            parties.Add(PartyUrn.PartyId.Create(GetNextPartyId()));
        }

        for (var i = 0; i < 60; i++)
        {
            parties.Add(PartyUrn.PartyUuid.Create(GetNextPartyUuid()));
        }

        for (var i = 0; i < 60; i++)
        {
            parties.Add(PartyUrn.OrganizationIdentifier.Create(GetNextOrganizationNumber()));
        }

        var result = await _client.GetPartyIdentifiers(parties).ToListAsync();

        Assert.NotNull(result);
        result.Should().HaveCount(parties.Count);

        var requests = _handler.Requests;
        requests.Should().HaveCount(4);

        requests.Should().ContainSingle(r => r.PartyIds == 50);
        requests.Should().ContainSingle(r => r.PartyIds == 10 && r.PartyUuids == 40);
        requests.Should().ContainSingle(r => r.PartyUuids == 20 && r.OrgNos == 30);
        requests.Should().ContainSingle(r => r.PartyUuids == 0 && r.OrgNos == 30);
    }

    private int GetNextPartyId() 
        => Interlocked.Increment(ref _nextPartyId);

    private OrganizationNumber GetNextOrganizationNumber() 
        => OrganizationNumber.Parse(GetNextPartyId().ToString("D9", CultureInfo.InvariantCulture));

    private Guid GetNextPartyUuid()
        => Guid.Parse($"00000000-0000-0000-0000-000{GetNextPartyId():D9}");

    class MockRegisterPartyLookupHandler
        : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private ImmutableList<RequestStats> _requests
            = ImmutableList<RequestStats>.Empty;
        //private int _numRequests = 0;
        private ImmutableList<PartyUrn> _requestedParties
            = ImmutableList<PartyUrn>.Empty;

        public int NumRequests => Requests.Count;

        public ImmutableList<RequestStats> Requests => Volatile.Read(ref _requests);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotNull(request.RequestUri);
            request.RequestUri.LocalPath.Should().EndWith("parties/identifiers");

            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            query.Count.Should().NotBe(0);

            var parties = new List<PartyIdentifiers>();
            var requested = new List<PartyUrn>();

            var idCount = 0;
            var uuidCount = 0;
            var orgNoCount = 0;

            foreach (var idList in query.GetValues("ids") ?? [])
            {
                foreach (var idString in idList.Split(','))
                {
                    var id = int.Parse(idString, NumberStyles.None);
                    parties.Add(FromId(id));
                    requested.Add(PartyUrn.PartyId.Create(id));
                    idCount++;
                }
            }

            foreach (var uuidList in query.GetValues("uuids") ?? [])
            {
                foreach (var uuidString in uuidList.Split(','))
                {
                    var uuid = Guid.Parse(uuidString);
                    parties.Add(FromUuid(uuid));
                    requested.Add(PartyUrn.PartyUuid.Create(uuid));
                    uuidCount++;
                }
            }

            foreach (var orgNoList in query.GetValues("orgs") ?? [])
            {
                foreach (var orgNoString in orgNoList.Split(','))
                {
                    var orgNo = orgNoString;
                    parties.Add(FromOrgNo(orgNo));
                    requested.Add(PartyUrn.OrganizationIdentifier.Create(OrganizationNumber.Parse(orgNo)));
                    orgNoCount++;
                }
            }

            var requestStats = new RequestStats(idCount, uuidCount, orgNoCount);
            ImmutableInterlocked.Update(ref _requests, r => r.Add(requestStats));
            ImmutableInterlocked.Update(ref _requestedParties, p => p.AddRange(requested));

            var responseContent = JsonContent.Create(parties, options: JsonOptions);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = responseContent
            };

            return Task.FromResult(response);
        }

        static PartyIdentifiers FromId(int id)
        {
            id.Should().BeGreaterThan(0);
            var orgNo = id.ToString("D9", CultureInfo.InvariantCulture);
            var guid = Guid.Parse($"00000000-0000-0000-0000-000{orgNo}");

            return new PartyIdentifiers
            {
                PartyId = id,
                PartyUuid = guid,
                OrgNumber = orgNo,
            };
        }

        static PartyIdentifiers FromUuid(Guid uuid)
        {
            var uuidString = uuid.ToString("D");
            uuidString.Should().StartWith("00000000-0000-0000-0000-000");
            var orgNo = uuidString[^9..];
            var id = int.Parse(orgNo, NumberStyles.None);

            return new PartyIdentifiers
            {
                PartyId = id,
                PartyUuid = uuid,
                OrgNumber = orgNo,
            };
        }

        static PartyIdentifiers FromOrgNo(string orgNo)
        {
            orgNo.Should().MatchRegex(@"^\d{9}$");
            var id = int.Parse(orgNo, NumberStyles.None);
            var guid = Guid.Parse($"00000000-0000-0000-0000-000{orgNo}");

            return new PartyIdentifiers
            {
                PartyId = id,
                PartyUuid = guid,
                OrgNumber = orgNo,
            };
        }
    }

    private record RequestStats(int PartyIds, int PartyUuids, int OrgNos);
}
