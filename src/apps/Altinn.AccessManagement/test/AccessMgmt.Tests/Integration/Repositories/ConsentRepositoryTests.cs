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
    /// consentevent.topartyuuid is denormalized from the parent consentrequest in
    /// ConsentRepository.EventQuery at insert time. Every event written for a request — the initial
    /// 'created' event and the later 'accepted' event both go through that same insert — must carry
    /// the request's recipient party, never null.
    /// </summary>
    [Fact]
    public async Task EventInserts_PopulateConsentEventToPartyUuidFromParentRequest()
    {
        IConsentRepository repository = _fixture.Services.GetRequiredService<IConsentRepository>();
        NpgsqlDataSource dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        Guid requestId = Guid.CreateVersion7();
        Guid fromPartyUuid = Guid.NewGuid();
        Guid toPartyUuid = Guid.NewGuid();

        ConsentRequest request = BuildConsentRequest(requestId, fromPartyUuid, toPartyUuid);

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

        (long total, long nulls, long wrongRecipient) =
            await QueryEventToPartyUuidStats(dataSource, requestId, toPartyUuid);

        Assert.True(total >= 2, "expected at least the 'created' and 'accepted' events");
        Assert.Equal(0L, nulls);
        Assert.Equal(0L, wrongRecipient);
    }

    private static ConsentRequest BuildConsentRequest(Guid id, Guid fromPartyUuid, Guid toPartyUuid) => new()
    {
        Id = id,
        From = ConsentPartyUrn.PartyUuid.Create(fromPartyUuid),
        To = ConsentPartyUrn.PartyUuid.Create(toPartyUuid),
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
    /// Returns (total events, events with null topartyuuid, events whose topartyuuid is not the
    /// expected recipient) for a single consent request.
    /// </summary>
    private static async Task<(long Total, long Nulls, long WrongRecipient)> QueryEventToPartyUuidStats(
        NpgsqlDataSource dataSource, Guid requestId, Guid expectedToPartyUuid)
    {
        await using NpgsqlCommand cmd = dataSource.CreateCommand(@"
            SELECT
                count(*)                                                       AS total,
                count(*) FILTER (WHERE topartyuuid IS NULL)                    AS nulls,
                count(*) FILTER (WHERE topartyuuid IS DISTINCT FROM @expected) AS wrong
            FROM consent.consentevent
            WHERE consentrequestid = @requestId");
        cmd.Parameters.AddWithValue("@requestId", NpgsqlDbType.Uuid, requestId);
        cmd.Parameters.AddWithValue("@expected", NpgsqlDbType.Uuid, expectedToPartyUuid);

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        await reader.ReadAsync(TestContext.Current.CancellationToken);
        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
    }
}
