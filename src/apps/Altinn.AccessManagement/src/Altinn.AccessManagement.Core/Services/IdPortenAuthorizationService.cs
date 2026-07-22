using System.Net;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Services
{
    public class IdPortenAuthorizationService : IIdPortenAuthorizationService
    {
        private readonly IAMPartyService _ampartyService;
        private readonly IIdPortenAuthorizationClient _idPortenAuthorizationClient;

        public IdPortenAuthorizationService(
            IAMPartyService ampartyService,
            IIdPortenAuthorizationClient idPortenAuthorizationClient)
        {
            _ampartyService = ampartyService;
            _idPortenAuthorizationClient = idPortenAuthorizationClient;
        }

        public async Task<Result<List<IdPortenAuthorization>>> GetIdPortenAuthorizations(Guid partyUuid, CancellationToken cancellationToken)
        {
            // look up ssn from partyUuid
            string ssn = await GetSsnFromPartyUuid(partyUuid, cancellationToken);

            if (string.IsNullOrEmpty(ssn))
            {
                return Problems.SsnNotFound;
            }

            // look up authorizations from IdPorten using ssn
            IdPortenClientResult<List<IdPortenAuthorization>> result = await _idPortenAuthorizationClient.GetIdPortenAuthorizations(ssn, cancellationToken);
            if (!result.IsSuccess)
            {
                return MapToProblem(result.StatusCode);
            }

            return result.Value;
        }

        public async Task<Result<bool>> DeleteIdPortenAuthorization(Guid partyUuid, string id, CancellationToken cancellationToken)
        {
            // look up ssn from partyUuid
            string ssn = await GetSsnFromPartyUuid(partyUuid, cancellationToken);
            
            if (string.IsNullOrEmpty(ssn))
            {
                return Problems.SsnNotFound;
            }

            // delete authorization from IdPorten using ssn and id
            IdPortenClientResult<bool> result = await _idPortenAuthorizationClient.DeleteIdPortenAuthorization(ssn, id, cancellationToken);
            if (!result.IsSuccess)
            {
                return MapToProblem(result.StatusCode);
            }

            return result.Value;
        }

        private async Task<string?> GetSsnFromPartyUuid(Guid partyUuid, CancellationToken cancellationToken)
        {
            // look up ssn from partyUuid
            MinimalParty party = await _ampartyService.GetByUid(partyUuid, cancellationToken);
            return party?.PersonId;
        }

        private static ProblemDescriptor MapToProblem(HttpStatusCode statusCode) => statusCode switch
        {
            HttpStatusCode.BadRequest => Problems.IdPortenAuthorizationBadRequest,
            HttpStatusCode.Unauthorized => Problems.IdPortenAuthorizationUnauthorized,
            HttpStatusCode.Forbidden => Problems.IdPortenAuthorizationForbidden,
            _ => Problems.IdPortenAuthorizationInternalServerError,
        };
    }
}
