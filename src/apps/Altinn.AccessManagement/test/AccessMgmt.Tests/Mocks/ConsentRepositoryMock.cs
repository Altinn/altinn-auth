using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.Authorization.ProblemDetails;

namespace AccessMgmt.Tests.Mocks
{
    public class ConsentRepositoryMock : IConsentRepository
    {
        public Task AcceptConsentRequest(Guid id, Guid performedByParty, ConsentContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ConsentRequestDetails> CreateRequest(ConsentRequest consentRequest, ConsentPartyUrn performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRequest(Guid id, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Consent>> GetAllConsents(Guid partyUid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Consent> GetConsent(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ConsentContext> GetConsentContext(Guid consentRequestId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ConsentRequestDetails> GetRequest(Guid id, CancellationToken cancellationToken = default)
        {
            Stream dataStream = File.OpenRead($"Data/Consent/consent_request_{id.ToString()}.json");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            ConsentRequestDetails result = JsonSerializer.Deserialize<ConsentRequestDetails>(dataStream, options);
            return Task.FromResult(result);
        }

        public Task<Result<List<ConsentRequestDetails>>> GetRequestsForParty(Guid offeredByParty, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RejectConsentRequest(Guid id, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Revoke(Guid id, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetConsentRequestCountForParty(Guid fromPartyUuid, ConsentRequestStatusType status, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<List<ConsentStatusChange>>> GetConsentStatusChangesForParty(Guid partyUuid, string continuationToken, int pageSize, CancellationToken cancellationToken)
        {
            // Return test data based on partyUuid
            // Empty party UUID returns empty list
            if (partyUuid == Guid.Empty)
            {
                return Task.FromResult<Result<List<ConsentStatusChange>>>(new List<ConsentStatusChange>());
            }

            // Create a predictable set of test data
            List<ConsentStatusChange> allChanges =
            [
                new ConsentStatusChange
                {
                    ConsentRequestId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    EventType = ConsentRequestEventType.Accepted,
                    ChangedDate = new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero),
                    ConsentEventId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                },
                new ConsentStatusChange
                {
                    ConsentRequestId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    EventType = ConsentRequestEventType.Rejected,
                    ChangedDate = new DateTimeOffset(2026, 4, 19, 10, 0, 0, TimeSpan.Zero),
                    ConsentEventId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
                },
                new ConsentStatusChange
                {
                    ConsentRequestId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    EventType = ConsentRequestEventType.Revoked,
                    ChangedDate = new DateTimeOffset(2026, 4, 18, 10, 0, 0, TimeSpan.Zero),
                    ConsentEventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
                },
            ];

            // Apply continuation token filtering (items older than the cursor)
            IEnumerable<ConsentStatusChange> filtered = allChanges;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                try
                {
                    byte[] data = Convert.FromBase64String(continuationToken);
                    long ticks = BitConverter.ToInt64(data, 0);
                    DateTimeOffset cursorTimestamp = new DateTimeOffset(ticks, TimeSpan.Zero);
                    filtered = allChanges.Where(c => c.ChangedDate < cursorTimestamp);
                }
                catch
                {
                    // Invalid token, return all
                }
            }

            // Apply page size limit
            List<ConsentStatusChange> result = filtered.Take(pageSize).ToList();

            return Task.FromResult<Result<List<ConsentStatusChange>>>(result);
        }
    }
}
