using System.Net.Http.Json;
using System.Text.Json;
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
    public IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<PartyReference> parties, CancellationToken cancellationToken = default)
    {
        List<int>? partyIds = null;
        List<Guid>? partyUuids = null;
        List<string>? orgNos = null;

        foreach (var party in parties)
        {
            switch (party)
            {
                case PartyReference.PartyId partyId:
                    partyIds ??= new List<int>();
                    partyIds.Add(partyId.Value);
                    break;

                case PartyReference.PartyUuid partyUuid:
                    partyUuids ??= new List<Guid>();
                    partyUuids.Add(partyUuid.Value);
                    break;

                case PartyReference.OrganizationIdentifier orgNo:
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

        var hasData = false;
        var query = QueryString.Empty;
        if (partyIds is not null)
        {
            var str = string.Join(',', partyIds);
            if (str.Length > 0)
            {
                hasData = true;
                query = query.Add("ids", str);
            }
        }

        if (partyUuids is not null)
        {
            var str = string.Join(',', partyUuids);
            if (str.Length > 0)
            {
                hasData = true;
                query = query.Add("uuids", str);
            }
        }

        if (orgNos is not null)
        {
            var str = string.Join(',', orgNos);
            if (str.Length > 0)
            {
                hasData = true;
                query = query.Add("orgs", str);
            }
        }

        if (!hasData)
        {
            return AsyncEnumerable.Empty<PartyIdentifiers>();
        }

        Log.GettingPartyIdentifiers(_logger, query);

        return _client.GetFromJsonAsAsyncEnumerable<PartyIdentifiers>($"parties/identifiers{query}", JsonOptions, cancellationToken)
            .Where(party => party is not null)!;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Getting party identifiers for {PartyReferences}.", EventName = "GetPartyIdentifiers")]
        public static partial void GettingPartyIdentifiers(ILogger logger, QueryString partyReferences);
    }
}
