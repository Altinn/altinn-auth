using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts
{
    public interface IPartyService
    {
        public Task<Result<AddPartyResult>> AddParty(PartyBaseInternal party, ChangeRequestOptions options, CancellationToken cancellationToken = default);
    }
}
