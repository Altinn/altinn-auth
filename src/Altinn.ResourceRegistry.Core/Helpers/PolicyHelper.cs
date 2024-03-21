using System.Buffers;
using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;
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
