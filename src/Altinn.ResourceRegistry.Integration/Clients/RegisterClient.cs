using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Register;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Integration.Clients;

/// <summary>
/// Implementation for <see cref="IRegisterClient"/>.
/// </summary>
internal partial class RegisterClient
    : IRegisterClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;
    private readonly ILogger<RegisterClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterClient"/> class.
    /// </summary>
    public RegisterClient(HttpClient client, ILogger<RegisterClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<PartyUrn> parties, CancellationToken cancellationToken = default)
    {
        List<int>? partyIds = null;
        List<Guid>? partyUuids = null;
        List<string>? orgNos = null;

        foreach (var party in parties)
        {
            switch (party)
            {
                case PartyUrn.PartyId partyId:
                    partyIds ??= new List<int>();
                    partyIds.Add(partyId.Value);
                    break;

                case PartyUrn.PartyUuid partyUuid:
                    partyUuids ??= new List<Guid>();
                    partyUuids.Add(partyUuid.Value);
                    break;

                case PartyUrn.OrganizationIdentifier orgNo:
                    orgNos ??= new List<string>();
                    orgNos.Add(orgNo.Value.ToString());
                    break;
            }
        }

        return GetPartyIdentifiers(partyIds, partyUuids, orgNos, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<Guid> partyUuids, CancellationToken cancellationToken = default)
    {
        return GetPartyIdentifiers(partyIds: null, partyUuids: partyUuids, orgNos: null, cancellationToken);
    }

    private IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(
        IEnumerable<int>? partyIds,
        IEnumerable<Guid>? partyUuids,
        IEnumerable<string>? orgNos,
        CancellationToken cancellationToken = default)
    {
        if (partyIds is null && partyUuids is null && orgNos is null)
        {
            return AsyncEnumerable.Empty<PartyIdentifiers>();
        }

        var queryEnumerator = EnumerateRequests(partyIds, partyUuids, orgNos);
        if (!queryEnumerator.MoveNext())
        {
            // No data to query for.
            return AsyncEnumerable.Empty<PartyIdentifiers>();
        }

        var result = MakeSingleRequest(queryEnumerator.Current, cancellationToken);
        
        while (queryEnumerator.MoveNext())
        {
            result = result.Merge(MakeSingleRequest(queryEnumerator.Current, cancellationToken));
        }

        return result;

        IAsyncEnumerable<PartyIdentifiers> MakeSingleRequest(QueryString query, CancellationToken token)
        {
            Log.GettingPartyIdentifiers(_logger, query);

            return _client.GetFromJsonAsAsyncEnumerable<PartyIdentifiers>($"parties/identifiers{query}", JsonOptions, token)
                .Where(party => party is not null)!;
        }

        static IEnumerator<QueryString> EnumerateRequests(
            IEnumerable<int>? partyIds,
            IEnumerable<Guid>? partyUuids,
            IEnumerable<string>? orgNos)
        {
            const int MAX_PER_REQUEST = 50;

            var query = QueryString.Empty;
            var count = 0;
            var builder = new StringBuilder((36 /* length of a guid */ + 1 /* comma */) * MAX_PER_REQUEST);

            if (partyIds is not null)
            {
                builder.Clear();
                foreach (var partyId in partyIds)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(partyId);
                    count++;

                    if (count >= MAX_PER_REQUEST)
                    {
                        query = query.Add("ids", builder.ToString());
                        yield return query;
                        query = QueryString.Empty;
                        count = 0;
                        builder.Clear();
                    }
                }

                if (builder.Length > 0)
                {
                    query = query.Add("ids", builder.ToString());
                }
            }

            if (partyUuids is not null)
            {
                builder.Clear();
                foreach (var partyUuid in partyUuids)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(partyUuid);
                    count++;

                    if (count >= MAX_PER_REQUEST)
                    {
                        query = query.Add("uuids", builder.ToString());
                        yield return query;
                        query = QueryString.Empty;
                        count = 0;
                        builder.Clear();
                    }
                }

                if (builder.Length > 0)
                {
                    query = query.Add("uuids", builder.ToString());
                }
            }

            if (orgNos is not null)
            {
                builder.Clear();
                foreach (var orgNo in orgNos)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(orgNo);
                    count++;

                    if (count >= MAX_PER_REQUEST)
                    {
                        query = query.Add("orgs", builder.ToString());
                        yield return query;
                        query = QueryString.Empty;
                        count = 0;
                        builder.Clear();
                    }
                }

                if (builder.Length > 0)
                {
                    query = query.Add("orgs", builder.ToString());
                }
            }

            if (query.HasValue)
            {
                yield return query;
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Getting party identifiers for {PartyReferences}.", EventName = "GetPartyIdentifiers")]
        public static partial void GettingPartyIdentifiers(ILogger logger, QueryString partyReferences);
    }
}
