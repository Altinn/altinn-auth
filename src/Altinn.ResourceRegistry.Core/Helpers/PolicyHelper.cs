using System.Buffers;
using System.Data;
using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.Urn;
using Altinn.Urn.Json;
using Nerdbank.Streams;
using static Altinn.ResourceRegistry.Core.Constants.AltinnXacmlConstants;

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
            foreach (XacmlRule policyRule in policy.Rules)
            {
                List<AttributeMatch> xacmlResources = GetResourceFromXacmlRule(policyRule);

                if (xacmlResources.Any(r => r.Id.Equals(MatchAttributeIdentifiers.ResourceRegistryAttribute) && !r.Value.Equals(serviceResources.Identifier)))
                {
                    throw new ArgumentException("Policy not accepted: Contains rules for a different registry resource");
                }

                if (!xacmlResources.Any(r => r.Id.Equals(MatchAttributeIdentifiers.ResourceRegistryAttribute) && r.Value.Equals(serviceResources.Identifier)))
                {
                    throw new ArgumentException("Policy not accepted: Contains rule without reference to registry resource id");
                }
            }
        }

        /// <summary>
        /// Extracts a list of all roles codes mentioned in a permit rule in a policy. 
        /// </summary>
        /// <param name="policy">The policy</param>
        /// <returns>List of role codes</returns>
        public static List<string> GetRolesWithAccess(XacmlPolicy policy)
        {
            HashSet<string> roleCodes = new HashSet<string>();

            foreach (XacmlRule rule in policy.Rules)
            {
                if (rule.Effect.Equals(XacmlEffectType.Permit) && rule.Target != null)
                {
                    foreach (XacmlAnyOf anyOf in rule.Target.AnyOf)
                    {
                        foreach (XacmlAllOf allOf in anyOf.AllOf)
                        {
                            foreach (XacmlMatch xacmlMatch in allOf.Matches)
                            {
                                if (xacmlMatch.AttributeDesignator.AttributeId.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute))
                                {
                                    roleCodes.Add(xacmlMatch.AttributeValue.Value);
                                }
                            }
                        }
                    }
                }
            }

            return roleCodes.ToList();
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
        /// Takes the file IO stream and parses the policy file to a XacmlPolicy <see cref="XacmlPolicy"/>
        /// </summary>
        /// <param name="stream">The file IO stream</param>
        /// <returns>XacmlPolicy</returns>
        public static async Task<XacmlPolicy> ParsePolicy(Stream stream)
        {
            MemoryStream memoryStreamPolicy = new MemoryStream();
            await stream.CopyToAsync(memoryStreamPolicy);
            memoryStreamPolicy.Position = 0;
            return ParsePolicy(memoryStreamPolicy);
        }

        /// <summary>
        /// Takes the memorystream and parses the policy file to a XacmlPolicy <see cref="XacmlPolicy"/>
        /// </summary>
        /// <param name="stream">The file IO stream</param>
        /// <returns>XacmlPolicy</returns>
        public static XacmlPolicy ParsePolicy(MemoryStream stream)
        {
            using XmlReader reader = XmlReader.Create(stream);
            XacmlPolicy policy = XacmlParser.ParseXacmlPolicy(reader);
            return policy;
        }

        /// <summary>
        /// Gets the authentication level requirement from the obligation expression of the XacmlPolicy if specified 
        /// </summary>
        /// <param name="policy">The policy</param>
        /// <returns>Minimum authentication level requirement</returns>
        public static int GetMinimumAuthenticationLevelFromXacmlPolicy(XacmlPolicy policy)
        {
            foreach (XacmlObligationExpression oblExpr in policy.ObligationExpressions)
            {
                foreach (XacmlAttributeAssignmentExpression attrExpr in oblExpr.AttributeAssignmentExpressions)
                {
                    if (attrExpr.Category.OriginalString == AltinnXacmlConstants.MatchAttributeCategory.MinimumAuthenticationLevel &&
                        attrExpr.Property is XacmlAttributeValue attrValue &&
                        int.TryParse(attrValue.Value, out int minAuthLevel))
                    {
                        return minAuthLevel;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Builds the policy path based on org and app names
        /// </summary>
        /// <param name="org">The organization name/identifier</param>
        /// <param name="app">The altinn app name</param>
        /// <returns></returns>
        public static string GetAltinnAppsPolicyPath(string org, string app)
        {
            if (string.IsNullOrWhiteSpace(org))
            {
                throw new ArgumentException("Org was not defined");
            }

            if (string.IsNullOrWhiteSpace(app))
            {
                throw new ArgumentException("App was not defined");
            }

            return $"{org.AsFileName()}/{app.AsFileName()}/policy.xml";
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
            List<List<PolicyAttributeMatch>> ruleAttributeMatches = new();

            foreach (XacmlAnyOf anyOf in rule.Target.AnyOf)
            {
                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    List<PolicyAttributeMatch> anyOfAttributeMatches = new();
                    foreach (XacmlMatch xacmlMatch in allOf.Matches)
                    {
                        if (xacmlMatch.AttributeDesignator.Category.Equals(category))
                        {
                            anyOfAttributeMatches.Add(new PolicyAttributeMatch { Id = xacmlMatch.AttributeDesignator.AttributeId.OriginalString, Value = xacmlMatch.AttributeValue.Value });
                        }
                    }

                    if (anyOfAttributeMatches.Any())
                    {
                        ruleAttributeMatches.Add(anyOfAttributeMatches);
                    }
                }
            }

            return ruleAttributeMatches;
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
