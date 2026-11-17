using System.Buffers;
using System.Data;
using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Xacml;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.Urn;
using Altinn.Urn.Json;
using Nerdbank.Streams;
using static Altinn.Authorization.Xacml.Constants.AltinnXacmlConstants;

namespace Altinn.ResourceRegistry.Core.Helpers
{
    /// <summary>
    /// Policy helper methods
    /// </summary>
    public static class PolicyHelper
    {
        /// <summary>
        /// Validate XACML Policy for resource information
        /// </summary>
        /// <param name="serviceResources">The resource from the registry</param>
        /// <param name="policy">The xacml policy</param>
        public static void EnsureValidPolicy(ServiceResource serviceResources, XacmlPolicy policy)
        {
            bool isAppResource = serviceResources.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX);
            string expectedOrg = null;
            string expectedApp = null;

            if (isAppResource)
            {
                string[] parts = serviceResources.Identifier.Split('_');
                if (parts.Length == 3)
                {
                    expectedOrg = parts[1];
                    expectedApp = parts[2];
                }
            }

            foreach (XacmlRule policyRule in policy.Rules)
            {
                List<AttributeMatch> xacmlResources = GetResourceFromXacmlRule(policyRule);

                if (xacmlResources.Any(r => r.Id.Equals(MatchAttributeIdentifiers.ResourceRegistryAttribute) && !r.Value.Equals(serviceResources.Identifier)))
                {
                    throw new ArgumentException("Policy not accepted: Contains rules for a different registry resource");
                }

                bool hasValidResourceReference = xacmlResources.Any(r => r.Id.Equals(MatchAttributeIdentifiers.ResourceRegistryAttribute) && r.Value.Equals(serviceResources.Identifier));

                if (isAppResource && !hasValidResourceReference && expectedOrg != null && expectedApp != null)
                {
                    bool hasOrgAttribute = xacmlResources.Any(r => r.Id.Equals(MatchAttributeIdentifiers.OrgAttribute) && r.Value.Equals(expectedOrg, StringComparison.OrdinalIgnoreCase));
                    bool hasAppAttribute = xacmlResources.Any(r => r.Id.Equals(MatchAttributeIdentifiers.AppAttribute) && r.Value.Equals(expectedApp, StringComparison.OrdinalIgnoreCase));

                    if (hasOrgAttribute && hasAppAttribute)
                    {
                        hasValidResourceReference = true;
                    }
                }

                if (!hasValidResourceReference)
                {
                    throw new ArgumentException("Policy not accepted: Contains rule without reference to registry resource id");
                }
            }
        }

        /// <summary>
        /// Takes the file IO stream and parses the policy file to a XacmlPolicy <see cref="XacmlPolicy"/>
        /// </summary>
        /// <param name="buffer">The buffer containing the xacml policy in xml format</param>
        /// <returns>XacmlPolicy</returns>
        public static XacmlPolicy ParsePolicy(ReadOnlySequence<byte> buffer)
        {
            using var stream = buffer.AsStream();
            using XmlReader reader = XmlReader.Create(stream);

            return XacmlParser.ParseXacmlPolicy(reader);
        }

        /// <summary>
        /// Converts a XACML policy to a list of PolicyRule
        /// </summary>
        public static List<PolicyRule> ConvertToPolicyRules(XacmlPolicy xacmlPolicy)
        {
            List<PolicyRule> rules = new List<PolicyRule>();
            foreach (XacmlRule xacmlRule in xacmlPolicy.Rules)
            {
                FlattenXacmlRule(xacmlRule, rules);
            }

            return rules;
        }

        /// <summary>
        /// Returns a list of rights for a resource. A right is a combination of resource and action. The response list the subjects in policy that is granted the right.
        /// Response is grouped by right.
        /// </summary>
        public static List<PolicyRight> ConvertToPolicyRight(XacmlPolicy policy)
        {
            List<PolicyRule> policyRules = ConvertToPolicyRules(policy);
            List<PolicyRight> policyRights = new(policyRules.Count);

            Dictionary<string, (PolicyRight Rights, List<PolicySubject> Subjects)> resourceActions = new();

            foreach (PolicyRule rule in policyRules)
            {
                List<PolicySubject> subjects = [new PolicySubject { SubjectAttributes = rule.Subject }];
                PolicyRight policyResourceAction = new PolicyRight()
                {
                    Action = rule.Action,
                    Resource = rule.Resource,
                    Subjects = subjects,
                };

                if (resourceActions.ContainsKey(policyResourceAction.RightKey))
                {
                    resourceActions[policyResourceAction.RightKey].Subjects.AddRange(policyResourceAction.Subjects);
                }
                else
                {
                    resourceActions.Add(policyResourceAction.RightKey, (policyResourceAction, subjects));
                    policyRights.Add(policyResourceAction);
                }
            }

            return policyRights;
        }

