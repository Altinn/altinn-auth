using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Integration.Clients;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using static Altinn.Authorization.ABAC.Constants.XacmlConstants;
using Right = AccessMgmt.Tests.Models.ResourceRegistry.Right;

namespace Altinn.AccessManagement.Tests.Mocks
{
    /// <summary>
    /// Mock class for <see cref="IResourceRegistryClient"></see> interface
    /// </summary>
    public class ResourceRegistryClientMock : IResourceRegistryClient
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegistryClient"/> class
        /// </summary>
        public ResourceRegistryClientMock()
        {
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResource(string resourceId, CancellationToken cancellationToken = default)
        {
            ServiceResource resource = null;
            string rolesPath = GetResourcePath(resourceId);
            if (File.Exists(rolesPath))
            {
                string content = File.ReadAllText(rolesPath);
                resource = (ServiceResource)JsonSerializer.Deserialize(content, typeof(ServiceResource), _serializerOptions);
            }

            return await Task.FromResult(resource);
        }

        /// <inheritdoc/>
        public Task<List<ServiceResource>> GetResources(CancellationToken cancellationToken = default, string? searchParams = null)
        {
            List<ServiceResource> resources = new List<ServiceResource>();

            string path = GetDataPathForResources();
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    if (file.Contains("resources"))
                    {
                        string content = File.ReadAllText(Path.Combine(path, file));
                        resources = JsonSerializer.Deserialize<List<ServiceResource>>(content, _serializerOptions);
                    }
                }
            }

