using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using static Altinn.ResourceRegistry.Core.Constants.AltinnXacmlConstants;

namespace Altinn.ResourceRegistry.Core.Helpers
{
    /// <summary>
    /// Policy helper methods
    /// </summary>
    public static class PolicyHelper
    {
        /// <summary>
        /// Validates the content of Xacml policy file stream, for any rules not for the given resource
        /// </summary>
        /// <param name="serviceResources">The resource from the registry</param>
        /// <param name="policyFileStream">The xacml policy file stream</param>
        public static void IsValidResourcePolicy(ServiceResource serviceResources, Stream policyFileStream)
        {
            XacmlPolicy policy = ParsePolicy(policyFileStream);
            policyFileStream.Position = 0;

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
        /// <param name="stream">The file IO stream</param>
        /// <returns>XacmlPolicy</returns>
        public static XacmlPolicy ParsePolicy(Stream stream)
        {
            stream.Position = 0;
            XacmlPolicy policy;
            using (XmlReader reader = XmlReader.Create(stream))
            {
                policy = XacmlParser.ParseXacmlPolicy(reader);
            }

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
