using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Models.Register;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.ResourceRegistry.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices;

public class ResourceRegistryMock : IResourceRegistry
{
    private readonly JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    private IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    public Task<ServiceResource> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        string cacheKey = "r:" + resourceId;
        if (!_memoryCache.TryGetValue(cacheKey, out ServiceResource resource))
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            string resourceListPath = Path.Combine(unitTestFolder, "Data", "Json", "ResourceList", "ResourceList.json");
            if (File.Exists(resourceListPath))
            {
                string content = File.ReadAllText(resourceListPath);
                List<ServiceResource> resourceList = JsonSerializer.Deserialize<List<ServiceResource>>(content, options);
                return Task.FromResult(resourceList.Find(r => r.Identifier.Equals(resourceId)));
            }

            if (resource != null)
            {
                PutInCache(cacheKey, 5, resource);
            }
        }

        return Task.FromResult(resource);
    }

    public async Task<XacmlPolicy> GetResourcePolicyAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        string cacheKey = "resourcepolicy:" + resourceId;
        if (!_memoryCache.TryGetValue(cacheKey, out XacmlPolicy policy))
        {
            if (File.Exists(Path.Combine(GetResourceRegistryPolicyPath(resourceId), "policy.xml")))
            {
                policy = await Task.FromResult(ParsePolicy("policy.xml", GetResourceRegistryPolicyPath(resourceId)));
            }

            if (policy != null)
            {
                if (File.Exists(Path.Combine(GetResourceRegistryPolicyPath(resourceId), "policy.xml")))
                {
                    return await Task.FromResult(ParsePolicy("policy.xml", GetResourceRegistryPolicyPath(resourceId)));
                }

                PutInCache(cacheKey, 5, policy);
            }
        }

        return policy;
    }

    public Task<IEnumerable<AccessListResourceMembershipWithActionFilterDto>> GetMembershipsForResourceForParty(PartyUrn partyUrn, ResourceIdUrn resourceIdUrn, CancellationToken cancellationToken = default)
    {
        partyUrn.IsPartyUuid(out Guid partyUuid);
        partyUrn.IsOrganizationIdentifier(out OrganizationNumber partyOrgNum);
        resourceIdUrn.IsResourceId(out ResourceIdentifier resourceId);

        string cacheKey = $"AccListMemb|{partyUrn}|{resourceIdUrn}";
        if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<AccessListResourceMembershipWithActionFilterDto> memberships))
        {
            if (partyOrgNum.ToString() == "910459880" && resourceId.ToString() == "ttd-accesslist-resource")
            {
                memberships = JsonSerializer.Deserialize<IEnumerable<AccessListResourceMembershipWithActionFilterDto>>(
                """
                [
                  {
                    "party": "urn:altinn:party:uuid:00000000-0000-0000-0005-000000005545",
                    "resource": "urn:altinn:resource:ttd-accesslist-resource",
                    "since": "2024-08-27T15:15:55.446051+00:00"
                  }
                ]
                """,
                options);
            }
            else if (partyOrgNum.ToString() == "910459880" && resourceId.ToString() == "ttd-accesslist-resource-with-actionfilter")
            {
                memberships = JsonSerializer.Deserialize<IEnumerable<AccessListResourceMembershipWithActionFilterDto>>(
                """
                [
                  {
                    "party": "urn:altinn:party:uuid:00000000-0000-0000-0005-000000005545",
                    "resource": "urn:altinn:resource:ttd-accesslist-resource",
                    "since": "2024-08-27T15:15:55.446051+00:00",
                    "actionFilters": [
                      "read"
                    ]
                  }
                ]
            """,
                options);
            }

            if (memberships != null)
            {
                PutInCache(cacheKey, 5, memberships);
                return Task.FromResult(memberships);
            }
        }

        return Task.FromResult(Enumerable.Empty<AccessListResourceMembershipWithActionFilterDto>());
    }

    private static string GetResourceRegistryPolicyPath(string resourceId)
    {
        string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
        return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Xacml", "3.0", "ResourceRegistry", resourceId);
    }
    
    public static XacmlPolicy ParsePolicy(string policyDocumentTitle, string policyPath)
    {
        XmlDocument policyDocument = new XmlDocument();

        policyDocument.Load(Path.Combine(policyPath, policyDocumentTitle));
        XacmlPolicy policy;
        using (XmlReader reader = XmlReader.Create(new StringReader(policyDocument.OuterXml)))
        {
            policy = XacmlParser.ParseXacmlPolicy(reader);
        }

        return policy;
    }

    private void PutInCache(string cachekey, int cacheTimeout, object cacheObject)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
           .SetPriority(CacheItemPriority.High)
           .SetAbsoluteExpiration(new TimeSpan(0, cacheTimeout, 0));

        _memoryCache.Set(cachekey, cacheObject, cacheEntryOptions);
    }

    public Task<IEnumerable<AccessListInfoDto>> GetMembershipsForParty(PartyUrn.PartyUuid partyUrn, CancellationToken cancellationToken = default)
    {
        if (partyUrn.IsPartyUuid(out Guid partyUuid))
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            string accessListfolder = Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ResourceRegistry", "AccessLists");
            string accessListPath = Path.Combine(accessListfolder, $"{partyUuid.ToString()}.json");

            if (File.Exists(accessListPath))
            {
                string content = File.ReadAllText(accessListPath);
                List<AccessListInfoDto> accessList = JsonSerializer.Deserialize<List<AccessListInfoDto>>(content, options);
                return Task.FromResult(accessList.AsEnumerable());
            }
        }

        return Task.FromResult(Enumerable.Empty<AccessListInfoDto>());
    }
}
