using System.Text;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums.ResourceRegistry;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.AccessMgmt.Core.Utils.Helper
{
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
        /// <returns></returns>
        public static List<Core.Models.Right> DecomposePolicy(XacmlPolicy policy, string resourceId)
        {
            Dictionary<string, List<string>> rights = new Dictionary<string, List<string>>();

            foreach (XacmlRule rule in policy.Rules)
            {
                IEnumerable<string> keys = DelegationCheckHelper.CalculateRightKeys(rule, resourceId);
                IEnumerable<string> ruleSubjects = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(rule, XacmlConstants.MatchAttributeCategory.Subject);
                ruleSubjects = RemoveNonUserRules(ruleSubjects);

                foreach (string key in keys)
                {
                    if (!rights.ContainsKey(key))
                    {
                        List<string> value = [.. ruleSubjects];
                        rights.Add(key, value);
                    }
                    else
                    {
                        rights[key].AddRange(ruleSubjects);
                    }
                }
            }

            List<Core.Models.Right> result = [];

            foreach (KeyValuePair<string, List<string>> right in rights)
            {
                if (right.Value.Count > 0)
                {
                    Core.Models.Right current = new Core.Models.Right();
                    current.Key = right.Key;
                    current.AccessorUrns = right.Value;
                    current.PackageAllowAccess = [];
                    current.PackageDenyAccess = [];
                    current.RoleAllowAccess = [];
                    current.RoleDenyAccess = [];
                    current.ResourceAllowAccess = [];

                    result.Add(current);
                }                
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

        public static XacmlTarget BuildDelegationRuleTarget(string toId, IEnumerable<string> resourceList, string action)
        {
            List<XacmlAnyOf> targetList = new List<XacmlAnyOf>();

            // Build Subject
            List<XacmlAllOf> subjectAllOfs = new List<XacmlAllOf>();

            subjectAllOfs.Add(new XacmlAllOf(new List<XacmlMatch>
            {
                new XacmlMatch(
                    new Uri(XacmlConstants.AttributeMatchFunction.StringEqualIgnoreCase),
                    new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), toId),
                    new XacmlAttributeDesignator(new Uri(XacmlConstants.MatchAttributeCategory.Subject), new Uri(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute), new Uri(XacmlConstants.DataTypes.XMLString), false))
            }));

            // Build Resource
            List<XacmlMatch> resourceMatches = new List<XacmlMatch>();
            foreach (string resource in resourceList)
            {
                string resourceId = resource.Substring(0, resource.LastIndexOf(':'));
                string resourceValue = resource.Substring(resource.LastIndexOf(':') + 1);

                resourceMatches.Add(
                    new XacmlMatch(
                        new Uri(XacmlConstants.AttributeMatchFunction.StringEqualIgnoreCase),
                        new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), resourceValue),
                        new XacmlAttributeDesignator(new Uri(XacmlConstants.MatchAttributeCategory.Resource), new Uri(resourceId), new Uri(XacmlConstants.DataTypes.XMLString), false)));
            }

            List<XacmlAllOf> resourceAllOfs = new List<XacmlAllOf> { new XacmlAllOf(resourceMatches) };

            // Build Action
            List<XacmlAllOf> actionAllOfs = new List<XacmlAllOf>();
            string actionId = action.Substring(0, action.LastIndexOf(':'));
            string actionValue = action.Substring(action.LastIndexOf(':') + 1);

            actionAllOfs.Add(new XacmlAllOf(new List<XacmlMatch>
            {
                new XacmlMatch(
                        new Uri(XacmlConstants.AttributeMatchFunction.StringEqualIgnoreCase),
                        new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), actionValue),
                        new XacmlAttributeDesignator(new Uri(XacmlConstants.MatchAttributeCategory.Action), new Uri(actionId), new Uri(XacmlConstants.DataTypes.XMLString), false))
            }));

            targetList.Add(new XacmlAnyOf(subjectAllOfs));
            targetList.Add(new XacmlAnyOf(resourceAllOfs));
            targetList.Add(new XacmlAnyOf(actionAllOfs));

            return new XacmlTarget(targetList);
        }

        /// <summary>
        /// Returns a list of resource/action keys based on a given policy rule
        /// </summary>
        /// <param name="rule">the rule to analyze</param>
        /// <param name="resource">the resource registry identifier value policy subjects must contain</param>
        /// <returns>list of resource/action keys</returns>
        public static IEnumerable<string> CalculateRightKeys(XacmlRule rule, string resource)
        {
            List<string> result = [];

            // Use policy to calculate the rest of the key
            var resources = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Resource).ToList();
            var actions = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Action);
            List<string> resourceKeys = new List<string>();
            List<string> actionKeys = new List<string>();

            foreach (var resourceObj in resources)
            {
                var org = resourceObj.FirstOrDefault(r => r.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute));
                var app = resourceObj.FirstOrDefault(r => r.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute));

                if (org != null && app != null)
                {
                    string resourceAppId = $"app_{org.Value}_{app.Value}";
                    resourceObj.Remove(org);
                    resourceObj.Remove(app);
                    resourceObj.Add(new PolicyAttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, Value = resourceAppId });
                }

                // Just throw away resources not matching the resourceid we are looking for
                if (resourceObj.Any(r => r.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute) && r.Value.Equals(resource, StringComparison.OrdinalIgnoreCase)) == false)
                {
                    continue;
                }

                StringBuilder resourceKey = new();

                resourceObj.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in resourceObj)
                {
                    resourceKey.Append(item.Id.ToLowerInvariant());
                    resourceKey.Append(':');
                    resourceKey.Append(item.Value.ToLowerInvariant());
                    resourceKey.Append(':');
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
                    actionKey.Append(item.Id.ToLowerInvariant());
                    actionKey.Append(':');
                    actionKey.Append(item.Value.ToLowerInvariant());
                    actionKey.Append(':');
                }

                if (actionKey.Length > 0)
                {
                    actionKey.Remove(actionKey.Length - 1, 1);
                }

                actionKeys.Add(actionKey.ToString());
            }

            foreach (var resourceKey in resourceKeys)
            {
                foreach (var actionKey in actionKeys)
                {
                    string rightKeyPlain = resourceKey + ":" + actionKey;
                    string rightKeyHashed = "01" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rightKeyPlain))).ToLowerInvariant();
                    result.Add(rightKeyHashed);
                }
            }

            return result;
        }

        /// <summary>
        /// Method to check if a resourceid is an app and decompose it into org/app values
        /// </summary>
        /// <param name="resource">the resourceid to check</param>
        /// <param name="org">the org part of the resourceid if it is an app</param>
        /// <param name="app">the app part of the resourceid if it is an app</param>
        /// <returns>true if app false if not</returns>
        public static bool IsAppResource(string resource, out string org, out string app)
        {
            org = null;
            app = null;
            bool isApp = false;

            if (resource.StartsWith("app_"))
            {
                isApp = true;
                string[] parts = resource.Split('_', 3);
                if (parts.Length == 3)
                {
                    org = parts[1];
                    app = parts[2];
                }
            }

            return isApp;
        }

        /// <summary>
        /// Check to verify if a given exception should be pushed to the error queue for later handling.
        /// </summary>
        /// <param name="ex">the exception to check</param>
        /// <returns>verdict to add to error queue</returns>
        public static bool CheckIfErrorShouldBePushedToErrorQueue(Exception ex)
        {
            if (ex.Message.StartsWith("Resource '", StringComparison.InvariantCultureIgnoreCase) && ex.Message.EndsWith("' not found", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (ex.InnerException != null && ex.InnerException.Message.StartsWith("23503: insert or update on table \"assignment\" violates foreign key constraint \"fk_assignment_entity_toid\"", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (ex.InnerException != null && ex.InnerException.Message.StartsWith("23503: insert or update on table \"assignment\" violates foreign key constraint \"fk_assignment_entity_fromid\"", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (ex.Message.StartsWith("Resource '", StringComparison.InvariantCultureIgnoreCase) && ex.Message.EndsWith("' not found", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (ex.Message.Equals("Audit fields are required.", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
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
    }
}
