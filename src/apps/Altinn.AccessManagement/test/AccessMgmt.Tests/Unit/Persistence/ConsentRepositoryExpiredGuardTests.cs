using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Persistence.Configuration;
using Altinn.AccessManagement.Persistence.Consent;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Altinn.AccessManagement.Tests.Unit.Persistence;

/// <summary>
/// Verifies that <see cref="ConsentRepository.CreateRequest"/> refuses to persist the derived <c>Expired</c>
/// status or event type. Neither the <c>consent.status_type</c> nor the <c>consent.event_type</c> PostgreSQL enum
/// defines an <c>expired</c> label, so an attempt to write it must fail with a clear exception before it reaches
/// the database rather than surfacing as an opaque Npgsql enum error.
/// </summary>
[UnitTest]
public class ConsentRepositoryExpiredGuardTests
{
    private static ConsentRepository CreateRepository()
    {
        // The guard runs before any connection is opened, so an unreachable data source is never contacted.
        NpgsqlDataSource dataSource = NpgsqlDataSource.Create("Host=localhost;Port=1;Database=none;Username=none;Password=none;Timeout=1;Command Timeout=1");
        IOptions<PostgreSQLSettings> settings = Options.Create(new PostgreSQLSettings());
        return new ConsentRepository(dataSource, settings, TimeProvider.System);
    }

    private static ConsentRequest CreateRequest(ConsentRequestStatusType status, params ConsentRequestEventType[] eventTypes)
    {
        ConsentPartyUrn party = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid());

        return new ConsentRequest
        {
            Id = Guid.NewGuid(),
            From = party,
            To = party,
            ValidTo = DateTimeOffset.UtcNow.AddDays(1),
            ConsentRights = [],
            RedirectUrl = string.Empty,
            ConsentRequestStatus = status,
            ConsentRequestEvents = [.. eventTypes.Select(type => new ConsentRequestEvent
            {
                EventType = type,
                Created = DateTimeOffset.UtcNow,
                PerformedBy = party,
            })],
        };
    }

    [Fact]
    public async Task CreateRequest_ExpiredStatus_ThrowsBeforePersisting()
    {
        ConsentRepository repository = CreateRepository();
        ConsentRequest request = CreateRequest(ConsentRequestStatusType.Expired);

        Func<Task> act = () => repository.CreateRequest(request, ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message.Contains("Expired") && ex.Message.Contains("cannot be persisted"));
    }

    [Fact]
    public async Task CreateRequest_ExpiredEventInList_ThrowsBeforePersisting()
    {
        ConsentRepository repository = CreateRepository();

        // A valid event precedes the derived one to prove the whole list is scanned.
        ConsentRequest request = CreateRequest(ConsentRequestStatusType.Created, ConsentRequestEventType.Created, ConsentRequestEventType.Expired);

        Func<Task> act = () => repository.CreateRequest(request, ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message.Contains("Expired") && ex.Message.Contains("cannot be persisted"));
    }

    [Theory]
    [InlineData(ConsentRequestStatusType.Created)]
    [InlineData(ConsentRequestStatusType.Accepted)]
    [InlineData(ConsentRequestStatusType.Rejected)]
    [InlineData(ConsentRequestStatusType.Revoked)]
    [InlineData(ConsentRequestStatusType.Deleted)]
    public async Task CreateRequest_PersistableStatus_DoesNotTripExpiredGuard(ConsentRequestStatusType status)
    {
        ConsentRepository repository = CreateRepository();
        ConsentRequest request = CreateRequest(status, ConsentRequestEventType.Created);

        // Persistable values pass the guard and reach the unreachable data source, which fails to connect.
        // The guard's InvalidOperationException must not be what surfaces.
        Exception thrown = await Record.ExceptionAsync(() => repository.CreateRequest(request, ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()), TestContext.Current.CancellationToken));

        thrown.Should().NotBeNull();
        (thrown is InvalidOperationException invalid && invalid.Message.Contains("cannot be persisted")).Should().BeFalse();
    }
}
