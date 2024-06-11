using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core.Register;

namespace Altinn.ResourceRegistry.Tests.Mocks;

internal class MockRegisterClient
    : IRegisterClient
{
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

    public IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<Guid> partyUuids, CancellationToken cancellationToken = default)
    {
        return GetPartyIdentifiers(partyIds: null, partyUuids, orgNos: null, cancellationToken);
    }

    private static async IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(
        IEnumerable<int>? partyIds,
        IEnumerable<Guid>? partyUuids,
        IEnumerable<string>? orgNos,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (partyIds is { } ids)
        {
            foreach (var id in ids)
            {
                yield return MakePartyIdentifiers(id);
            }
        }

        if (partyUuids is { } uuids)
        {
            foreach (var uuid in uuids)
            {
                yield return MakePartyIdentifiers(uuid);
            }
        }

        if (orgNos is { } orgs)
        {
            foreach (var org in orgs)
            {
                yield return MakePartyIdentifiers(org);
            }
        }
    }

    private static PartyIdentifiers MakePartyIdentifiers(int id)
    {
        var orgNo = id.ToString("D9");
        var lastGuidPart = id.ToString("D12");
        var guidString = $"00000000-0000-0000-0000-{lastGuidPart}";
        var guid = Guid.Parse(guidString);

        return new PartyIdentifiers
        {
            PartyId = id,
            PartyUuid = guid,
            OrgNumber = orgNo,
        };
    }

    private static PartyIdentifiers MakePartyIdentifiers(Guid guid)
    {
        var lastGuidPart = guid.ToString().AsSpan()[^12..];
        var id = int.Parse(lastGuidPart);
        var orgNo = id.ToString("D9");

        return new PartyIdentifiers
        {
            PartyId = id,
            PartyUuid = guid,
            OrgNumber = orgNo,
        };
    }

    private static PartyIdentifiers MakePartyIdentifiers(string orgNo)
    {
        var id = int.Parse(orgNo);

        return MakePartyIdentifiers(id);
    }
}
