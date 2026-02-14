using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Integration.Clients;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Altinn.AccessManagement.Tests.Mocks
{
    /// <summary>
    /// Mock class for <see cref="IAltinn2ConsentClient"></see> interface
    /// </summary>
    public class Altinn2ConsentClientMock : IAltinn2ConsentClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Altinn2ConsentClientMock"/> class
        /// </summary>
        public Altinn2ConsentClientMock()
        {
        }

        /// <inheritdoc/>
        public Task<Altinn2ConsentRequest> GetAltinn2Consent(Guid consentGuid, CancellationToken cancellationToken = default)
        {
            Altinn2ConsentRequest request = GetAltinn2Request(consentGuid);

            return Task.FromResult(request);
        }

        /// <inheritdoc/>
        public Task<List<Guid>> GetAltinn2ConsentListForMigration(int numberOfConsentsToReturn, int? status, bool onlyGetExpired, CancellationToken cancellationToken = default)
        {
            List<Guid> list = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            return Task.FromResult(list);
        }

        /// <inheritdoc/>
        public Task<List<Altinn2ConsentRequest>> GetMultipleAltinn2Consents(List<string> consentList, CancellationToken cancellationToken = default)
        {
            List<Altinn2ConsentRequest> consents = new List<Altinn2ConsentRequest>();

            consents.Add(GetAltinn2Request(Guid.Parse(consentList[0])));
            consents.Add(GetAltinn2Request(Guid.Parse(consentList[1])));
            consents.Add(GetAltinn2Request(Guid.Parse(consentList[2])));

            return Task.FromResult(consents);
        }

        public Task<bool> UpdateAltinn2ConsentMigrateStatus(string consentGuid, int status, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        private Altinn2ConsentRequest GetAltinn2Request(Guid id)
        {
            Stream dataStream = File.OpenRead($"Data/Consent/a2consent_request_{id.ToString()}.json");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Altinn2ConsentRequest result = JsonSerializer.Deserialize<Altinn2ConsentRequest>(dataStream, options);

            return result;
        }
    }
}
