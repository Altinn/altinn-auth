using System.Text;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using static Altinn.ResourceRegistry.Core.Constants.AltinnXacmlConstants;

namespace Altinn.AccessMgmt.Core.Utils.Helper
{
    /// <summary>
    /// Delegation check helper class responsible for processing XACML policies to extract resource/action keys and associated subjects (users, roles, access packages) for delegation checks. The class provides methods to decompose policies into actionable rights and to filter relevant subjects based on specific attribute prefixes. This is essential for determining which users or roles have access to certain resources and actions as defined in the XACML policies.
    /// </summary>
    public class DelegationCheckHelper
    {
        /// <summary>
        /// Gets a nested list of AttributeMatche models for all XacmlMatch instances matching the specified attribute category. 
        /// </summary>
        /// <param name="rule">The xacml rule to process</param>
        /// <param name="category">The attribute category to match</param>
        /// <returns>Nested list of PolicyAttributeMatch models</returns>
        public static IEnumerable<string> GetFirstAccessorValuesFromPolicy(XacmlRule rule, string category)
        {
            List<string> result = [];

            foreach (XacmlAnyOf anyOf in rule.Target.AnyOf)
            {
                foreach (XacmlAllOf allOf in anyOf.AllOf)
                {
                    List<string> anyOfAttributeMatches = new();
                    foreach (XacmlMatch xacmlMatch in allOf.Matches)
                    {
                        if (xacmlMatch.AttributeDesignator.Category.Equals(category))
                        {
                            anyOfAttributeMatches.Add(xacmlMatch.AttributeDesignator.AttributeId.OriginalString + ":" + xacmlMatch.AttributeValue.Value);
                        }
                    }

                    if (anyOfAttributeMatches.Count() == 1)
                    {
                        result.Add(anyOfAttributeMatches[0]);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Decompose a policyfile into a list of resource/action keys and a list of packages and roles giving acces to the actual key
        /// </summary>
        /// <param name="policy">the policy to process</param>
        /// <param name="resourceId">the resource id the subjects must point to</param>
        /// <param name="includeServiceOwnerRights">boolean indicating whether to include rights related to service owners</param>
        /// <param name="includeAppRights">boolean indicating whether to include rights related to applications (used for paralell signing rights)</param>
        /// <returns></returns>
        public static List<Right> DecomposePolicy(XacmlPolicy policy, string resourceId, bool includeServiceOwnerRights, bool includeAppRights)
        {
            Dictionary<string, Right> rights = new Dictionary<string, Right>();

            foreach (XacmlRule rule in policy.Rules)
            {
                IEnumerable<Right> rightsWithKeys = DelegationCheckHelper.CalculateActionKey(rule, resourceId);
                List<string> ruleSubjects = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(rule, XacmlConstants.MatchAttributeCategory.Subject).ToList();

                ruleSubjects = FilterSubjects(ruleSubjects, includeServiceOwnerRights, includeAppRights);

                if (ruleSubjects.Count == 0 || !rightsWithKeys.Any())
                {
                    continue;
                }

                foreach (Right rightWithKey in rightsWithKeys)
                {
                    if (!rights.TryGetValue(rightWithKey.Key, out Right value))
                    {
                        rightWithKey.AccessorUrns = [.. ruleSubjects];
                        rights.Add(rightWithKey.Key, rightWithKey);
                    }
                    else
                    {
                        value.AccessorUrns.UnionWith(ruleSubjects);
                    }
                }
            }

            return rights.Values.ToList();
        }

        /// <summary>
        /// Returns a list of resource/action keys based on a given policy rule
        /// </summary>
        /// <param name="rule">the rule to analyze</param>
        /// <param name="resourceId">the resourceid subjects must contain</param>
        /// <returns>list of resource/action keys</returns>
        public static IEnumerable<Right> CalculateActionKey(XacmlRule rule, string resourceId)
        {
            List<Right> result = [];

            // Use policy to calculate the rest of the key
            List<List<PolicyAttributeMatch>> resources = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Resource).ToList();
            List<List<PolicyAttributeMatch>> actions = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Action);
            List<Right> resourceKeys = new List<Right>();
            List<string> actionKeys = new List<string>();

            foreach (var resource in resources)
            {
                var org = resource.FirstOrDefault(r => r.Type.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute));
                var app = resource.FirstOrDefault(r => r.Type.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute));

                if (org != null && app != null)
                {
                    string resourceAppId = $"app_{org.Value}_{app.Value}";
                    resource.Remove(org);
                    resource.Remove(app);
                    resource.Add(new PolicyAttributeMatch { Type = AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, Value = resourceAppId });
                }

                // Just throw away resources not matching the resourceid we are looking for
                if (resource.Any(r => r.Type.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute) && r.Value.Equals(resourceId, StringComparison.OrdinalIgnoreCase)) == false)
                {
                    continue;
                }

                Right rightWithKey = new Right
                {
                    Resource = [..resource] // Collection expression with spread - creates a new list
                };

                StringBuilder resourceKey = new();

                resource.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in resource)
                {
                    resourceKey.Append(item.Type);
                    resourceKey.Append(':');
                    resourceKey.Append(item.Value);
                    resourceKey.Append(':');
                }

