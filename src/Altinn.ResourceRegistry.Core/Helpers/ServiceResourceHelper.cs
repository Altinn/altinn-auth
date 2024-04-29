using System.Text.RegularExpressions;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Helpers
{
    /// <summary>
    /// ServiceResource helper methods
    /// </summary>
    public static class ServiceResourceHelper
    {
        private static readonly Regex ResourceIdentifierRegex = new Regex("^[a-z0-9_-]{4,}$", RegexOptions.Compiled);

        /// <summary>
        /// Gets resources from the resourcelist that fits the search criteria
        /// </summary>
        /// <param name="resourceList">The resourceList that needs to be searched</param>
        /// <param name="resourceSearch">The search criteria</param>
        public static List<ServiceResource> GetSearchResultsFromResourceList(List<ServiceResource> resourceList, ResourceSearch resourceSearch)
        {
            List<ServiceResource> searchResults = new List<ServiceResource>();

            foreach (ServiceResource serviceResource in resourceList)
            {
                if (MatchingIdentifier(serviceResource, resourceSearch) && MatchingDescription(serviceResource, resourceSearch) && MatchingResourceType(serviceResource, resourceSearch) && MatchingKeywords(serviceResource, resourceSearch))
                {
                    searchResults.Add(serviceResource);
                }
            }

            return searchResults;
        }

        /// <summary>
        /// Method to validate service resource
        /// </summary>
        public static bool ValidateResource(ServiceResource serviceResource, out Dictionary<string, List<string>> validationMessages)
        {
            bool isValid = true;
            validationMessages = new Dictionary<string, List<string>>();
            if (IsInvalidServiceEngineResource(serviceResource))
            {
                // Uses Service engine prefix without it beeing a service engine resource
                AddValidationMessage(validationMessages, "Identifier", "Invalid prefix. Only to be used for Altinn 2 services");
                isValid = false;
            }

            if (IsInvalidAppResource(serviceResource))
            {
                // Uses app prefix without it beeing a app resource
                AddValidationMessage(validationMessages, "Identifier", "Invalid Prefix. app_ is only for Altinn Studio apps");
                isValid = false;
            }

            if (serviceResource.ResourceType.Equals(ResourceType.Default))
            {
                AddValidationMessage(validationMessages, "ResourceType", "ResourceType needs to be set");
                isValid = false;
            }

            if (!ValidateResourceOwner(serviceResource))
            {
                AddValidationMessage(validationMessages, "HasCompetentAuthority.Organization", "HasCompetentAuthority needs to be set with valid organization number");
                isValid = false;
            }

            if (!ResourceIdentifierRegex.IsMatch(serviceResource.Identifier))
            {
                AddValidationMessage(validationMessages, "Identifier", "Invalid id. Only a-z and 0-9 is allowed together with _ and -.  Minimum 4 characters");
                isValid = false;
            }

            if (!HasLanguage(serviceResource.Title, ResourceConstants.LANGUAGE_EN))
            {
                AddValidationMessage(validationMessages, "Title", $"Missing title in english {ResourceConstants.LANGUAGE_EN}");
                isValid = false;
            }

            if (!HasLanguage(serviceResource.Title, ResourceConstants.LANGUAGE_NB))
            {
                AddValidationMessage(validationMessages, "Title", $"Missing title in bokmal {ResourceConstants.LANGUAGE_NB}");
                isValid = false;
            }

            if (!HasLanguage(serviceResource.Title, ResourceConstants.LANGUAGE_NN))
            {
                AddValidationMessage(validationMessages, "Title", $"Missing title in nynorsk {ResourceConstants.LANGUAGE_NN}");
                isValid = false;
            }
       
            if (serviceResource.Delegable && (!HasLanguage(serviceResource.RightDescription, ResourceConstants.LANGUAGE_EN)))
            {
                AddValidationMessage(validationMessages, "RightDescription", $"Missing RightDescription in english {ResourceConstants.LANGUAGE_EN}");
                isValid = false;
            }

            if (serviceResource.Delegable && (!HasLanguage(serviceResource.RightDescription, ResourceConstants.LANGUAGE_NB)))
            {
                AddValidationMessage(validationMessages, "RightDescription", $"Missing RightDescription in bokmal {ResourceConstants.LANGUAGE_NB}");
                isValid = false;
            }

            if (serviceResource.Delegable && (!HasLanguage(serviceResource.RightDescription, ResourceConstants.LANGUAGE_NN)))
            {
                AddValidationMessage(validationMessages, "RightDescription", $"Missing RightDescription in nynorsk {ResourceConstants.LANGUAGE_NN}");
                isValid = false;
            }

            if (!HasLanguage(serviceResource.Description, ResourceConstants.LANGUAGE_EN))
            {
                AddValidationMessage(validationMessages, "Description", $"Missing Description in english {ResourceConstants.LANGUAGE_EN}");
                isValid = false;
            }

            if (!HasLanguage(serviceResource.Description, ResourceConstants.LANGUAGE_NB))
            {
                AddValidationMessage(validationMessages, "Description", $"Missing Description in bokmal {ResourceConstants.LANGUAGE_NB}");
                isValid = false;
            }

            if (!HasLanguage(serviceResource.Description, ResourceConstants.LANGUAGE_NN))
            {
                AddValidationMessage(validationMessages, "Description", $"Missing Description in nynorsk {ResourceConstants.LANGUAGE_NN}");
                isValid = false;
            }

            if (serviceResource.ResourceType.Equals(ResourceType.MaskinportenSchema) && (serviceResource.ResourceReferences == null || !serviceResource.ResourceReferences.Any(r => r.ReferenceType.Equals(ReferenceType.MaskinportenScope))))
            {
                AddValidationMessage(validationMessages, "ResourceReferences", $"Missing maskinporten scopen for MaskinportenSchema resource");
                isValid = false;
            }

            return isValid;

            static bool IsInvalidServiceEngineResource(ServiceResource serviceResource)
            {
                if (serviceResource.Identifier.StartsWith(ResourceConstants.SERVICE_ENGINE_RESOURCE_PREFIX)
                    && (serviceResource.ResourceReferences == null
                    || !serviceResource.ResourceReferences.Any()
                    || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceCode))
                    || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceEditionCode))))
                {
                    // Uses Service engine prefix without it beeing a service engine resource
                    return true;
                }

                return false;
            }

            static bool IsInvalidAppResource(ServiceResource serviceResource)
            {
                if (serviceResource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX)
                    && (serviceResource.ResourceReferences == null
                    || !serviceResource.ResourceReferences.Any()
                    || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ApplicationId))))
                {
                    // Uses app prefix without it beeing a app resource
                    return true;
                }

                return false;
            }

            // Validates that orgs that is not TTD needs to have orgnumber set.
            static bool ValidateResourceOwner(ServiceResource serviceResource)
            {
                if (serviceResource.HasCompetentAuthority == null)
                {
                    return false;
                }

                if (serviceResource.HasCompetentAuthority.Orgcode != null && serviceResource.HasCompetentAuthority.Orgcode.Equals("ttd", StringComparison.OrdinalIgnoreCase))
                {
                    // TTD does not have orgnumber
                    return true;
                }

                if (string.IsNullOrWhiteSpace(serviceResource.HasCompetentAuthority.Organization))
                {
                    return false;
                }

                return true;
            }
        }

        private static void AddValidationMessage(Dictionary<string, List<string>> validationMessages, string key, string message)
        {
            if (validationMessages.ContainsKey(key))
            {
                validationMessages[key].Add(message);
            }
            else
            {
                validationMessages.Add(key, [message]);
            }
        }

        private static bool HasLanguage(Dictionary<string, string> textDictionary, string language)
        {
            if (textDictionary == null || !textDictionary.ContainsKey(language) || string.IsNullOrWhiteSpace(textDictionary[language]))
            {
                return false;
            }

            return true;
        }

        private static bool MatchingIdentifier(ServiceResource resource, ResourceSearch resourceSearch)
        {
            return resourceSearch.Id == null || resource.Identifier.Contains(resourceSearch.Id, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool MatchingDescription(ServiceResource resource, ResourceSearch resourceSearch)
        {
            if (resourceSearch.Description == null)
            {
                return true;
            }
            else
            {
                if (resource.Description == null)
                {
                    return false;
                }
                else
                {
                    return resource.Description.Any(d => d.Value.Contains(resourceSearch.Description, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        private static bool MatchingResourceType(ServiceResource resource, ResourceSearch resourceSearch)
        {
            return resourceSearch.ResourceType == null || resource.ResourceType == resourceSearch.ResourceType;
        }

        private static bool MatchingKeywords(ServiceResource resource, ResourceSearch resourceSearch)
        {
            if (resourceSearch.Keyword != null && resource.Keywords != null)
            {
                if (resourceSearch.Keyword != null && resource.Keywords.Count == 0)
                {
                    return false;
                }
                else
                {
                    return resource.Keywords.Exists(k => k.Word.Contains(resourceSearch.Keyword, StringComparison.InvariantCultureIgnoreCase));
                }
            }
            else
            {
                if ((resourceSearch.Keyword != null) && (resource.Keywords == null || resource.Keywords.Count == 0))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
