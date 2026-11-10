using Altinn.ResourceRegistry.Core;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class PRPMock : IPRP
    {
        public Task<bool> WriteResourcePolicyAsync(string resourceId, Stream policystream)
        {
            return Task.FromResult(true);
        }
    }
}