            return Task.FromResult(resources);
        }

        /// <inheritdoc/>
        public Task<List<ServiceResource>> GetResourceList(CancellationToken cancellationToken = default)
        {
            string content = File.ReadAllText($"Data/Resources/resourceList.json");
            List<ServiceResource> resources = (List<ServiceResource>)JsonSerializer.Deserialize(content, typeof(List<ServiceResource>), _serializerOptions);

            return Task.FromResult(resources);
        }

        /// <inheritdoc/>
        public Task<IDictionary<string, IEnumerable<BaseAttribute>>> GetSubjectResources(IEnumerable<string> subjects, CancellationToken cancellationToken = default)
        {
            string content = File.ReadAllText($"Data/Resources/subjectResources.json");
            PaginatedResult<SubjectResources> allSubjectResources = (PaginatedResult<SubjectResources>)JsonSerializer.Deserialize(content, typeof(PaginatedResult<SubjectResources>), _serializerOptions);

            IDictionary<string, IEnumerable<BaseAttribute>> result = new Dictionary<string, IEnumerable<BaseAttribute>>();
            if (allSubjectResources != null && allSubjectResources.Items != null)
            {
                foreach (SubjectResources resultItem in allSubjectResources.Items.Where(sr => subjects.Contains(sr.Subject.Urn)))
                {
                    result.Add(resultItem.Subject.Urn, resultItem.Resources);
                }
            }

            return Task.FromResult(result);
        }

        public async Task<ConsentTemplate> GetConsentTemplate(string templateId, int? version, CancellationToken cancellationToken = default)
        {
            string path = GetDataPathConsentTemplate();
            if (!File.Exists(path))
            {
                return null;
            }

            using FileStream stream = File.OpenRead(path);
            List<ConsentTemplate> allTemplates = await JsonSerializer.DeserializeAsync<List<ConsentTemplate>>(stream, _serializerOptions, cancellationToken);
            return allTemplates?.FirstOrDefault(ct => ct.Id == templateId);
        }

        private static string GetDataPathConsentTemplate()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(ResourceRegistryClientMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "Data", "ConsentTemplates", "consent_templates.json");
        }

        private static string GetResourcePath(string resourceRegistryId)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(ResourceRegistryClientMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ResourceRegistryResources", $"{resourceRegistryId}", "resource.json");
        }

        private static string GetDataPathForResources()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(ResourceRegistryClientMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "Data", "Resources");
        }

        public async Task<List<RightDto>> GetPolicyRightsV2(string resource, string languageCode, CancellationToken cancellationToken)
        {
            PolicyRetrievalPointMock policyRetrievalPointMock = new();
            XacmlPolicy policy = await policyRetrievalPointMock.GetPolicyAsync(resource, cancellationToken);
            List<Right> rulesl = DecomposePolicy(policy, resource, includeServiceOwnerRights: false, includeAppRights: false);
            List<RightDto> policyRights = await MapFromInternalToDecomposedRights(rulesl, resource, languageCode, cancellationToken);
            return policyRights;
        }

        #region Code from resource registry to support mocking of rights decomposition in access management tests
        public static List<Right> DecomposePolicy(XacmlPolicy policy, string resourceId, bool includeServiceOwnerRights, bool includeAppRights)
        {
            Dictionary<string, Right> rights = new Dictionary<string, Right>();

            foreach (XacmlRule rule in policy.Rules)
            {
                IEnumerable<Right> rightsWithKeys = CalculateActionKey(rule, resourceId);
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

        private static IEnumerable<Right> CalculateActionKey(XacmlRule rule, string resourceId)
        {
            List<Right> result = [];

            // Use policy to calculate the rest of the key
            List<List<PolicyAttributeMatch>> resources = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Resource).ToList();
            List<List<PolicyAttributeMatch>> actions = PolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Action);
            List<Right> resourceKeys = new List<Right>();
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

                // Just throw away resources not matching the resourceid we are looking for
                if (resource.Any(r => r.Id.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute) && r.Value.Equals(resourceId, StringComparison.OrdinalIgnoreCase)) == false)
                {
                    continue;
                }

                Right rightWithKey = new()
                {
                    Resource = [.. resource] // Collection expression with spread - creates a new list
                };

                StringBuilder resourceKey = new();

                resource.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in resource)
                {
                    resourceKey.Append(item.Id);
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

                action.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.InvariantCultureIgnoreCase));
                foreach (var item in action)
                {
                    actionKey.Append(item.Id);
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
                    result.Add(new Right { Key = resource.Key + ":" + action, Resource = resource.Resource, Action = new PolicyAttributeMatch() { Id = MatchAttributeIdentifiers.ActionId, Value = action.Replace(MatchAttributeIdentifiers.ActionId + ":", string.Empty) } });
                }
            }

            return result;
        }

        /// <summary>
        /// Copy from resource registry
        /// </summary>
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

        private async Task<List<RightDto>> MapFromInternalToDecomposedRights(List<Right> rights, string resource, string language, CancellationToken cancellationToken = default)
        {
            List<RightDto> result = [];

            foreach (var right in rights)
            {
                result.Add(await MapFromInternalToDecomposeRight(right, resource, language, cancellationToken));
            }

            return result;
        }

        private async Task<RightDto> MapFromInternalToDecomposeRight(Right rights, string resource, string language, CancellationToken cancellationToken)
        {
            ResourceAndAction resourceAndAction = SplitRightKey(rights.Key);

            RightDto right = new()
            {
                Key = "01" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rights.Key.ToLowerInvariant()))).ToLowerInvariant(),
                Name = GetActionNameFromRightKey(rights.Key, resource, language),
                Resource = rights.Resource.Select(m => new AttributeDto() { Type = m.Id, Value = m.Value }).ToList(),
                Action = new AttributeDto() { Type = MatchAttributeIdentifiers.ActionId, Value = rights.Action.Value }
            };

            return right;
        }

        private static ResourceAndAction SplitRightKey(string actionKey)
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

        private string GetActionNameFromRightKey(string key, string resourceId, string language)
        {
            string[] parts = key.Split("urn:", options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            StringBuilder sb = new();

            bool actionAdded = false;
            foreach (string part in parts.OrderDescending())
            {
                string currentPart = part;
                if (currentPart.Substring(currentPart.Length - 1, 1) == ":")
                {
                    currentPart = currentPart.Substring(0, currentPart.Length - 1);
                }

                int removeBefore = currentPart.LastIndexOf(':');
                if (removeBefore > -1)
                {
                    currentPart = currentPart.Substring(currentPart.LastIndexOf(':') + 1);
                }

                if (currentPart.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                if (part.StartsWith("oasis:names:tc:xacml:1.0:action:action-id"))
                {
                    currentPart = GetActionName(currentPart, language);
                    actionAdded = true;
                }
                else if (actionAdded)
                {
                    currentPart = "(" + currentPart + ")";
                }

                sb.Append(UppercaseFirstLetter(currentPart));
                sb.Append(' ');
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private string UppercaseFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        private string GetActionName(string actionName, string language)
        {
            if (language == null)
            {
                language = "nb";
            }

            if (actionName == null)
            {
                return actionName;
            }

            Dictionary<string, string> actionDictionary = GetActionDictionary(language);

            if (actionDictionary != null)
            {
                if (actionDictionary.TryGetValue(actionName.ToLowerInvariant(), out string translatedAction))
                {
                    return translatedAction;
                }
            }

            return actionName;
        }

        private Dictionary<string, string> GetActionDictionary(string language)
        {
            Dictionary<string, Dictionary<string, string>> actionDictionary = new Dictionary<string, Dictionary<string, string>>()
                {
                    { "nb", new Dictionary<string, string>() { { "read", "les" }, { "write", "skriv" }, { "delete", "slett" } } },
                    { "en", new Dictionary<string, string>() { { "read", "read" }, { "write", "write" }, { "delete", "delete" } } }
                };

            if (actionDictionary.TryGetValue(language.ToLowerInvariant(), out Dictionary<string, string> translatedActions))
            {
                return translatedActions;
            }

            return actionDictionary["en"];
        }

        #endregion
    }
}