        /// <summary>
        /// Gets a nested list of AttributeMatche models for all XacmlMatch instances matching the specified attribute category. 
        /// </summary>
        /// <param name="rule">The xacml rule to process</param>
        /// <param name="category">The attribute category to match</param>
        /// <returns>Nested list of PolicyAttributeMatch models</returns>
        public static List<List<PolicyAttributeMatch>> GetRulePolicyAttributeMatchesForCategory(XacmlRule rule, string category)
        {
            return XacmlPolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, category, (type, value) => new PolicyAttributeMatch { Type = type, Value = value });
        }

        /// <summary>
        /// This method will flatten the XACML rule into a list of PolicyRule where each PolicyRule contains a list of KeyValueUrn for subject, action and resource
        /// The list will cotain duplicates if there is duplicate rules in XACML.
        /// 
        /// The code also enforce some extra rules:
        /// For each of the three categories (subject, action, resource) they need to be in a separate AnyOf element in the Target element.
        /// Action can only be one match in a AllOf element.(you cant do both read and write at the same time)
        /// Subject have multiple matches in a AllOf element, but it is not used in the current implementation in Altinn. (requiring a user to have multiple roles to access a resource)
        /// A resource can have multiple matched in a AllOf element to be able to match on multiple attributes. (app, task1 , task2 etc)
        /// </summary>
        private static void FlattenXacmlRule(XacmlRule xacmlRule, List<PolicyRule> policyRules)
        {
            XacmlAnyOf anyOfSubjects = null;
            XacmlAnyOf anyOfActions = null;
            XacmlAnyOf anyOfResourcs = null;

            foreach (XacmlAnyOf anyOf in xacmlRule.Target.AnyOf)
            {
                string category = null;

                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    foreach (XacmlMatch match in allOf.Matches)
                    {
                        if (category == null)
                        {
                            category = match.AttributeDesignator.Category.ToString();
                        }
                        else if (!category.Equals(match.AttributeDesignator.Category.ToString()))
                        {
                            throw new ArgumentException("All matches in a all must have the same category ruleId " + xacmlRule.RuleId);
                        }
                    }
                }

                if (category.Equals(XacmlConstants.MatchAttributeCategory.Action))
                {
                    anyOfActions = anyOf;
                }
                else if (category.Equals(XacmlConstants.MatchAttributeCategory.Subject))
                {
                    anyOfSubjects = anyOf;
                }
                else if (category.Equals(XacmlConstants.MatchAttributeCategory.Resource))
                {
                    anyOfResourcs = anyOf;
                }
            }

            foreach (XacmlAllOf allOfSubject in anyOfSubjects.AllOf)
            {
                foreach (XacmlAllOf allOfAction in anyOfActions.AllOf)
                {
                    foreach (XacmlAllOf allOfResource in anyOfResourcs.AllOf)
                    {
                        PolicyRule policyRule = new PolicyRule()
                        {
                            Subject = GetMatchValuesFromAllOff(allOfSubject),
                            Action = GetMatchValueFromAllOff(allOfAction),
                            Resource = GetMatchValuesFromAllOff(allOfResource)
                        };
                        policyRules.Add(policyRule);
                    }
                }
            }
        }

        /// <summary>
        /// Convert all matches in a allOf to a list of KeyValueUrn
        /// </summary>
        private static List<UrnJsonTypeValue> GetMatchValuesFromAllOff(XacmlAllOf allOfs)
        {
            List<UrnJsonTypeValue> subjectMatches = new List<UrnJsonTypeValue>();

            foreach (XacmlMatch match in allOfs.Matches)
            {
                string attributeId = match.AttributeDesignator.AttributeId.ToString();
                subjectMatches.Add(KeyValueUrn.Create($"{attributeId}:{match.AttributeValue.Value}", attributeId.Length + 1));
            }

            return subjectMatches;
        }

        /// <summary>
        /// Convert all matches in a allOf to a list of KeyValueUrn
        /// </summary>
        private static UrnJsonTypeValue GetMatchValueFromAllOff(XacmlAllOf allOfs)
        {
            if (allOfs.Matches.Count > 1)
            {
                throw new ArgumentException("Only one match is allowed in a allOf for action category");
            }

            if (allOfs.Matches.Count == 0)
            {
                throw new ArgumentException("No match found in allOf for action category");
            }

            XacmlMatch match = allOfs.Matches.First();
            string matchId = match.AttributeDesignator.AttributeId.ToString();
            return KeyValueUrn.Create($"{matchId}:{match.AttributeValue.Value}", matchId.Length + 1);
        }

        private static AttributeMatch GetActionValueFromRule(XacmlRule rule)
        {
            foreach (XacmlAnyOf anyOf in rule.Target.AnyOf)
            {
                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    XacmlMatch action = allOf.Matches.FirstOrDefault(m => m.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Action));

                    if (action != null)
                    {
                        return new AttributeMatch { Id = action.AttributeDesignator.AttributeId.OriginalString, Value = action.AttributeValue.Value };
                    }
                }
            }

            return null;
        }

        private static List<AttributeMatch> GetResourceFromXacmlRule(XacmlRule rule)
        {
            List<AttributeMatch> result = new List<AttributeMatch>();
            if (rule.Target == null)
            {
                return result;
            }

            foreach (XacmlAnyOf anyOf in rule.Target.AnyOf)
            {
                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    foreach (XacmlMatch xacmlMatch in allOf.Matches.Where(m => m.AttributeDesignator.Category.Equals(XacmlConstants.MatchAttributeCategory.Resource)))
                    {
                        result.Add(new AttributeMatch { Id = xacmlMatch.AttributeDesignator.AttributeId.OriginalString, Value = xacmlMatch.AttributeValue.Value });
                    }
                }
            }

            return result;
        }
    }
}