                if (resourceKey.Length > 0)
                {
                    resourceKey.Remove(resourceKey.Length - 1, 1);
                }

                rightWithKey.Key = resourceKey.ToString();
                resourceKeys.Add(rightWithKey);
            }

            foreach (var action in actions)
            {
                StringBuilder actionKey = new();

                action.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in action)
                {
                    actionKey.Append(item.Type);
                    actionKey.Append(':');
                    actionKey.Append(item.Value);
                    actionKey.Append(':');
                }

                if (actionKey.Length > 0)
                {
                    actionKey.Remove(actionKey.Length - 1, 1);
                }

                actionKeys.Add(actionKey.ToString());
            }

            foreach (Right resource in resourceKeys)
            {
                foreach (var action in actionKeys)
                {
                    result.Add(new Right { Key = resource.Key + ":" + action, Resource = resource.Resource, Action = new PolicyAttributeMatch() { Type = MatchAttributeIdentifiers.ActionId, Value = action.Replace(MatchAttributeIdentifiers.ActionId + ":", string.Empty) } });
                }
            }

            return result;
        }

        /// <summary>
        /// Splits a resource/action key into its resource and action components. The method processes the input string by separating it into parts based on the "urn:" delimiter. Each part is evaluated to determine whether it represents an action or a resource, based on specific prefixes. The resulting resource and action components are returned as a ResourceAndAction object, where the Resource property contains a list of resource identifiers and the Action property contains the first action identifier found in the input string.
        /// </summary>
        public static ResourceAndAction SplitRightKey(string actionKey)
        {
            List<string> resourceList = [];
            List<string> actionList = [];

            string[] urns = actionKey.Split("urn:", StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in urns)
            {
                string current = "urn:" + part;

                if (current.EndsWith(':'))
                {
                    current = current.Remove(current.Length - 1);
                }

                if (current.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.ActionId))
                {
                    actionList.Add(current);
                }
                else
                {
                    resourceList.Add(current.ToLowerInvariant());
                }
            }

            return new ResourceAndAction { Resource = resourceList, Action = actionList.FirstOrDefault() };
        }

        /// <summary>
        /// Filters the specified list of urns giving acces to only include the ones actual for end users.
        /// attribute prefixes.
        /// </summary>
        /// <remarks>urns are identified by specific attribute prefixes, such as role or access
        /// package attributes. This method excludes any urns that do not match these prefixes.</remarks>
        /// <param name="accessUrns">An enumerable collection of urns to be filtered. Each string is evaluated to determine if it
        /// matches a user rule attribute prefix.</param>
        /// <param name="includeServiceOwnerRights">boolean indicating whether to include rights related to service owners</param>
        /// <param name="includeAppRights">boolean indicating whether to include rights related to applications (used for paralell signing rights)</param>
        /// <returns>An enumerable collection containing only the rule subjects that correspond to user rules. The collection
        /// will be empty if no subjects match the recognized prefixes.</returns>
        private static List<string> FilterSubjects(IEnumerable<string> accessUrns, bool includeServiceOwnerRights, bool includeAppRights)
        {
            List<string> result = [];
            foreach (string urn in accessUrns)
            {
                if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute))
                {
                    result.Add(urn);
                }
                else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCcrRoleAttribute))
                {
                    result.Add(urn);
                }
                else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCraRoleAttribute))
                {
                    result.Add(urn);
                }
                else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute))
                {
                    result.Add(urn);
                }
                else if (includeServiceOwnerRights && urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute))
                {
                    result.Add(urn);
                }
                else if (includeAppRights && urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.Delegation))
                {
                    result.Add(urn);
                }
            }

            return result;
        }
    }
}
