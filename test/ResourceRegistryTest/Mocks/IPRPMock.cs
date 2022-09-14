using Altinn.ResourceRegistry.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceRegistryTest.Mocks
{
    public class PRPMock : IPRP
    {
        public Task<bool> WriteResourcePolicyAsync(string resourceId, Stream policystream)
        {
            return Task.FromResult(true);
        }
    }
}
