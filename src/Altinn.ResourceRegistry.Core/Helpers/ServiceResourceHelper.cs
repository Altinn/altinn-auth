using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Helpers
{
    /// <summary>
    /// ServiceResource helper methods
    /// </summary>
    public static class ServiceResourceHelper
    {
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
