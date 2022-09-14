using Altinn.ResourceRegistry.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    public class PRPClient : IPRP
    {
        public Task<bool> WriteResourcePolicyAsync(string resourceId, Stream policystream)
        {
            throw new NotImplementedException();
        }
    }
}
