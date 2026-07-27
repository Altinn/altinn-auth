using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.Core.Services.Interfaces
{
    /// <summary>
    /// Service for operations regarding single rights delegations
    /// </summary>
    public interface ISingleRightsService
    {
        /// <summary>
        /// Takes entities and a list of actionKeys and tries to write delegation policy
        /// </summary>
        /// <param name="from">From (OfferedBy)</param>
        /// <param name="to">To (CoveredBy)</param>
        /// <param name="resource">Resource to delegate</param>
        /// <param name="ruleKeys">Rules on resource to delegate</param>
        /// <param name="performedBy">Performed by</param>
        /// <param name="ignoreExistingPolicy">Ignore existing policy when writing new policy</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The stored rules</returns>
        Task<List<Rule>> TryWriteDelegationPolicyRules(Entity from, Entity to, Resource resource, List<string> ruleKeys, Entity performedBy, bool ignoreExistingPolicy = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Takes entities, an instance id and a list of actionKeys and tries to write instance-specific delegation policy
        /// </summary>
        /// <param name="from">From (OfferedBy)</param>
        /// <param name="to">To (CoveredBy)</param>
        /// <param name="resource">Resource to delegate</param>
        /// <param name="instanceId">Instance identifier</param>
        /// <param name="ruleKeys">Rules on resource to delegate</param>
        /// <param name="performedBy">Performed by</param>
        /// <param name="ignoreExistingPolicy">Ignore existing policy when writing new policy</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The stored instance rules</returns>
        Task<List<InstanceRule>> TryWriteInstanceDelegationPolicyRules(Entity from, Entity to, Resource resource, string instanceId, List<string> ruleKeys, Entity performedBy, bool ignoreExistingPolicy = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all rules from policy and returns new versionId
        /// </summary>
        /// <param name="policyPath">Path to policy blob</param>
        /// <param name="policyVersion">Blob version</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>VersionId</returns>
        Task<string> ClearPolicyRules(string policyPath, string policyVersion, CancellationToken cancellationToken = default);
    }
}
