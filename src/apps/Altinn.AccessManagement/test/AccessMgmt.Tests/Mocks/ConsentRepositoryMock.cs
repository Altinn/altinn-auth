using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;

namespace AccessMgmt.Tests.Mocks
{
    public class ConsentRepositoryMock : IConsentRepository
    {
        public Task AcceptConsentRequest(Guid id, Guid performedByParty,  ConsentContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ConsentRequestDetails> CreateRequest(ConsentRequest consentRequest,  ConsentPartyUrn performedByParty, CancellationToken cancellationToken = default)
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

        public Task RejectConsentRequest(Guid id, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Revoke(Guid id, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
