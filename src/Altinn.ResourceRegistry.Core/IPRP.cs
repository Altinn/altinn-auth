using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core
{
    /// <summary>
    /// Interface for the policy retrieval point client
    /// </summary>
    public interface IPRP
    {
        /// <summary>
        /// Operation for writing a XACML policy to the policy blob storage
        /// </summary>
        /// <param name="filePath">The filepath to storing the policy</param>
        /// <param name="policyStream">The filestream for the XACML policy file</param>
        /// <returns>bool indicating the success or failure of the operation</returns>
        Task<bool> WriteResourcePolicyAsync(string filePath, Stream policyStream);
    }
}
