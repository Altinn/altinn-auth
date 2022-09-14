using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core
{
    public interface IPRP
    {
        Task<bool> WriteResourcePolicyAsync(string resourceId, Stream policystream);
    }
}
