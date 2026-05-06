using System;
using System.IO;
using System.Threading.Tasks;

using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Storage.Interface.Models;

using Newtonsoft.Json;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class InstanceMetadataRepositoryMock : IInstanceMetadataRepository
    {
        /// <inheritdoc/>
        public Task<AuthInfo> GetAuthInfo(string instanceId)
        {
            // The Storage HTTP API path that ContextHandler now always takes only
            // consumes Process + AppId from AuthInfo, so derive both from the same
            // canned Instance JSON the mock has always served.
            Instance instance = GetTestInstance(instanceId);
            return Task.FromResult(new AuthInfo
            {
                Process = instance.Process,
                AppId = instance.AppId,
            });
        }

        private static string GetInstancePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Instances");
        }

        private static Instance GetTestInstance(string instanceId)
        {
            string partyPart = instanceId.Split('/')[0];
            string instancePart = instanceId.Split('/')[1];

            string content = File.ReadAllText(Path.Combine(GetInstancePath(), $"{partyPart}/{instancePart}.json"));
            return (Instance)JsonConvert.DeserializeObject(content, typeof(Instance));
        }
    }
}
