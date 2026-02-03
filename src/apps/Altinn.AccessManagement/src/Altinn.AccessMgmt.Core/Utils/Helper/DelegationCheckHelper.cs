using System.Text;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Enums.ResourceRegistry;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.AccessMgmt.Core.Utils.Helper
{
    public class DelegationCheckHelper
    {
        /// <summary>
        /// Check if it exist any roles giving access to the resource if there is no such access rules this must be a rule defined for the service owner as there is not any way the end user could gain access
        /// </summary>
        /// <param name="right">the right to analyze</param>
        /// <returns>the decision</returns>
        public static bool CheckIfRuleIsAnEndUserRule(Right right)
        {
            List<RightSource> rightAccessSources = right.RightSources.Where(rs => rs.RightSourceType != RightSourceType.DelegationPolicy).ToList();
            List<AttributeMatch> userAccess = [];
            if (rightAccessSources.Any())
            {
                List<AttributeMatch> roles = GetAttributeMatches(rightAccessSources.SelectMany(roleAccessSource => roleAccessSource.PolicySubjects)).FindAll(policySubject => policySubject.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, StringComparison.OrdinalIgnoreCase) 
                                                                                                                                                                           || policySubject.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCcrRoleAttribute, StringComparison.OrdinalIgnoreCase)
                                                                                                                                                                           || policySubject.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCraRoleAttribute, StringComparison.OrdinalIgnoreCase)
                                                                                                                                                                           || policySubject.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute, StringComparison.OrdinalIgnoreCase));
                return roles.Count != 0;
            }

            return false;
        }

        /// <summary>
        /// Checks if AccessList feature is enabled and applicable for the given right, resource, and fromParty. The AccessListMode feature is currently enabled only for orgs.
        /// </summary>
        /// <param name="right">The right to be delegated</param>
        /// <param name="resource">The resource we are making delegations for</param>
        /// <param name="fromParty">The party we are making delegations on behalf of</param>
        /// <returns>True if Access List authorization mode is enabled and applicable</returns>
        public static bool IsAccessListModeEnabledAndApplicable(Right right, ServiceResource resource, AccessManagement.Core.Models.Party.MinimalParty fromParty)
        {
            if (right.CanDelegate.HasValue && right.CanDelegate.Value
                && resource.AccessListMode == AccessManagement.Core.Enums.ResourceRegistry.ResourceAccessListMode.Enabled
                && fromParty.PartyType == EntityTypeId.Organization)
            {
                return true;
            }

            return false;
        }

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
        /// <returns></returns>
        public static List<ActionAccess> DecomposePolicy(XacmlPolicy policy)
        {
            Dictionary<string, List<string>> rules = new Dictionary<string, List<string>>();

            foreach (XacmlRule rule in policy.Rules)
            {
                IEnumerable<string> keys = DelegationCheckHelper.CalculateActionKey(rule);
                IEnumerable<string> ruleSubjects = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(rule, XacmlConstants.MatchAttributeCategory.Subject);
                ruleSubjects = RemoveNonUserRules(ruleSubjects);

                foreach (string key in keys)
                {
                    if (!rules.ContainsKey(key))
                    {
                        List<string> value = [.. ruleSubjects];
                        rules.Add(key, value);
                    }
                    else
                    {
                        rules[key].AddRange(ruleSubjects);
                    }
                }
            }

            List<ActionAccess> result = [];

            foreach (KeyValuePair<string, List<string>> action in rules)
            {
                ActionAccess current = new ActionAccess();
                current.ActionKey = action.Key;
                current.AccessorUrns = action.Value;
                current.PackageAllowAccess = [];
                current.PackageDenyAccess = [];
                current.RoleAllowAccess = [];
                current.RoleDenyAccess = [];
                current.ResourceAllowAccess = [];
                current.ResourceDenyAccess = [];

                result.Add(current);
            }

            return result;
        }

        /// <summary>
        /// Checks if AccessList feature is enabled and applicable for the given right, resource, and fromParty. The AccessListMode feature is currently enabled only for orgs.
        /// </summary>
        /// <returns>True if Access List authorization mode is enabled and applicable</returns>
        public static bool IsAccessListModeEnabledAndApplicable(ResourceAccessListMode accessListMode, Guid entityType)
        {
            if (accessListMode == ResourceAccessListMode.Enabled
                && entityType == EntityTypeConstants.Organization.Id)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a list of resource/action keys based on a given policy rule
        /// </summary>
        /// <param name="rule">the rule to analyze</param>
        /// <returns>list of resource/action keys</returns>
        public static IEnumerable<string> CalculateActionKey(XacmlRule rule)
        {
            List<string> result = [];

            //Use policy to calculate the rest of the key
            var resources = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Resource).ToList();
            var actions = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Action);
            List<string> resourceKeys = new List<string>();
            List<string> actionKeys = new List<string>();

            foreach (var resource in resources)
            {
                var org = resource.FirstOrDefault(r => r.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute));
                var app = resource.FirstOrDefault(r => r.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute));

                if (org != null && app != null)
                {
                    string resourceAppId = $"app_{org.Value}_{app.Value}";
                    resource.Remove(org);
                    resource.Remove(app);
                    resource.Add(new PolicyAttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, Value = resourceAppId });
                }

                StringBuilder resourceKey = new();

                resource.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in resource)
                {
                    resourceKey.Append(item.Id);
                    resourceKey.Append(":");
                    resourceKey.Append(item.Value);
                    resourceKey.Append(":");
                }

                if (resourceKey.Length > 0)
                {
                    resourceKey.Remove(resourceKey.Length - 1, 1);
                }

                resourceKeys.Add(resourceKey.ToString());
            }

            foreach (var action in actions)
            {
                StringBuilder actionKey = new();

                action.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in action)
                {
                    actionKey.Append(item.Id);
                    actionKey.Append(":");
                    actionKey.Append(item.Value);
                    actionKey.Append(":");
                }

                if (actionKey.Length > 0)
                {
                    actionKey.Remove(actionKey.Length - 1, 1);
                }

                actionKeys.Add(actionKey.ToString());
            }

            foreach (var resource in resourceKeys)
            {
                foreach (var action in actionKeys)
                {
                    result.Add(resource + ":" + action);
                }
            }

            return result;
        }

        /// <summary>
        /// Method to check if a resourceid is an app and decompose it into org/app values
        /// </summary>
        /// <param name="resourceId">the resourceid to check</param>
        /// <param name="org">the org part of the resourceid if it is an app</param>
        /// <param name="app">the app part of the resourceid if it is an app</param>
        /// <returns>true if app false if not</returns>
        public static bool IsAppResourceId(string resourceId, out string org, out string app)
        {
            org = null;
            app = null;
            bool isApp = false;

            if (resourceId.StartsWith("app_"))
            {
                isApp = true;
                string[] parts = resourceId.Split('_', 3);
                if (parts.Length == 3)
                {
                    org = parts[1];
                    app = parts[2];
                }
            }

            return isApp;
        }

        /// <summary>
        /// Filters the specified list of urns giving acces to only include the ones actual for end users.
        /// attribute prefixes.
        /// </summary>
        /// <remarks>urns are identified by specific attribute prefixes, such as role or access
        /// package attributes. This method excludes any urns that do not match these prefixes.</remarks>
        /// <param name="accessUrns">An enumerable collection of urns to be filtered. Each string is evaluated to determine if it
        /// matches a user rule attribute prefix.</param>
        /// <returns>An enumerable collection containing only the rule subjects that correspond to user rules. The collection
        /// will be empty if no subjects match the recognized prefixes.</returns>
        private static IEnumerable<string> RemoveNonUserRules(IEnumerable<string> accessUrns)
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
            }

            return result;
        }

        /// <summary>
        /// Converts a list of policy attribute matches into a list of attribute matches
        /// </summary>
        /// <param name="policySubjects">a list of policy attribute matches</param>
        /// <returns>a list of attribute matches</returns>
        private static List<AttributeMatch> GetAttributeMatches(IEnumerable<List<PolicyAttributeMatch>> policySubjects)
        {
            List<AttributeMatch> attributeMatches = new List<AttributeMatch>();
            foreach (List<PolicyAttributeMatch> attributeMatch in policySubjects)
            {
                attributeMatches.AddRange(attributeMatch);
            }

            return attributeMatches;
        }
    }
}
