using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;

namespace Altinn.AccessManagement.Tests.Mocks
{
    public class IdPortenAuthorizationClientMock : IIdPortenAuthorizationClient
    {
        /// <summary>
        /// Maps an SSN to the HTTP status code the external IdPorten API should return for it, so
        /// tests can drive each error path (and thus each <c>MapToProblem</c> arm) via a chosen party.
        /// Any SSN not listed here is treated as a successful (200) response.
        /// </summary>
        private static readonly Dictionary<string, HttpStatusCode> SsnToStatusCode = new()
        {
            ["07124912037"] = HttpStatusCode.BadRequest,          // KASPER BØRSTAD
            ["02056260016"] = HttpStatusCode.Unauthorized,        // PAULA RIMSTAD
            ["27099450067"] = HttpStatusCode.Forbidden,           // ØRJAN RAVNÅS
            ["01124621077"] = HttpStatusCode.InternalServerError, // KARI GUNNERUD (hits the fallback arm)
        };

        public Task<IdPortenClientResult<List<IdPortenAuthorization>>> GetIdPortenAuthorizations(string ssn, CancellationToken cancellationToken)
        {
            if (SsnToStatusCode.TryGetValue(ssn, out HttpStatusCode statusCode))
            {
                return Task.FromResult(new IdPortenClientResult<List<IdPortenAuthorization>>(statusCode, null));
            }

            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AMPartyServiceMock).Assembly.Location).LocalPath);
            string partiesPath = Path.Combine(unitTestFolder, "Data", "IdPortenAuthorization", "IdPortenAuthorization_list.json");
            string content = File.ReadAllText(partiesPath);
            List<IdPortenAuthorization> results = JsonSerializer.Deserialize<List<IdPortenAuthorization>>(content);
            return Task.FromResult(new IdPortenClientResult<List<IdPortenAuthorization>>(HttpStatusCode.OK, results));
        }

        public Task<IdPortenClientResult<bool>> DeleteIdPortenAuthorization(string ssn, string id, CancellationToken cancellationToken)
        {
            if (SsnToStatusCode.TryGetValue(ssn, out HttpStatusCode statusCode))
            {
                return Task.FromResult(new IdPortenClientResult<bool>(statusCode, false));
            }

            return Task.FromResult(new IdPortenClientResult<bool>(HttpStatusCode.OK, true));
        }
    }
}
