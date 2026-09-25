using Altinn.ResourceRegistry.Core.Models;
using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Models
{
    /// <summary>
    /// Definees a flatten Policy Rule
    /// </summary>
    public class PolicyRuleDTO
    {
        /// <summary>
        /// The Subject target in rule
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> Subject { get; init; }

        /// <summary>
        /// Defines the action that the subject is allowed to perform on the resource
        /// </summary>
        public required UrnJsonTypeValue Action { get; init; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> Resource { get; init; }

        /// <summary>
        /// Map to DTO List
        /// </summary>
        public static IEnumerable<PolicyRuleDTO> MapFrom(IEnumerable<PolicyRule> policyRules)
        {
            return policyRules?.Select(static r => MapFrom(r));
        }

        /// <summary>
        /// Ma to DTO
        /// </summary>
        public static PolicyRuleDTO MapFrom(PolicyRule policyRule)
        {
            if (policyRule == null)
            {
                return null;
            }

            PolicyRuleDTO policyRuleDTO = new PolicyRuleDTO
            {
                Action = policyRule.Action,
                Resource = policyRule.Resource,
                Subject = policyRule.Subject
            };

            return policyRuleDTO;
        }
    }
}
