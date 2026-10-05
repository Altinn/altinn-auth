using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Client implementation for the policy retireval point integration
    /// </summary>
    public class PRPClient : IPRP
    {
        /// <inheritdoc/>
        public Task<bool> WriteResourcePolicyAsync(string filePath, Stream policyStream)
        {
            throw new NotImplementedException();
        }
    }
}
