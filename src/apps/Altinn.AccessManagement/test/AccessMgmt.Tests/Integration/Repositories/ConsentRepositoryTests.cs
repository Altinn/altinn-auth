using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Altinn.AccessManagement.Tests.Integration.Repositories;

/// <summary>
/// Persistence-layer tests for <see cref="Altinn.AccessManagement.Persistence.Consent.ConsentRepository"/>.
/// Uses <see cref="LegacyApiFixture"/> because the consent repository depends on an
/// <see cref="NpgsqlDataSource"/> with the consent enum types mapped (status_type, event_type,
/// portal_view_mode) — the fixture configures those via the production data-source setup.
/// </summary>
[IntegrationTest]
public class ConsentRepositoryTests : IAsyncLifetime
{
    private LegacyApiFixture _fixture = null!;

    public async ValueTask InitializeAsync()
    {
        _fixture = new LegacyApiFixture();
        await _fixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    /// <summary>
    /// consentevent.topartyuuid and consentevent.handledbypartyuuid are denormalized from the parent
    /// consentrequest in ConsentRepository.EventQuery at insert time. GetConsentEventsForParty filters
    /// on <c>topartyuuid = @party OR handledbypartyuuid = @party</c>, so the join-free end state needs
    /// both columns on the event. Every event written for a request — the initial 'created' event and
    /// the later 'accepted' event both go through that same insert — must carry the request's recipient
    /// and handler parties, never null when the request has them.
    /// </summary>
    [Fact]
    public async Task EventInserts_PopulateConsentEventPartyUuidsFromParentRequest()
    {
        IConsentRepository repository = _fixture.Services.GetRequiredService<IConsentRepository>();
        NpgsqlDataSource dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        Guid requestId = Guid.CreateVersion7();
        Guid fromPartyUuid = Guid.NewGuid();
        Guid toPartyUuid = Guid.NewGuid();
        Guid handledByPartyUuid = Guid.NewGuid();

        ConsentRequest request = BuildConsentRequest(requestId, fromPartyUuid, toPartyUuid, handledByPartyUuid);

        // Inserts the consentrequest row + the initial 'created' event.
        await repository.CreateRequest(
            request,
            ConsentPartyUrn.PartyUuid.Create(fromPartyUuid),
            TestContext.Current.CancellationToken);

        // Inserts an 'accepted' event through the same EventQuery insert path.
        await repository.AcceptConsentRequest(
            requestId,
            toPartyUuid,
            new ConsentContext { Language = "nb" },
            TestContext.Current.CancellationToken);

        (long total, long toNulls, long wrongRecipient, long handledByNulls, long wrongHandledBy) =
            await QueryEventPartyUuidStats(dataSource, requestId, toPartyUuid, handledByPartyUuid);

        Assert.True(total >= 2, "expected at least the 'created' and 'accepted' events");
        Assert.Equal(0L, toNulls);
        Assert.Equal(0L, wrongRecipient);
        Assert.Equal(0L, handledByNulls);
        Assert.Equal(0L, wrongHandledBy);
    }

    private static ConsentRequest BuildConsentRequest(Guid id, Guid fromPartyUuid, Guid toPartyUuid, Guid handledByPartyUuid) => new()
    {
        Id = id,
        From = ConsentPartyUrn.PartyUuid.Create(fromPartyUuid),
        To = ConsentPartyUrn.PartyUuid.Create(toPartyUuid),
        HandledBy = ConsentPartyUrn.PartyUuid.Create(handledByPartyUuid),
        ValidTo = DateTimeOffset.UtcNow.AddDays(1),
        RedirectUrl = "https://example.test",
        TemplateId = "test-template",
        RequestMessage = new Dictionary<string, string> { ["en"] = "Please approve this consent request" },
        ConsentRequestStatus = ConsentRequestStatusType.Created,
        ConsentRights =
        [
            new ConsentRight
            {
                Action = ["read"],
                Resource =
                [
                    new ConsentResourceAttribute { Type = "urn:altinn:resource", Value = "ttd_test" },
                ],
            },
        ],
    };

    /// <summary>
    /// Returns, for a single consent request: total events, events with null topartyuuid, events whose
    /// topartyuuid is not the expected recipient, events with null handledbypartyuuid, and events whose
    /// handledbypartyuuid is not the expected handler.
    /// </summary>
    private static async Task<(long Total, long ToNulls, long WrongRecipient, long HandledByNulls, long WrongHandledBy)> QueryEventPartyUuidStats(
        NpgsqlDataSource dataSource, Guid requestId, Guid expectedToPartyUuid, Guid expectedHandledByPartyUuid)
    {
        await using NpgsqlCommand cmd = dataSource.CreateCommand(@"
            SELECT
                count(*)                                                              AS total,
                count(*) FILTER (WHERE topartyuuid IS NULL)                           AS to_nulls,
                count(*) FILTER (WHERE topartyuuid IS DISTINCT FROM @expectedTo)      AS wrong_to,
                count(*) FILTER (WHERE handledbypartyuuid IS NULL)                    AS handledby_nulls,
                count(*) FILTER (WHERE handledbypartyuuid IS DISTINCT FROM @expectedHandledBy) AS wrong_handledby
            FROM consent.consentevent
            WHERE consentrequestid = @requestId");
        cmd.Parameters.AddWithValue("@requestId", NpgsqlDbType.Uuid, requestId);
        cmd.Parameters.AddWithValue("@expectedTo", NpgsqlDbType.Uuid, expectedToPartyUuid);
        cmd.Parameters.AddWithValue("@expectedHandledBy", NpgsqlDbType.Uuid, expectedHandledByPartyUuid);

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        await reader.ReadAsync(TestContext.Current.CancellationToken);
        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
    }
}
